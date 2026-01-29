using aia_core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services.AIA
{
    public interface IAiaWalletService
    {
        Task<RewardTransactionResponseModel> GetRewardTransactions(string masterClientId);
        Task<RewardBalanceResponseModel> GetRewardBalance(string masterClientId);
    }
    public class AiaWalletService : IAiaWalletService
    {
        private string baseUrl = "";
        public AiaWalletService()
        {
            baseUrl = AppSettingsHelper.GetSetting("AIAWalletService:BaseUrl") 
                ?? "http://10.217.144.18/v1";
        }
        async Task<RewardBalanceResponseModel> IAiaWalletService.GetRewardBalance(string masterClientId)
        {
            var responseModel = new RewardBalanceResponseModel();
            
            try
            {
                string url = $"{baseUrl}/rewards/balance?clientId={masterClientId}";

                Console.WriteLine($"--- GetRewardBalance Making GET request to: {masterClientId} {url} ---");

                using HttpClient client = new HttpClient();                
                using HttpResponseMessage response = await client.GetAsync(url);   
                
                //response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
               
                Console.WriteLine($"--- GetRewardBalance Response Body: {masterClientId} {response.StatusCode} {responseBody} ---");

                if(response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseBody))
                {
                    responseModel = System.Text.Json.JsonSerializer.Deserialize<RewardBalanceResponseModel>(responseBody);
                    if(responseModel != null)
                        return responseModel;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"--- GetRewardBalance Request error: {masterClientId} {ex.Message} {ex.StackTrace} ---");
                
            }

            return responseModel;
        }

        async Task<RewardTransactionResponseModel> IAiaWalletService.GetRewardTransactions(string masterClientId)
        {
            var responseModel = new RewardTransactionResponseModel();

            try
            {
                string url = $"{baseUrl}/rewards/transactions?clientId={masterClientId}";

                Console.WriteLine($"--- GetRewardTransactions Making GET request to: {masterClientId} {url} ---");

                using HttpClient client = new HttpClient();
                using HttpResponseMessage response = await client.GetAsync(url);

                //response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"--- GetRewardTransactions Response Body: {masterClientId} {response.StatusCode} {responseBody} ---");

                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseBody))
                {
                    responseModel = System.Text.Json.JsonSerializer.Deserialize<RewardTransactionResponseModel>(responseBody);
                    if (responseModel != null)
                        return responseModel;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"--- GetRewardTransactions Request error: {masterClientId} {ex.Message} {ex.StackTrace} ---");

            }

            return responseModel;
        }
    }
}
