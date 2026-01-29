using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Reflection.Metadata;
using static System.Reflection.Metadata.BlobBuilder;

namespace aia_core.Services
{
    public interface IAzureStorageService
    {
        Task<ResponseModel<BlobContentInfo>> UploadAsync(string blobName, IFormFile file);

        Task<ResponseModel<BlobContentInfo>> UploadBase64Async(string blobName, byte[] data);

        Task<string> GetUrl(string blobName);
        Task<string> GetUrlFromPrivate(string blobName);
        Task<string> GetUrlFromPrivateDefault(string blobName);
        Task<ResponseModel<string>> DownloadAsync(string blobName);
        //Task<ResponseModel<BlobContentInfo>> DeleteAsync(string blobName);
        Task<bool> DeleteAsync(string blobName);
        Task<string> GetBase64ByFileName(string blobName);
        Task<byte[]> GetByteByFileName(string blobName);

        Task RenameFile(string oldFileName, string newFileName);
    }
    public class AzureStorageService : IAzureStorageService
    {
        private readonly string connectionString = "";
        private readonly string containerName = "";
        private readonly string accountName = "";
        private readonly string accountKey = "";
        private readonly string clientId = "";
        private readonly string env = "";
        private readonly string volumeMothPath = "";
        private readonly string baseUrl = "";
        public AzureStorageService(IConfiguration config)
        {
            connectionString = config["Azure:BlobStorage:connectionString"] ?? throw new NullReferenceException("azure blob storage connectionString");
            containerName = config["Azure:BlobStorage:containerName"] ?? throw new NullReferenceException("azure blob storage containerName");
            accountName = config["Azure:BlobStorage:accountName"] ?? throw new NullReferenceException("azure blob storage accountName");
            accountKey = config["Azure:BlobStorage:accountKey"] ?? throw new NullReferenceException("azure blob storage accountKey");
            clientId = config["Azure:BlobStorage:clientId"] ?? throw new NullReferenceException("azure blob storage clientId");
            env = config["Env"] ?? throw new NullReferenceException("config");
            volumeMothPath = config["volumeMothPath"] ?? throw new NullReferenceException("volumeMothPath");
            baseUrl = config["baseUrl"] ?? throw new NullReferenceException("baseUrl");
        }

        private BlobContainerClient GetBlobContainerClient()
        {
            // if (env != "Development")
            // {
            //     var blobServiceClient = new BlobServiceClient(new Uri($"https://{accountKey}.blob.core.windows.net"), new DefaultAzureCredential(new DefaultAzureCredentialOptions
            //     {
            //         ManagedIdentityClientId = clientId,
            //     }));
            //     var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            //     return containerClient;
            // }
            // else
            // {
            //     BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            //     return containerClient;
            // }
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            return containerClient;
        }

        public byte[] ConvertIFormFileToByteArray(IFormFile file)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public async Task<ResponseModel<BlobContentInfo>> UploadAsync(string blobName, IFormFile file)
        {
            try
            {
                if(env == "Development")
                {
                    //BlobContainerClient container = new BlobContainerClient(connectionString, containerName);
                    BlobContainerClient container = GetBlobContainerClient();
                    BlobClient client = container.GetBlobClient(blobName);

                    await using (Stream? data = file.OpenReadStream())
                    {
                        var result = await client.UploadAsync(data, true);
                        return new ResponseModel<BlobContentInfo>
                        {
                            Code = 200,
                            Message = "success",
                            Data = result.Value
                        };
                    }
                }
                else
                {
                    try
                    {
                        // Combine the mounted path with the file name
                        string filePath = Path.Combine(volumeMothPath, blobName);

                        Console.WriteLine($"UploadAsync > {filePath}");

                        byte[] imageData = ConvertIFormFileToByteArray(file);
                        // Write the image data to the file
                        File.WriteAllBytes(filePath, imageData);
                        
                        Console.WriteLine($"UploadAsync success!!");

                        return new ResponseModel<BlobContentInfo>
                        {
                            Code = 200,
                            Message = "success"
                        };
                    }
                    catch (Exception ex)
                    {
                        // Handle any exceptions that may occur during the upload process
                        Console.WriteLine($"Error volume uploading image: {ex.Message}");
                        return new ResponseModel<BlobContentInfo> { Code = 500, Message = $"{ex.Message} | {ex?.InnerException?.Message}" };
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload Error : {ex}");
                return new ResponseModel<BlobContentInfo> { Code = 500, Message = $"{ex.Message} | {ex?.InnerException?.Message}" };
            }
        }
        public async Task<string> GetUrl(string blobName)
        {
            try
            {
                //BlobContainerClient client = new BlobContainerClient(connectionString, containerName);
                BlobContainerClient client = GetBlobContainerClient();
                BlobClient file = client.GetBlobClient(blobName);

                return $"{file?.Uri}";
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        public async Task<string> GetUrlFromPrivate(string blobName)
        {
            try
            {
                if(env == "Development")
                {
                     //BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new StorageSharedKeyCredential(accountName, accountKey));
                    //BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    BlobContainerClient blobContainerClient = GetBlobContainerClient();

                    BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                    // Generate a SAS token for the blob
                    BlobSasBuilder sasBuilder = new BlobSasBuilder()
                    {
                        BlobContainerName = containerName,
                        BlobName = blobName,
                        Resource = "b", // 'b' indicates blob
                        ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), // Set expiration time
                    };

                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    // Generate the SAS token
                    string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey)).ToString();

                    // Construct the full public URL
                    string publicImageUrl = $"{blobClient.Uri}?{sasToken}";

                    Console.WriteLine($"Public Image URL: {publicImageUrl}");
                    return publicImageUrl;
                }
                else
                {
                    try
                    {

                        Console.WriteLine($"GetUrlFromPrivate > {volumeMothPath} {blobName}");

                        // Combine the mounted path with the file name
                        string filePath = Path.Combine(volumeMothPath, blobName);

                        if (System.IO.File.Exists(filePath))
                        {
                            var fullFilePath = Path.Combine(baseUrl, blobName);
                            Console.WriteLine($"GetUrlFromPrivate > fullFilePath {fullFilePath}");

                            return fullFilePath;
                        }
                        else
                        {

                            Console.WriteLine($"GetUrlFromPrivate > fullFilePath empty!");

                            return "";
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"GetUrlFromPrivate Error : {ex}");
                        return "";
                    }
                }
               
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetUrlFromPrivate : {ex}");
                return "";
            }
        }

        public async Task<string> GetUrlFromPrivateDefault(string blobName)
        {
            try
            {
                // var managedIdentityCredential = new DefaultAzureCredential();

                // var blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), managedIdentityCredential);
                // var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobContainerClient blobContainerClient = GetBlobContainerClient();

                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                // Generate a SAS token for the blob
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // 'b' indicates blob
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), // Set expiration time
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate the SAS token string
                string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, null)).ToString();

                // Construct the full public URL
                string publicImageUrl = $"{blobClient.Uri}?{sasToken}";
                return publicImageUrl;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetUrlFromPrivateDefault : {ex}");
                return "";
            }

        }
        public async Task<ResponseModel<string>> DownloadAsync(string blobName)
        {
            try
            {
                //BlobContainerClient client = new BlobContainerClient(connectionString, containerName);
                BlobContainerClient client = GetBlobContainerClient();
                BlobClient file = client.GetBlobClient(blobName);

                bool exist = await file.ExistsAsync();
                return new ResponseModel<string>
                {
                    Code = exist ? 200 : 400,
                    Message = exist ? "success" : "failed",
                    Data = $"{file.Uri}/{file.Name}"
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel<string> { Code = 500, Message = $"{ex.Message} | {ex?.InnerException?.Message}" };
            }

        }

        public async Task<bool> DeleteAsync(string blobName)
        {
            try
            {
                if(env == "Development")
                {
                    //BlobContainerClient client = new BlobContainerClient(connectionString, containerName);
                    BlobContainerClient client = GetBlobContainerClient();
                    BlobClient file = client.GetBlobClient(blobName);

                    var result = await file.DeleteAsync();
                    return true;
                }
                else
                {
                    // Combine the mounted path with the file name
                    string filePath = Path.Combine(volumeMothPath, blobName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                //return new ResponseModel<BlobContentInfo> { Code = 500, Message = $"{ex.Message} | {ex?.InnerException?.Message}" };
                return true;
            }

        }

        public async Task<string> GetBase64ByFileName(string blobName)
        {
            try
            {
                if(env == "Development")
                {
                    BlobContainerClient blobContainerClient = GetBlobContainerClient();

                    BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
                    if(await blobClient.ExistsAsync())
                    {
                        Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();

                        // Read the stream into a byte array
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            await response.Value.Content.CopyToAsync(memoryStream);

                            // Convert the byte array to Base64
                            byte[] bytes = memoryStream.ToArray();
                            string base64String = Convert.ToBase64String(bytes);

                            return base64String;
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    try
                    {
                        // Combine the mounted path with the file name
                        string filePath = Path.Combine(volumeMothPath, blobName);

                        if (File.Exists(filePath))
                        {
                            // Read the file content as a byte array
                            byte[] fileBytes = File.ReadAllBytes(filePath);

                            // Convert the byte array to Base64
                            string base64String = Convert.ToBase64String(fileBytes);
                            return base64String;
                        }
                        else
                        {
                            return "";
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"GetUrlFromPrivate Error : {ex}");
                        return "";
                    }
                }
               
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetUrlFromPrivate : {ex}");
                return "";
            }
        }

        public async Task<byte[]> GetByteByFileName(string blobName)
        {
            try
            {
                if(env == "Development")
                {
                    BlobContainerClient blobContainerClient = GetBlobContainerClient();

                    BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
                    if(await blobClient.ExistsAsync())
                    {
                        Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();

                        // Read the stream into a byte array
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            await response.Value.Content.CopyToAsync(memoryStream);

                            // Convert the byte array to Base64
                            byte[] bytes = memoryStream.ToArray();

                            return bytes;
                        }
                    }
                    else
                    {
                        return new byte[0];
                    }
                }
                else
                {
                    try
                    {
                        // Combine the mounted path with the file name
                        string filePath = Path.Combine(volumeMothPath, blobName);

                        if (File.Exists(filePath))
                        {
                            // Read the file content as a byte array
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            return fileBytes;
                        }
                        else
                        {
                            return new byte[0];
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"GetUrlFromPrivate Error : {ex}");
                        return new byte[0];
                    }
                }
               
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetUrlFromPrivate : {ex}");
                return new byte[0];
            }
        }

        public async Task<ResponseModel<BlobContentInfo>> UploadBase64Async(string blobName, byte[] data)
        {
            try
            {
                // Combine the mounted path with the file name
                string filePath = Path.Combine(volumeMothPath, blobName);

                // Write the image data to the file
                File.WriteAllBytes(filePath, data);
                return new ResponseModel<BlobContentInfo>
                {
                    Code = 200,
                    Message = "success"
                };
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the upload process
                Console.WriteLine($"Error volume uploading image: {ex.Message}");
                return new ResponseModel<BlobContentInfo> { Code = 500, Message = $"{ex.Message} | {ex?.InnerException?.Message}" };
            }
        }

        public async Task RenameFile(string oldFileName, string newFileName)
        {
            try
            {
                string newFilePath = Path.Combine(volumeMothPath, newFileName);
                string oldFilePath = Path.Combine(volumeMothPath, oldFileName);     
                
                File.Move(oldFilePath, newFilePath);

                Console.WriteLine($"File renamed from {oldFilePath} to {newFilePath}");
            }
            catch (Exception ex)
            {                
                Console.WriteLine($"Error rename image: {ex.Message}");
            }
        }
    }
}
