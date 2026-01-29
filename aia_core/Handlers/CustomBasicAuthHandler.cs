using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text;
using aia_core.Services;

namespace aia_core.Handlers
{
    public class CustomBasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;

        public CustomBasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options
            , ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IUserService userService)
            : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                var response = new ResponseModel<string> { Code = 400, Message = "Missing Authorization Header!" };
                Response.StatusCode = 401;
                Response.ContentType = "application/json";
                await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(response));
                await Response.CompleteAsync();
                return null;
            }

            UserManager user = null;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                user = await _userService.Authenticate(username, password, "crm");
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }

            if (user == null)
            {
                var response = new ResponseModel<string> { Code = 401, Message = "Invalid Username or Password!" };
                Response.StatusCode = 401;
                Response.ContentType = "application/json";
                await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(response));
                await Response.CompleteAsync();
                return null;
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
