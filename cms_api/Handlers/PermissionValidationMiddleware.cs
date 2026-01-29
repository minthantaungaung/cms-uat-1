using aia_core.Model.Cms;
using aia_core;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace cms_api.Handlers
{
    public class PermissionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private IConfiguration _config;
        private readonly IServiceScopeFactory serviceFactory;

        public PermissionValidationMiddleware(RequestDelegate next, IConfiguration config, IServiceScopeFactory serviceFactory)
        {
            _next = next;
            _config = config;
            this.serviceFactory = serviceFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {


            var route = context?.Request?.Path.ToString() ?? "";
            
            using (var scope = this.serviceFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();
                //var isListedRoute = unitOfWork.GetRepository<aia_core.Entities.Route>().Query(x => x.Route1 == route).Any();



                // if(route.ToLower().Contains("migrate"))
                // {
                //     await _next(context);
                // }

                var authToken = context.Request.Headers["Authorization"];

                

                if (authToken.Count() != 0)
                {

                    var splitValue = authToken.FirstOrDefault().Split(" ");


                    if (splitValue[0] == "Bearer")
                    {
                        if (!route.ToLower().Contains("/v1/auth/login") && !route.ToLower().Contains("/v1/saml")
                            && !route.ToLower().Contains("/v1/dev") && !route.ToLower().Contains("/v1/migrate") && !route.ToLower().Contains("/v1/crm/update"))
                        {
                            var sessionId = context.User?.Claims?.FirstOrDefault(c => c.Type == CMSClaim.GenerateToken).Value;

                            if (!string.IsNullOrEmpty(sessionId))
                            {
                                var isValid = unitOfWork.GetRepository<aia_core.Entities.CmsUserSession>()
                                    .Query(x => x.SessionId == Guid.Parse(sessionId)).Any();

                                if (!isValid)
                                {
                                    context.Response.StatusCode = 401;
                                    return;
                                }

                            }
                            else
                            {
                                context.Response.StatusCode = 401;
                                return;
                            }
                        }

                        var isListedRoute = unitOfWork.GetRepository<aia_core.Entities.Route>().Query(x => route.StartsWith(x.Route1.Trim())).Any();


                        if (isListedRoute)
                        {
                            // Extract role from validated token
                            var role = GetRoleFromToken(context.User);

                            // Check route permission based on the extracted role
                            if (!IsAuthorized(role, context.Request.Path))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                return;
                            }
                        }
                    }
                }
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }

        private string GetRoleFromToken(ClaimsPrincipal user)
        {
            // Extract role from claims

            try
            {
                using (var scope = this.serviceFactory.CreateScope())
                {
                    var email = user?.Claims?.FirstOrDefault(c => c.Type == CMSClaim.Email).Value;
                    if (email == null) return null;

                    var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();
                    var staff = unitOfWork.GetRepository<aia_core.Entities.Staff>()
                        .Query(x => x.Email == email && x.IsActive == true).FirstOrDefault();

                    if (staff == null) return null;
                    return staff?.RoleId?.ToString();

                }
            }
            catch
            {
            }

            return null;

            //return user?.Claims?.FirstOrDefault(c => c.Type == CMSClaim.RoleID).Value;
        }

        private bool IsAuthorized(string roleId, string requestedPath)
        {
            try
            {
                using (var scope = this.serviceFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();

                    var role = unitOfWork.GetRepository<aia_core.Entities.Role>().Query(x => x.Id == Guid.Parse(roleId)).FirstOrDefault();
                    if (role == null) { return false; }

                    var permissions = role.Permissions.Split(",");
                    var permList = new List<string>();

                    foreach (var permission in permissions)
                    {
                        var perm = permission.Replace("[", "").Replace("]", "");
                        permList.Add(perm);
                    }

                    var routes = unitOfWork.GetRepository<aia_core.Entities.Route>().Query(x => permList.Contains(x.Permission)).ToList();
                    if (routes == null || routes.Count == 0) { return false; }

                    //var isAuthrozed = routes.Where(x => x.Route1.Contains(requestedPath)).Any();

                    foreach (var route in routes)
                    {
                        var isAuthrozed = requestedPath.StartsWith(route.Route1.Trim());
                        if (isAuthrozed) { return true; }
                    }



                }
            }
            catch
            {
            }

            return false;
        }
    }

}
