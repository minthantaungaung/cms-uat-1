using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Services;
using aia_core.UnitOfWork;

using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;

namespace aia_core.Repository.Cms
{
    public interface IMemberRepository
    {
        Task<ResponseModel<PagedList<MemberListResponse>>> List(ListMemberRequest model);
        Task<ResponseModel<List<MemberListResponse>>> Export(ListMemberRequest model);
        Task<ResponseModel<MemberResponse>> Get(string AppRegMemberId);
        Task<ResponseModel<MemberResponse>> Update(UpdateMemberRequest model);
    }
    public class MemberRepository : BaseRepository, IMemberRepository
    {
        private readonly IOktaService oktaService;
        public MemberRepository(IOktaService oktaService, IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork) 
            :base(httpContext, azureStorage, errorCodeProvider, unitOfWork) 
        {
            this.oktaService = oktaService;
        }

        public async Task<ResponseModel<MemberResponse>> Get(string AppRegMemberId)
        {
            #region #Update MemberType & GroupMemberID
            try
            {
                var member = unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == Guid.Parse(AppRegMemberId)).FirstOrDefault();
                if (member != null)
                {
                    (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(Guid.Parse(AppRegMemberId));
                    member.MemberType = clientInfo.membertype;
                    member.GroupMemberID = clientInfo.groupMemberId;
                    //member.IndividualMemberID = clientInfo.memberID;
                    unitOfWork.SaveChanges();

                    var memberResponse = new MemberResponse
                    {
                        AppRegMemberId = member.MemberId.ToString(),
                        MemberName = member.Name,
                        MemberEmail = member.Email,
                        MemberPhone = member.Mobile,
                        MemberIdNrc = member.Nrc,
                        MemberIdPassport = member.Passport,
                        MemberIdOther = member.Others,
                        MemberIsActive = member.IsActive,
                        MemberDob = member.Dob,
                        MemberGender = member.Gender,
                        RegisterDate = member.RegisterDate,
                        LastActiveDate = member.LastActiveDate,
                        MemberType = clientInfo.membertype,
                        GroupMemberID = clientInfo.groupMemberId,
                        MemberId = clientInfo.memberID,
                        IndividualMemberID = clientInfo.memberID,
                    };

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Members,
                        objectAction: EnumObjectAction.View,
                        objectId: member.MemberId,
                        objectName: member.Name);

                    return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E0, memberResponse);
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E400);
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E500);
            }

            #endregion
        }

        public async Task<ResponseModel<PagedList<MemberListResponse>>> List(ListMemberRequest model)
        {
            var memberResponses = new List<MemberResponse>();

            try
            {
                model.QueryType = Model.Cms.Request.Common.EnumSqlQueryType.List;
                var queryStrings = PrepareListQuery(model);

                var count = unitOfWork.GetRepository<GetCountByRawQuery>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<MemberListResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();

                list?.ForEach(item => 
                {
                    item.IsVerified = item.IsVerified_Flag == true ? "Verified" : "Not Verified";
                    item.LastActiveDate = item.IsVerified_Flag == true ? item.LastActiveDate : null;
                });

                var data = new PagedList<MemberListResponse>(
                   source: list,
                   totalCount: count?.SelectCount ?? 0,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Members,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<MemberListResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<MemberListResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<MemberListResponse>>> Export(ListMemberRequest model)
        {

            

            try
            {
                model.QueryType = Model.Cms.Request.Common.EnumSqlQueryType.Export;
                var queryStrings = PrepareListQuery(model);

                Console.WriteLine($"Member Export Query => {queryStrings.ListQuery}");


                var list = unitOfWork.GetRepository<MemberListResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();
                Console.WriteLine($"Member Export Query => Result => {list?.Count}");

                

                return errorCodeProvider.GetResponseModel<List<MemberListResponse>>(ErrorCode.E0, list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Member Export Ex => {JsonConvert.SerializeObject(ex)}");


                return errorCodeProvider.GetResponseModel<List<MemberListResponse>>(ErrorCode.E500);
            }

        }

        public async Task<ResponseModel<MemberResponse>> Update(UpdateMemberRequest model)
        {
            try
            {
                CmsErrorLog($"MemberUpdate", "", JsonConvert.SerializeObject(model), httpContext?.HttpContext.Request.Path);

                var member = await unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == Guid.Parse(model.AppRegMemberId))
                    .Include(x => x.MemberClients).ThenInclude(x => x.Client).ThenInclude(x => x.PolicyHolder)
                    .Include(x => x.MemberClients).ThenInclude(x => x.Client).ThenInclude(x => x.PolicyInsured)
                    .FirstOrDefaultAsync();

                CmsErrorLog($"MemberUpdate", "i am here ", "" , httpContext?.HttpContext.Request.Path);

                if (member == null)
                    return new ResponseModel<MemberResponse> { Code = 400, Message = "No member found." };

                if (member.Mobile != model.MemberPhone || member.Email != model.MemberEmail)
                {
                    var isExistPhone = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => r.MemberId != Guid.Parse(model.AppRegMemberId) && r.IsActive == true && r.IsVerified == true && r.Mobile == model.MemberPhone).AnyAsync();
                    if (isExistPhone) return new ResponseModel<MemberResponse> { Code = 400, Message = "Phone number is used by another registered member." };

                    var isExistEmail = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => r.MemberId != Guid.Parse(model.AppRegMemberId) && r.IsActive == true && r.IsVerified == true && r.Email == model.MemberEmail).AnyAsync();
                    if (isExistEmail) return new ResponseModel<MemberResponse> { Code = 400, Message = "Email is used by another registered member." };
                }

                CmsErrorLog($"MemberUpdate", "i am here also", "", httpContext?.HttpContext.Request.Path);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new MemberResponse(member));

                #region #okta-api-call


                if (member.IsVerified == true
                    && !string.IsNullOrEmpty(member.Auth0Userid))
                {
                    CmsErrorLog($"MemberUpdate", "i am here also 123", "", httpContext?.HttpContext.Request.Path);

                    var oktaUser = await oktaService.GetUser(member.Auth0Userid);
                    //if (oktaUser?.Code != (int)ErrorCode.E0) return new ResponseModel<MemberResponse> { Code = 400, Message = "Unable to get okta user profile." };
                    if (oktaUser?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E400,oktaUser?.Message);


                    if (model.MemberIsActive != null)
                    {
                        if (model.MemberIsActive == false
                        && member.IsActive == true
                        && oktaUser?.Data?.status == "ACTIVE"

                        )
                        {
                            var suspendUser = await oktaService.SuspendUser(member.Auth0Userid);
                            if (suspendUser?.Code != (int)ErrorCode.E0) return new ResponseModel<MemberResponse> { Code = 400, Message = "Unable to suspend user." };
                        }
                        else if (model.MemberIsActive == true
                            && member.IsActive == false
                            && oktaUser?.Data?.status != "ACTIVE"
                            )
                        {
                            var unsuspendUser = await oktaService.UnsuspendUser(member.Auth0Userid);
                            if (unsuspendUser?.Code != (int)ErrorCode.E0) return new ResponseModel<MemberResponse> { Code = 400, Message = "Unable to unsuspend user." };
                        }
                    }
                    

                    if (model.MemberIsActive == true /*&& oktaUser?.Data?.status == "ACTIVE"*/
                        && (member.Mobile != model.MemberPhone || member.Email != model.MemberEmail))
                    {
                        CmsErrorLog($"MemberUpdate", "i am here also 1234", "", httpContext?.HttpContext.Request.Path);

                        var profileUpdate = await oktaService.UpdateUser(member.Auth0Userid, model.MemberEmail, model.MemberPhone);
                        //if (profileUpdate?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E500);
                        if (profileUpdate?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E500,profileUpdate?.Message);

                        CmsErrorLog($"MemberUpdate", "i am here also 12345", "", httpContext?.HttpContext.Request.Path);
                        var factors = await oktaService.ListEnrollFactors(member.Auth0Userid);
                        //if (factors?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E500);
                        if (factors?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E500,factors?.Message);

                        CmsErrorLog($"MemberUpdate", "i am here also 123456", "", httpContext?.HttpContext.Request.Path);
                        var emailFactor = factors?.Data.Where(r => r.factorType == "email").FirstOrDefault();
                        var smsFactor = factors?.Data.Where(r => r.factorType == "sms").FirstOrDefault();

                        CmsErrorLog($"smsFactor | {model.MemberPhone} | {JsonConvert.SerializeObject(smsFactor)}", "check sms factor", "", httpContext?.HttpContext.Request.Path);
                        if (smsFactor == null && member.Mobile != model.MemberPhone) // && condition added by KZM
                        {
                            var enrollSms = await oktaService.EnrollNewSMS(model.MemberPhone, member.Auth0Userid);
                            // if (enrollSms?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E500);
                            if (enrollSms?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E500,enrollSms?.Message);
                        }
                        else if (member.Mobile != model.MemberPhone
                            && smsFactor != null)
                        {
                            var unenrollSMS = await oktaService.UnenrollSMS(smsFactor.id, member.Auth0Userid);
                            //if (unenrollSMS?.Code != (int)ErrorCode.E0) return new ResponseModel<MemberResponse> { Code = 400, Message = "Unable to unenroll existing sms." };
                            if (unenrollSMS?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E400,unenrollSMS?.Message);

                            var enrollSms = await oktaService.EnrollNewSMS(model.MemberPhone, member.Auth0Userid);
                            //if (unenrollSMS?.Code != (int)ErrorCode.E0) return new ResponseModel<MemberResponse> { Code = 400, Message = "Unable to nroll new sms." };
                            if (enrollSms?.Code != (int)ErrorCode.E0) return errorCodeProvider.GetResponseModelCustom<MemberResponse>(ErrorCode.E400,enrollSms?.Message);
                        }
                    }
                }
                #endregion

                CmsErrorLog($"MemberUpdate", "updating to DB", "", httpContext?.HttpContext.Request.Path);
                member.Mobile = model.MemberPhone;
                member.Email = model.MemberEmail;
                member.IsActive = model.MemberIsActive;

                await unitOfWork.SaveChangesAsync();

                CmsErrorLog($"MemberUpdate", "updating to DB 123", "", httpContext?.HttpContext.Request.Path);

                var memberResponse = new MemberResponse(member);

                try
                {
                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Members,
                        objectAction: EnumObjectAction.Update,
                        objectId: member.MemberId,
                        objectName: member.Name,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(memberResponse));
                }
                catch (Exception ex)
                {
                    CmsErrorLog("MemberUpdate CmsAuditLog", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                }
                
                return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E0, memberResponse);
            }
            catch (Exception ex)
            {
                MobileErrorLog("MemberUpdate Exception", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                CmsErrorLog("MemberUpdate Exception", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<MemberResponse>(ErrorCode.E500);
            }
        }

        private aia_core.Repository.QueryStrings PrepareListQuery(ListMemberRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(Member.Member_ID) AS SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT 
                            Member.Member_ID AS AppRegMemberId,
                            Member.Name AS MemberName,
                            Member.Mobile AS MemberPhone,
                            Member.Email AS MemberEmail,
                            Member.NRC AS MemberIdNrc,
                            Member.Passport AS MemberIdPassport,
                            Member.Others AS MemberIdOther,
                            Member.Gender AS MemberGender,
                            Member.DOB AS MemberDob,
                            Member.Is_Active AS MemberIsActive,
                            Member.Register_Date AS RegisterDate,
                            Member.Last_Active_Date AS LastActiveDate,
                            Member.MemberType AS MemberType,
                            Member.GroupMemberID AS GroupMemberID,
                            Member.IndividualMemberID AS MemberId,
                            Member.IndividualMemberID AS IndividualMemberID, 
                            Member.Is_Verified AS IsVerified_Flag ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM Member ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"ORDER BY Member.Register_Date DESC ";
            var orderQueryForCount = @" ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"where Member.Is_Verified = 1 ";

            if (!string.IsNullOrEmpty(model.MemberId))
            {
                filterQuery += $@"AND (Member.IndividualMemberID LIKE '%{model.MemberId}%' OR Member.GroupMemberID LIKE '%{model.MemberId}%') ";
            }

            if (!string.IsNullOrEmpty(model.MemberName))
            {
                filterQuery += $@"AND Member.Name LIKE '%{model.MemberName}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberPhone))
            {
                filterQuery += $@"AND Member.Mobile LIKE '%{model.MemberPhone}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberEmail))
            {
                filterQuery += $@"AND Member.Email LIKE '%{model.MemberEmail}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberIden))
            {
                filterQuery += $@"AND (Member.NRC LIKE '%{model.MemberIden}%' OR Member.Passport LIKE '%{model.MemberIden}%' OR Member.Others LIKE '%{model.MemberIden}%') ";
            }

            if (model.Status != null)
            {
                int active = model.Status.Value ? 1 : 0;
                filterQuery += $@"AND Member.Is_Active = {active} ";

            }

            if (model.IsVerified != null)
            {
                int isVerified = model.IsVerified == true ? 1 : 0;

                filterQuery += $@"AND Member.Is_Verified = {isVerified} ";

            }

            if (model.MemberIdenType != null && model.MemberIdenType.Any())
            {
                

                var subQuery = new List<string>();

                foreach (var iden in  model.MemberIdenType)
                {
                    if (iden == EnumIdenType.Nrc)
                    {
                        subQuery.Add(@"Member.NRC IS NOT NULL AND Member.NRC <> '' ");
                    }
                    else if (iden == EnumIdenType.Passport)
                    {
                        subQuery.Add(@"Member.Passport IS NOT NULL AND Member.Passport <> '' ");
                    }
                    else if (iden == EnumIdenType.Others)
                    {
                        subQuery.Add(@"Member.Others IS NOT NULL AND Member.Others <> '' ");
                    }
                }

                var subQueryString = string.Join("OR ", subQuery);

                filterQuery += $@"AND ({subQueryString})";
            }

            if (model.MemberType != null)
            {
                if (model.MemberType == EnumIndividualMemberType.Ruby)
                {
                    filterQuery += $@"AND Lower(Member.MemberType) = '{EnumIndividualMemberType.Ruby.ToString().ToLower()}' ";
                }
                else if (model.MemberType == EnumIndividualMemberType.Member)
                {
                    filterQuery += $@"AND Lower(Member.MemberType) = '{EnumIndividualMemberType.Member.ToString().ToLower()}' ";
                }
            }


            
            #endregion

            #region #OffsetQuery

            #endregion
            var offsetQuery = "";
            if (model.QueryType == Model.Cms.Request.Common.EnumSqlQueryType.List)
            {
                offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";
            }

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQueryForCount}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new aia_core.Repository.QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }
    }
}
