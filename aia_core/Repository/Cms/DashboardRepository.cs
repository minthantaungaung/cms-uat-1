using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using aia_core.Model.Cms.Response.Dashboard;
using aia_core.Model.Cms.Request.Dashboard;

namespace aia_core.Repository.Cms
{
    public interface IDashboardRepository
    {
       Task<ResponseModel<DashboardChartResponse>> GetChartByClaim(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByProduct(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByPerformance(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByClaimStatus(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByFailLog(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByServiceType(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByServicePerformance(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByServiceStatus(DashboardChartRequest model);
       Task<ResponseModel<DashboardChartResponse>> GetChartByServiceFailLog(DashboardChartRequest model);
       Task<ResponseModel<DashboardTotalResponse>> GetChartTotal(DashboardChartTotalRequest model);
    }

    public class DashboardRepository : BaseRepository, IDashboardRepository
    {
        #region "const"
        private readonly IDBCommonRepository dbCommonRepository;
        private readonly IErrorCodeProvider errorCodeProvider;
        public DashboardRepository(IHttpContextAccessor httpContext,
            IAzureStorageService azureStorage,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, 
            IDBCommonRepository dbCommonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.dbCommonRepository = dbCommonRepository;
            this.errorCodeProvider = errorCodeProvider;
        }
        #endregion

        #region "Claim Chart"
        public async Task<ResponseModel<DashboardChartResponse>> GetChartByClaim(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByClaimType", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.name))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.FirstOrDefault().name;
                    c.type = item.FirstOrDefault().name;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        string _status = i.status;
                        if(i.status == "Not-approved" || i.status == "Not approved")
                        {
                            _status = "Rejected";
                        }
                        else if(i.status == "Reject")
                        {
                            _status = "Rejected";
                        }
                        else if(i.status == "Withdraw")
                        {
                            _status = "Withdrawn";
                        }
                        else if(i.status == "Pending")
                        {
                            _status = "Pending >= 3 days";
                        }
                        else if(i.status == "PaymentPending")
                        {
                            _status = "Payment Pending >= 3 days";
                        }
                        c.status.Add(new ChartResponseStatus(){
                            name = _status,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByProduct(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByProductType", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.name))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.FirstOrDefault().name;
                    c.type = item.FirstOrDefault().code;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        c.status.Add(new ChartResponseStatus(){
                            name = i.status,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByPerformance(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByPerformance", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.name))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.FirstOrDefault().name;
                    c.type = item.FirstOrDefault().name;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        c.status.Add(new ChartResponseStatus(){
                            name = i.status,
                            type = i.status.Replace(" ",""),
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                var avgData = dbCommonRepository.GetListBySP<DashboardBClaimDecisionAvg>("sp_CMS_GetDashboardBClaimDecisionAvg", parameters);
                res.avgReceivedDecisionTime = avgData.FirstOrDefault().claimdecision.ToString();
                res.avgReceivedPaidTime = avgData.FirstOrDefault().paiddecision.ToString();

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByClaimStatus(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByClaimStatus", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.status))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    if(item.FirstOrDefault().status == "Pending")
                    {
                        c.name = "Pending >= 3 days";
                    }
                    else if(item.FirstOrDefault().status == "PaymentPending")
                    {
                        c.name = "Payment Pending >= 3 days";
                    }
                    else if (item.FirstOrDefault().status == "Not-approved" || item.FirstOrDefault().status == "Not approved")
                    {
                        c.name = "Rejected";
                    }
                    else if (item.FirstOrDefault().status == "Reject")
                    {
                        c.name = "Rejected";
                    }
                    else if (item.FirstOrDefault().status == "Withdraw")
                    {
                        c.name = "Withdrawn";
                    }
                    else
                    {
                        c.name = item.FirstOrDefault().status;
                    }
                    c.type = item.FirstOrDefault().status;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        c.status.Add(new ChartResponseStatus(){
                            name = i.status,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByFailLog(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("[sp_CMS_GetDashboardByFailLog]", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.Where(x=>x.status == "fail"))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.name;
                    c.type = item.name;
                    c.totalCount = item.count;
                    c.status = new List<ChartResponseStatus>();
                    c.status.Add(new ChartResponseStatus(){
                            name = item.status,
                            type = item.status,
                            count = item.count
                        });
                    res.response.Add(c);
                }
                res.claimFailLog = new ClaimFailLog();
                int failCount = data.Where(x=> x.status != "success").Sum(s=>s.count)??0;
                int successCount = data.Where(x=> x.status == "success").Sum(s=>s.count)??0;

                double total = successCount + failCount;

                double successPercentage = Math.Round((successCount / total) * 100, 2);
                double failPercentage = Math.Round((failCount / total) * 100, 2);

                res.claimFailLog.failCount = failCount;
                res.claimFailLog.successCount = successCount;

                if(total!=0)
                {
                    res.claimFailLog.failPercentage = failPercentage;
                    res.claimFailLog.successPercentage = successPercentage;
                }
                else
                {
                    res.claimFailLog.failPercentage = 0;
                    res.claimFailLog.successPercentage = 0;
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }
        #endregion

        #region "Service Chart"
        public async Task<ResponseModel<DashboardChartResponse>> GetChartByServiceType(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByServiceType", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.name))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.FirstOrDefault().name;
                    c.type = item.FirstOrDefault().code;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        string _status = i.status;
                        if(i.status == "Not-approved" || i.status == "Not approved")
                        {
                            _status = "Rejected";
                        }
                        else if(i.status == "Reject")
                        {
                            _status = "Rejected";
                        }
                        else if(i.status == "Withdraw")
                        {
                            _status = "Withdrawn";
                        }
                        // else if(i.status == "Pending")
                        // {
                        //     _status = "Pending >= 3 days";
                        // }
                        else if(i.status == "PaymentPending")
                        {
                            _status = "Payment Pending >= 3 days";
                        }
                        c.status.Add(new ChartResponseStatus(){
                            name = _status,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByServicePerformance(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByServicePerformance", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.name))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.FirstOrDefault().name;
                    c.type = item.FirstOrDefault().code;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        c.status.Add(new ChartResponseStatus(){
                            name = i.code,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                var avgData = dbCommonRepository.GetListBySP<DashboardBClaimDecisionAvg>("sp_CMS_GetDashboardBServiceDecisionAvg", parameters);
                res.avgReceivedDecisionTime = avgData.FirstOrDefault().claimdecision.ToString();
                res.avgReceivedPaidTime = avgData.FirstOrDefault().paiddecision.ToString();

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByServiceStatus(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("sp_CMS_GetDashboardByServiceStatus", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.GroupBy(g=>g.status))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    // if(item.FirstOrDefault().status == "Pending")
                    // {
                    //     c.name = "Pending >= 3 days";
                    // }
                    if(item.FirstOrDefault().status == "PaymentPending")
                    {
                        c.name = "Payment Pending >= 3 days";
                    }
                    else if (item.FirstOrDefault().status == "Not-approved" || item.FirstOrDefault().status == "Not approved")
                    {
                        c.name = "Rejected";
                    }
                    else if (item.FirstOrDefault().status == "Reject")
                    {
                        c.name = "Rejected";
                    }
                    else if (item.FirstOrDefault().status == "Withdraw")
                    {
                        c.name = "Withdrawn";
                    }
                    else
                    {
                        c.name = item.FirstOrDefault().status;
                    }
                    c.type = item.FirstOrDefault().status;
                    c.totalCount = item.Sum(s=>s.count);
                    c.status = new List<ChartResponseStatus>();
                    foreach (var i in item)
                    {
                        c.status.Add(new ChartResponseStatus(){
                            name = i.status,
                            type = i.status,
                            count = i.count
                        });
                    }
                    res.response.Add(c);
                }

                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }

        public async Task<ResponseModel<DashboardChartResponse>> GetChartByServiceFailLog(DashboardChartRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardCommonModel>("[sp_CMS_GetDashboardByServiceFailLog]", parameters);

                DashboardChartResponse res = new DashboardChartResponse();
                res.totalCount = data.Sum(s=> s.count)??0;
                res.response = new List<ChartResponseModel>();
                foreach (var item in data.Where(x=>x.status != "success"))
                {
                    ChartResponseModel c = new ChartResponseModel();
                    c.name = item.name;
                    c.type = item.name;
                    c.totalCount = item.count;
                    c.status = new List<ChartResponseStatus>();
                    c.status.Add(new ChartResponseStatus(){
                            name = item.status,
                            type = item.status,
                            count = item.count
                        });
                    res.response.Add(c);
                }
                res.claimFailLog = new ClaimFailLog();

                int failCount = data.Where(x=> x.status != "success").Sum(s=>s.count)??0;
                int successCount = data.Where(x=> x.status == "success").Sum(s=>s.count)??0;


                double total = successCount + failCount;

                double successPercentage = Math.Round((successCount / total) * 100, 2);
                double failPercentage = Math.Round((failCount / total) * 100, 2);

                res.claimFailLog.failCount = failCount;
                res.claimFailLog.successCount = successCount;

                if(total!=0)
                {
                    res.claimFailLog.failPercentage = failPercentage;
                    res.claimFailLog.successPercentage = successPercentage;
                }
                else
                {
                    res.claimFailLog.failPercentage = 0;
                    res.claimFailLog.successPercentage = 0;
                }
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E0,res);
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardChartResponse>(ErrorCode.E400);
            }
        }
        #endregion

        public async Task<ResponseModel<DashboardTotalResponse>> GetChartTotal(DashboardChartTotalRequest model)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["@StartDate"] = model.StartDate;
                parameters["@EndDate"] = model.EndDate;
                var data = dbCommonRepository.GetListBySP<DashboardTotalResponse>("sp_CMS_GetDashboardTotal", parameters);

                return errorCodeProvider.GetResponseModel<DashboardTotalResponse>(ErrorCode.E0,data.FirstOrDefault());
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<DashboardTotalResponse>(ErrorCode.E400);
            }
        }
    }
}