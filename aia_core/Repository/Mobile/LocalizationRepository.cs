using aia_core.Entities;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Response.Localization;
using aia_core.Repository.Cms;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface ILocalizationRepository
    {
        ResponseModel<Dictionary<string, LableValue>> List();
    }
    public class LocalizationRepository : BaseRepository, ILocalizationRepository
    {
        public LocalizationRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        { }

        ResponseModel<Dictionary<string, LableValue>> ILocalizationRepository.List()
        {
            try
            {
                StringBuilder lableList = new StringBuilder();

                var lable = "";

                var localizations = unitOfWork.GetRepository<Entities.Localization>().Query(x => x.IsDeleted == false).ToList();

                if (!localizations.Any())
                {
                    return errorCodeProvider.GetResponseModel<Dictionary<string, LableValue>>(ErrorCode.E404);
                }

                var dict = localizations.ToDictionary(item => item.Key,
                        item => new LableValue { English = item.English, Burmese = item.Burmese }
                    );

                return errorCodeProvider.GetResponseModel<Dictionary<string, LableValue>>(ErrorCode.E0, dict);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<Dictionary<string, LableValue>>(ErrorCode.E500);
            }
        }
    }
}
