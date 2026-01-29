using System.Linq.Expressions;
using System.Reflection;
using aia_core.Entities;
using aia_core.Model.Mobile.Request.Blog;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.Bank;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface IBankRepository
    {
        ResponseModel<List<BankModelResponse>> GetList();
        ResponseModel CheckBankInfoValidation(BankModelRequest model);
        ResponseModel SaveBankInfo(BankModelRequest model);
        ResponseModel<List<BankInfoResponse>> GetBankInfo();
        ResponseModel UpdateBankInfo(UpdateBankModelRequest model);
        ResponseModel<string> DeleteBankInfo(Guid id);
    }
    public class BankRepository : BaseRepository, IBankRepository
    {
        #region "const"
        private readonly ICommonRepository commonRepository;

        public BankRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
        }
        #endregion

        #region "get bank list"
        public ResponseModel<List<BankModelResponse>> GetList()
        {
            try
            {
                List<Bank> bankList = unitOfWork.GetRepository<Bank>()
                .Query(x => x.IsActive == true && x.IsDelete == false).ToList();

                List<BankModelResponse> data = new List<BankModelResponse>();
                foreach (var item in bankList)
                {

                    var bankLogo = "";
                    if (!string.IsNullOrEmpty(item.BankLogo))
                    {
                        bankLogo = GetFileFullUrl(EnumFileType.Bank, item.BankLogo);
                    }
                    else
                    {
                        var defaultCmsImage = unitOfWork.GetRepository<Entities.DefaultCmsImage>().Query().FirstOrDefault();

                        if(!string.IsNullOrEmpty(defaultCmsImage?.image_url))
                            bankLogo = GetFileFullUrl(defaultCmsImage.image_url);
                    }

                    data.Add(new BankModelResponse
                    {
                        ID = item.ID,
                        BankName = item.BankName,
                        BankName_MM = item.BankName_MM,
                        BankCode = item.BankCode,
                        BankLogo = bankLogo,
                    });
                }

                return errorCodeProvider.GetResponseModel<List<BankModelResponse>>(ErrorCode.E0,data);

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<BankModelResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        #region "Check Bank Info Validation"
        private ResponseModel CheckBankValidation(string AccountNumber, Bank bank)
        {
            #region "Account Number validation"
            if(bank.DigitType == EnumBankDigitType.Range.ToString())
            {
                if(AccountNumber.Length > bank.DigitEndRange || AccountNumber.Length < bank.DigitStartRange)
                {
                    return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number length should be {bank.DigitStartRange} to {bank.DigitEndRange}.");
                }
            }
            else if (bank.DigitType == EnumBankDigitType.OR.ToString())
            {
                List<int> customDigits = bank.DigitCustom.Split(',').Select(int.Parse).ToList();
                int bankAccLength = AccountNumber.Length;
                if(!customDigits.Contains(bankAccLength))
                {
                    return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number length should be {bank.DigitCustom.Replace(","," or")}.");
                }
            }
            else if (bank.DigitType == EnumBankDigitType.Custom.ToString())
            {
                List<int> customDigits = bank.DigitCustom.Split(',').Select(int.Parse).ToList();
                int bankAccLength = AccountNumber.Length;
                if(!customDigits.Contains(bankAccLength))
                {
                    return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number length should be {bank.DigitCustom.Replace(","," or")}.");
                }
            }

            if(bank.BankCode.Contains("YOMA"))
            {
                string extractedDigits = AccountNumber.Substring(4, 3);
                if(bank.AccountType == EnumBankAccountType.OnlySpecial.ToString())
                {
                    if(!(extractedDigits == "454" || extractedDigits == "111"))
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be special account number.");
                    }
                }
                else if(bank.AccountType == EnumBankAccountType.OnlySaving.ToString())
                {
                    if(!(extractedDigits == "441" || extractedDigits == "451" || extractedDigits == "102"))
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be saving account number.");
                    }
                }
            }
            else if(bank.BankCode.Contains("KBZ"))
            {
                string extractedDigits = AccountNumber.Substring(3, 3);
                List<string> substringsToCheck = new List<string> { "325", "326", "137", "138", "139", "140", "511", "512", "513" };


                if(bank.AccountType == EnumBankAccountType.OnlySpecial.ToString())
                {
                    bool containsSubstring = substringsToCheck.Any(sub => extractedDigits.Contains(sub));

                    if(!containsSubstring)
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be special account number.");
                    }
                }
                else if(bank.AccountType == EnumBankAccountType.OnlySaving.ToString())
                {
                    bool containsSubstring = substringsToCheck.Any(sub => extractedDigits.Contains(sub));

                    if(containsSubstring)
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be normal account number.");
                    }
                }
            }
            else if(bank.BankCode.Contains("CB"))
            {
                string extractedDigits = AccountNumber.Substring(4, 4);
                List<string> substringsToCheck = new List<string> { "1009", "6009"};


                if(bank.AccountType == EnumBankAccountType.OnlySpecial.ToString())
                {
                    bool containsSubstring = substringsToCheck.Any(sub => extractedDigits.Contains(sub));

                    if(!containsSubstring)
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be special account number.");
                    }
                }
                else if(bank.AccountType == EnumBankAccountType.OnlySaving.ToString())
                {
                    bool containsSubstring = substringsToCheck.Any(sub => extractedDigits.Contains(sub));

                    if(containsSubstring)
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E405, $"Bank account number should be normal account number.");
                    }
                }
            }

            #endregion
            return errorCodeProvider.GetResponseModel(ErrorCode.E0);
        }
        public ResponseModel CheckBankInfoValidation(BankModelRequest model)
        {
            try
            {
                Bank bank = unitOfWork.GetRepository<Bank>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.ID == model.BankId).FirstOrDefault();

                if(bank == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400); 

                #region "Account Number validation"
                return CheckBankValidation(model.AccountNumber, bank);
                #endregion

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel(ErrorCode.E500);
            }
        }
        #endregion

        #region "Save Bank Info"
        public ResponseModel SaveBankInfo(BankModelRequest model)
        {
            try
            {
                Bank bank = unitOfWork.GetRepository<Bank>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.ID == model.BankId).FirstOrDefault();

                if(bank == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400); 

                ResponseModel checkResult = CheckBankValidation(model.AccountNumber, bank);
                if(checkResult.Code != 0)
                {
                    return checkResult;
                }

                var entity = new Entities.MemberBank
                {
                    ID = Guid.NewGuid(),
                    MemberID = commonRepository.GetMemberIDFromToken()??new Guid(),
                    BankID = bank.ID,
                    AccountHolderName = model.AccountHolderName,
                    AccountNumber = model.AccountNumber,
                    CreatedDate = Utils.GetDefaultDate()
                };

                unitOfWork.GetRepository<Entities.MemberBank>().Add(entity);
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0);

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel(ErrorCode.E500);
            }
        }
        #endregion

        #region "Get Bank Info"
        public ResponseModel<List<BankInfoResponse>> GetBankInfo()
        {
            try
            {
                Guid? memberID = commonRepository.GetMemberIDFromToken();
                List<MemberBank> bankList = unitOfWork.GetRepository<MemberBank>()
                .Query(x=> x.MemberID == memberID)
                .Include(i=>i.Bank).ToList();

                List<BankInfoResponse> data = new List<BankInfoResponse>();
                foreach (var item in bankList)
                {
                    var bankLogo = "";
                    if (!string.IsNullOrEmpty(item.Bank?.BankLogo))
                    {
                        bankLogo = GetFileFullUrl(EnumFileType.Bank, item.Bank.BankLogo);
                    }
                    else
                    {
                        var defaultCmsImage = unitOfWork.GetRepository<Entities.DefaultCmsImage>().Query().FirstOrDefault();                        

                        if (!string.IsNullOrEmpty(defaultCmsImage?.image_url))
                            bankLogo = GetFileFullUrl(defaultCmsImage.image_url);
                    }

                    data.Add(new BankInfoResponse
                    {
                        ID = item.ID,
                        BankID = item.BankID,
                        BankName = item.Bank.BankName,
                        BankName_MM = item.Bank.BankName_MM,
                        BankCode = item.Bank.BankCode,
                        AccountHolderName = item.AccountHolderName,
                        AccountNumber = item.AccountNumber,
                        BankLogo = bankLogo
                    });
                }

                return errorCodeProvider.GetResponseModel<List<BankInfoResponse>>(ErrorCode.E0,data);

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<BankInfoResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        #region "Update Bank Info"
        public ResponseModel UpdateBankInfo(UpdateBankModelRequest model)
        {
            try
            {
                Guid? memberID = commonRepository.GetMemberIDFromToken();
                MemberBank memberBank = unitOfWork.GetRepository<MemberBank>()
                .Query(x => x.ID == model.ID)
                .Include(i=>i.Bank).FirstOrDefault();

                if(memberBank == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400); 

                Bank bank = unitOfWork.GetRepository<Bank>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.ID == model.BankId).FirstOrDefault();

                if(bank == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400); 

                ResponseModel checkResult = CheckBankValidation(model.AccountNumber, bank);
                if(checkResult.Code != 0)
                {
                    return checkResult;
                }

                memberBank.MemberID = commonRepository.GetMemberIDFromToken()??new Guid();
                memberBank.BankID = model.BankId;
                memberBank.AccountHolderName= model.AccountHolderName;
                memberBank.AccountNumber = model.AccountNumber;
                memberBank.UpdatedDate = Utils.GetDefaultDate();

                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0);

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel(ErrorCode.E500);
            }
        }
        #endregion

         #region "Update Bank Info"
        public ResponseModel<string> DeleteBankInfo(Guid id)
        {
            try
            {
                Guid? memberID = commonRepository.GetMemberIDFromToken();
                MemberBank memberBank = unitOfWork.GetRepository<MemberBank>()
                .Query(x => x.ID == id).FirstOrDefault();


                if(memberBank == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400); 

                unitOfWork.GetRepository<MemberBank>().Delete(memberBank);
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);

            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }
        #endregion
    }
}