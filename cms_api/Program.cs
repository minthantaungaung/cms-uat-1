using aia_core;
using aia_core.Entities;
using aia_core.Handlers;
using aia_core.RecurringJobs;
using aia_core.Repository;
using aia_core.Repository.Cms;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using Azure.Identity;
using cms_api.Filters;
using cms_api.Handlers;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FirebaseAdmin;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
// Add Key Vault configuration
builder.Configuration.AddAzureKeyVault(
    new Uri("https://kv-mm01-sea-u-app-vlt01.vault.azure.net/"),
    new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = "9941156c-93d3-4957-b2e1-f8c5ee2a9c98"
    }),
    new JsonKeyVaultSecretManager("uat-cms-appsettings") // single JSON secret
);

//builder.Configuration
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
//                  optional: true, reloadOnChange: true)
//    .AddEnvironmentVariables();

// After loading, parse the JSON secret manually
var secretString = builder.Configuration["uat-cms-appsettings"];
Console.WriteLine($"Key Vault Secret Value: {secretString}");

if (!string.IsNullOrEmpty(secretString))
{
    var jsonData = JsonDocument.Parse(secretString);
    var dict = new Dictionary<string, string>();

    void Flatten(JsonElement element, string prefix = "")
    {
        foreach (var prop in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
                Flatten(prop.Value, key);
            else
                dict[key] = prop.Value.ToString();
        }
    }

    Flatten(jsonData.RootElement);
    builder.Configuration.AddInMemoryCollection(dict);
}

ConfigurationManager config = builder.Configuration;

#region #Temp Write Okta Config Values
var baseUrl = config["Okta:BaseUrl"];
var clientID = config["Okta:ClientID"];
var groupID = config["Okta:GroupID"];
var privateKeyFile = config["Okta:PrivateKeyFile"];
var signUrl = config["okta:JwtSignUrl"];

Console.WriteLine($"Okta BaseUrl: {baseUrl}");
Console.WriteLine($"Okta ClientID: {clientID}");
Console.WriteLine($"Okta GroupID: {groupID}");
Console.WriteLine($"Okta PrivateKeyFile: {privateKeyFile}");
Console.WriteLine($"Okta SignUrl: {signUrl}");
#endregion

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // instead of 80
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddAuthentication(DefaultConstants.BasicAuthentication)
                .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(DefaultConstants.BasicAuthentication, null);
builder.Services.AddAuthentication(DefaultConstants.CustomBasicAuthentication)
                .AddScheme<AuthenticationSchemeOptions, CustomBasicAuthHandler>(DefaultConstants.CustomBasicAuthentication, null);


#region# HangFire Config
// Add Hangfire services.

var hangFireConnectionString = config["Database:connectionString"];


builder.Services.AddHangfire(configuration =>
configuration
    //.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    //.UseStorage(new MySqlStorage(
    //   hangFireConnectionString, new MySqlStorageOptions
    //   {
    //       InvisibilityTimeout = TimeSpan.FromMinutes(5),
    //       TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
    //       QueuePollInterval = TimeSpan.FromSeconds(15),
    //       JobExpirationCheckInterval = TimeSpan.FromHours(1),
    //       CountersAggregateInterval = TimeSpan.FromMinutes(5),
    //       PrepareSchemaIfNecessary = true,
    //       DashboardJobListLimit = 50000,
    //       TransactionTimeout = TimeSpan.FromMinutes(1),
    //   }))
    .UseSqlServerStorage(hangFireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true // Migration to Schema 7 is required
    })
    );

//// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

#endregion

#region #cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().SetIsOriginAllowed(origin => true).AllowAnyHeader();
    });
});
#endregion

#region #api-versioning
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});
builder.Services.AddVersionedApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, cms_api.Filters.ConfigureSwaggerOptions>();
#endregion

#region #auth
var domain = $"https://{config["Auth0:Domain"]}/";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(DefaultConstants.AccessTokenBearer, options =>
              {
                  options.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuer = true,
                      ValidateAudience = true,
                      ValidateLifetime = true,
                      ValidateIssuerSigningKey = true,
                      ValidIssuer = config["Auth0:Domain"],
                      ValidAudience = config["Auth0:Audience"],
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Auth0:AccessToken"])),
                      ClockSkew = TimeSpan.Zero
                  };
                  options.Events = new JwtBearerEvents
                  {
                      OnChallenge = async context =>
                      {
                          context.Response.StatusCode = 401;
                          var response = new { error_code = 401, error_msg = "Invalid Access Token or Token Expire!" };
                          context.HttpContext.Response.ContentType = "application/json";
                          context.HttpContext.Response.Headers.Add("error-code", "401");
                          await context.HttpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
                          context.HandleResponse();
                      }

                  };

              });
// .AddJwtBearer(DefaultConstants.AccessTokenBearer, options =>
// {
//     options.Authority = domain;
//     options.Audience = config["Auth0:Audience"];
//     options.Events = new JwtBearerEvents
//     {
//         OnChallenge = async context =>
//         {
//             context.Response.StatusCode = 401;
//             var response = new ResponseModel<string> { Code = 401, Message = "Invalid Token or Token Expire!" };
//             context.HttpContext.Response.ContentType = "application/json";
//             context.HttpContext.Response.Headers.Add("x-error-code", $"401");
//             await context.HttpContext.Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(response));

//             context.HandleResponse();
//         }
//     };
// });
#endregion

#region #swagger
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
    c.SchemaFilter<cms_api.Filters.NamespaceSchemaFilter>();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    #region #basic-auth
    c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Description = "Basic auth added to authorization header",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "basic",
        Type = SecuritySchemeType.Http
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" }
            },
            new List<string>()
        }
    });
    #endregion

    #region #jwt-auth
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer authorisation token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Bearer {token}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
    #endregion
});
#endregion

#region #database
var env = config["Env"];
var connectionString = config["Database:connectionString"];
Console.WriteLine($"Env : {env}");
Console.WriteLine($"connectionString : {connectionString}");
builder.Services.AddDbContext<aia_core.Entities.Context>(options =>
    options.UseSqlServer(connectionString)
);
#endregion

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddMvc().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var modelState = actionContext.ModelState.Values;
        var response = new ResponseModel<object> { Code = 400, Message = string.Join(" | ", modelState.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) };
        return new OkObjectResult(response);
    };
}).SetCompatibilityVersion(CompatibilityVersion.Latest);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IErrorCodeProvider, ErrorCodeProvider>();
builder.Services.AddTransient<IAzureStorageService, AzureStorageService>();
builder.Services.AddTransient<IUnitOfWork<aia_core.Entities.Context>, UnitOfWork<aia_core.Entities.Context>>();
builder.Services.AddTransient<IGeneralRepository, GeneralRepository>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddTransient<ICoverageRepository, CoverageRepository>();
builder.Services.AddTransient<IProductRepository, ProductRepository>();
builder.Services.AddTransient<IBlogRepository, BlogRepository>();
builder.Services.AddTransient<IMemberRepository, MemberRepository>();
builder.Services.AddTransient<IMemberPolicyRepository, MemberPolicyRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IStaffRepository, StaffRepository>();
builder.Services.AddTransient<ILocalizationRepository, LocalizationRepository>();
builder.Services.AddTransient<IOktaService, OktaService>();
builder.Services.AddTransient<IPropositionRepository, PropositionRepository>();
builder.Services.AddTransient<IDevRepository, DevRepository>();
builder.Services.AddTransient<IBankRepository, BankRepository>();
builder.Services.AddTransient<IHospitalRepository, HospitalRepository>();
builder.Services.AddTransient<IClaimIncurredLocationRepository, ClaimIncurredLocationRepository>();
builder.Services.AddTransient<IDiagnosisRepository, DiagnosisRepository>();
builder.Services.AddTransient<IPartialDisabilityRepository, PartialDisabilityRepository>();
builder.Services.AddTransient<IPermanentDisabilityRepository, PermanentDisabilityRepository>();
builder.Services.AddTransient<ICriticalIllnessRepository, CriticalIllnessRepository>();
builder.Services.AddTransient<IDeathRepository, DeathRepository>();
builder.Services.AddTransient<IHolidayRepository, HolidayRepository>();
builder.Services.AddTransient<ICrmRepository, CrmRepository>();
builder.Services.AddTransient<IAiaCrmApiService, AiaCrmApiService>();

builder.Services.AddTransient<IDBCommonRepository, DBCommonRepository>();
builder.Services.AddTransient<IDashboardRepository, DashboardRepository>();


builder.Services.AddScoped<IRecurringJobRunner, RecurringJobRunner>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddSingleton<ITemplateLoader, TemplateLoader>();
builder.Services.AddSingleton<ICmsTokenGenerator, CmsTokenGenerator>();


builder.Services.AddTransient<BaseRepository, BaseRepository>();
builder.Services.AddTransient<IClaimRepository, ClaimRepository>();
builder.Services.AddTransient<IProfileRepository, ProfileRepository>();
builder.Services.AddTransient<IServicingRepository, ServicingRepository>();
builder.Services.AddTransient<IPaymentChangeConfigRepository, PaymentChangeConfigRepository>();
builder.Services.AddTransient<IDocConfigRepository, DocConfigRepository>();
builder.Services.AddTransient<IMigrationRepository, MigrationRepository>();

//builder.Services.AddTransient<INotificationRepository, NotificationRepository>();
builder.Services.AddTransient<IMasterDataRepository, MasterDataRepository>();

builder.Services.AddTransient<IFaqRepository, FaqRepository>();

builder.Services.AddControllers().AddJsonOptions(x =>
{
    // serialize enums as strings in api responses (e.g. Role)
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(NoSingleQuoteActionFilter));
});

try
{
    var firebaseAccount = config["Firebase:ConfigJson"];

    Console.WriteLine($"Firebase:ConfigJson {firebaseAccount}");
    if (!string.IsNullOrEmpty(firebaseAccount))
    {
        var firebaseJson = Encoding.UTF8.GetString(
            Convert.FromBase64String(firebaseAccount)
        );

        var firebaseStream = new MemoryStream(Encoding.UTF8.GetBytes(firebaseJson));
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromStream(firebaseStream)
        });
    }
}
catch (Exception ex)
{
    Console.WriteLine($"firebaseJson error {ex.Message} {ex.StackTrace}");
}

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;

});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

var app = builder.Build();



AppSettingsHelper.AppSettingsConfigure(app.Services.GetRequiredService<IConfiguration>());

app.UsePathBase("/cms-api");

app.UseHttpLogging();


//if (app.Environment.IsDevelopment())
//{
app.UseSwaggerAuthorized();
app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs/api";
        options.DefaultModelsExpandDepth(-1);
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"{config["SwaggerPath"]}/{description.GroupName}/swagger.json", $"{description.GroupName}");
        }
    });
//}
app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        var currentUser = context.User;
        if (context.Response.StatusCode == (int)HttpStatusCode.InternalServerError)
        {
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            context.Response.ContentType = "application/json";

            var errorDetail = $"{exception?.Path} {exception?.Error}";

            await context.Response.Body.WriteAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { code = 500, message = "An error has occurred.", detail = errorDetail }));
        }
    });
});

#region# HangFire Server Enabled OnOff

var varJobOptions = new BackgroundJobServerOptions();
varJobOptions.ServerName = "aia.production-hangfire.server";
varJobOptions.WorkerCount = 2;

var prefixPath = "";
var dashboardPath = "/hangfire";
try
{
    var appSettingEnv = config["Env"];

    if (appSettingEnv == "Uat")
    {
        prefixPath = "/aiaplus/cms-api";
        dashboardPath = "/hangfire";
    }
    else if (appSettingEnv == "Production")
    {
        prefixPath = "/cms-api";
        dashboardPath = "/hangfire";
    }
}
catch
{ }


var dashboardOptions = new DashboardOptions
{

    PrefixPath = prefixPath,
    Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
        {
            RequireSsl = false,
            SslRedirect = false,
            LoginCaseSensitive = true,
            Users = new []
            {
                new BasicAuthAuthorizationUser
                {
                    Login = "admin",
                    PasswordClear =  "codigo180@a!a99"
                }
            }

        }) }
};

app.UseHangfireServer(varJobOptions);
app.UseHangfireDashboard(dashboardPath, dashboardOptions);


var varMonitoringApi = JobStorage.Current.GetMonitoringApi();
var varServerList = varMonitoringApi.Servers().Where(r => r.Name.Contains("aia.production-hangfire.server"));

foreach (var varServerItem in varServerList)
{
    using (var connection = JobStorage.Current.GetConnection())
    {
        connection.RemoveServer(varServerItem.Name);
    }
}

using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    IRecurringJobRunner jobRunner = services.GetRequiredService<IRecurringJobRunner>();

    //8Am jobs
    RecurringJob.AddOrUpdate("SendClaimNotification", () => jobRunner.SendClaimNotification(), "30 1 * * *");
    RecurringJob.AddOrUpdate("SendServiceNotification", () => jobRunner.SendServiceNotification(), "30 1 * * *");
    RecurringJob.AddOrUpdate("CheckBeneficiaryStatusAndSendNoti", () => jobRunner.CheckBeneficiaryStatusAndSendNoti(), "30 1 * * *");
    RecurringJob.AddOrUpdate("UpdateClaimStatus", () => jobRunner.UpdateClaimStatus(true, ""), "30 1 * * *");
    RecurringJob.AddOrUpdate("SendUpcomingPremiumsNotification", () => jobRunner.SendUpcomingPremiumsNotification(), "30 1 * * *");


    //4Am jobs
    RecurringJob.AddOrUpdate("UpdateMemberDataPullFromAiaCoreTables", () => jobRunner.UpdateMemberDataPullFromAiaCoreTables(), "30 21 * * *");


    ////Here are the UTC times for your desired schedule:


    ////6 PM MST = 11:30 AM UTC



    RecurringJob.AddOrUpdate("SendClaimSms", () => jobRunner.SendClaimSms(), "30 11 * * *");
    RecurringJob.AddOrUpdate("SendServicingSms", () => jobRunner.SendServicingSms(), "30 11 * * *");

    //RecurringJob.AddOrUpdate("SendClaimSms", () => jobRunner.SendClaimSms(), "30 9 * * *");
    //RecurringJob.AddOrUpdate("SendServicingSms", () => jobRunner.SendServicingSms(), "30 9 * * *");




    try
    {

        await jobRunner.UploadDefaultCmsImages();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"An error occurred: {ex.Message}");
    }
}

#endregion



app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
// app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.UseMiddleware<PermissionValidationMiddleware>();

app.Run();
