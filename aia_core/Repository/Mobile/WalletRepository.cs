using aia_core.Entities;
using aia_core.Model.Mobile.Response.Wallet;
using aia_core.Services;
using aia_core.Services.AIA;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IWalletRepository
    {
        Task<ResponseModel<WalletBalanceResponseModel>> GetWalletBalance();
        Task<ResponseModel<List<WalletTransactionResponseModel>>> GetRewardTransactions();
    }
    public class WalletRepository : BaseRepository, IWalletRepository
    {
        private readonly IAiaWalletService aiaWalletService;
        public WalletRepository(IHttpContextAccessor httpContext, 
            IAzureStorageService azureStorage, 
            IErrorCodeProvider errorCodeProvider, 
            IUnitOfWork<Context> unitOfWork,
            IAiaWalletService aiaWalletService) 
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.aiaWalletService = aiaWalletService;
        }

        async Task<ResponseModel<List<WalletTransactionResponseModel>>> IWalletRepository.GetRewardTransactions()
        {
            var walletTransactionResponseModels = new List<WalletTransactionResponseModel>();
            var member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == GetMemberIDFromToken()).FirstOrDefault();
            if (member != null)
            {
                var masterClientId = GetMasterClientIdByMemberId(member.MemberId);
                var rewardTransactions = await aiaWalletService.GetRewardTransactions(masterClientId);
                if (rewardTransactions != null && rewardTransactions.code == 200 && rewardTransactions.result != null)
                {
                    walletTransactionResponseModels = rewardTransactions.result.Select(x => new WalletTransactionResponseModel
                    {
                        Points = x.amount,
                        TransactionDate = x.transactionDate.Date.AddHours(18),
                        TransactionType = x.transactionType.ToLower() == "Credit".ToLower() ? WalletTransactionType.Credit : WalletTransactionType.Debit,
                        CampaignDescription = x.campaignDescription
                    }).ToList();
                }
            }

            return errorCodeProvider.GetResponseModel<List<WalletTransactionResponseModel>>(ErrorCode.E0, walletTransactionResponseModels);            
        }

        async Task<ResponseModel<WalletBalanceResponseModel>> IWalletRepository.GetWalletBalance()
        {
            var walletBalanceResponseModel = new WalletBalanceResponseModel();

            var member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == GetMemberIDFromToken()).FirstOrDefault();

            if(member != null)
            {
                var masterClientId = GetMasterClientIdByMemberId(member.MemberId);
                var rewardBalance = await aiaWalletService.GetRewardBalance(masterClientId);
                if (rewardBalance != null && rewardBalance.code == 200 && rewardBalance.result != null)
                {
                    walletBalanceResponseModel = new WalletBalanceResponseModel
                    {
                        PointBalance = rewardBalance.result.balance,
                    };
                }
            }

            return errorCodeProvider.GetResponseModel<WalletBalanceResponseModel>(ErrorCode.E0, walletBalanceResponseModel);            
        }

        public Guid GetTestMemberId()
        {
            var memberId =
            unitOfWork.GetRepository<Member>()
            .Query(x => x.Email == "tinlinnnsoe@codigo.sg" && x.IsActive == true && x.IsVerified == true)
                .Select(x => x.MemberId)
                .FirstOrDefault();

            return memberId;
        }
    }
}
