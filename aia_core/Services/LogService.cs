using aia_core.Entities;
using aia_core.UnitOfWork;
using Hangfire.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface ILogService
    {
        public void LogError(Guid claimId, Exception ex);

        public void InsertOcrResponse(ClaimDocumentsMedicaBillApiLog entity);
    }
    public class LogService : ILogService
    {
        private readonly IServiceProvider _serviceProvider;

        public LogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void LogError(Guid claimId, Exception ex)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<aia_core.Entities.Context>>();

                    var claim = unitOfWork.GetRepository<ClaimTran>().Query(x => x.ClaimId == claimId).FirstOrDefault();
                    if (claim != null)
                    {
                        claim.IlerrorMessage = $"{claim.IlerrorMessage} OCR API => {JsonConvert.SerializeObject(ex)}";
                        unitOfWork.SaveChanges();
                    }
                }
            }
            catch (Exception exce)
            {
                Console.WriteLine($"#MedicalBill Data Saving error => {claimId} {exce.Message} {JsonConvert.SerializeObject(exce)}");
            }
        }

        public void InsertOcrResponse(ClaimDocumentsMedicaBillApiLog entity)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<aia_core.Entities.Context>>();
                unitOfWork.GetRepository<ClaimDocumentsMedicaBillApiLog>().Add(entity);
                unitOfWork.SaveChanges();
            }
        }
    }

}
