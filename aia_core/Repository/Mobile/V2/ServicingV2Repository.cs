using aia_core.Entities;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile.V2
{
    public interface IServicingV2Repository
    {
        bool IsOtpRequired(EnumServiceType serviceType);
    }
    public class ServicingV2Repository : BaseRepository, IServicingV2Repository
    {
        public ServicingV2Repository(IHttpContextAccessor httpContext, 
            IAzureStorageService azureStorage, 
            IErrorCodeProvider errorCodeProvider, 
            IUnitOfWork<Context> unitOfWork) 
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
        }

        bool IServicingV2Repository.IsOtpRequired(EnumServiceType serviceType)
        {

            return 
            unitOfWork.GetRepository<Claim_Service_Otp_Setup>()
           .Query(x => x.FormName == serviceType.ToString() && x.FormType == "Service")
           .Select(x => x.IsOtpRequired)
              .FirstOrDefault();
            
        }
    }
}
