using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using aia_core;

namespace cms_api.Filters
{

    public class NoSingleQuoteActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            string httpPath = context?.HttpContext?.Request?.Path;
            string httpMethod = context?.HttpContext?.Request?.Method;

            var projectRoot = context?.HttpContext?.GetEndpoint()?.DisplayName; // cms_api.Controllers.ProductsController.Create (cms_api)

            Console.WriteLine($"NoSingleQuoteActionFilter => {httpPath} {httpMethod} {projectRoot}");

            string[] allowdPaths = new string[] { "/v1/products", "/v1/propositions", "/v1/blogs", "/v1/notification", "/v1/faq", "/v1/file" };

            
            string[] allowdMethods = new string[] { "POST", "PUT" , "GET" };
            string allowedRootPath = "cms_api";

            if (!string.IsNullOrEmpty(httpPath) && !string.IsNullOrEmpty(httpMethod) && !string.IsNullOrEmpty(projectRoot) 
                && projectRoot.StartsWith(allowedRootPath)
                && allowdPaths.Any(allowedPath => httpPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
                && allowdMethods.Any(x => x.Contains(httpMethod)))
            {
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                if (IsCustomObject(argument))
                {
                    // Argument is a complex type, iterate through properties
                    var properties = argument.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        var value = Convert.ToString(property.GetValue(argument));
                        if (value.Contains("'"))
                        {
                            var badRequestResponse = new ResponseModel { Code = 400, Message = "The input should not contain single quotes." };
                            context.Result = new OkObjectResult(badRequestResponse);
                            return;
                        }
                    }
                }
                else
                {
                    // Argument is a primitive type, check for single quotes directly
                    var value = Convert.ToString(argument);
                    if (value.Contains("'"))
                    {
                        var badRequestResponse = new ResponseModel { Code = 400, Message = "The input should not contain single quotes." };
                        context.Result = new OkObjectResult(badRequestResponse);
                        return;
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing on action execution completion
        }

        private bool IsCustomObject(object obj)
        {

            if (obj != null && obj.GetType() != null)
            {
                var _namespace = obj.GetType().Namespace;

                if(!string.IsNullOrEmpty(_namespace) && (_namespace.StartsWith("cms_api") || _namespace.StartsWith("aia_core")))
                    return true;
            }

            return false;

        }
    }
}