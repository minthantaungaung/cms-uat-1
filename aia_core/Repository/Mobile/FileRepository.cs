using aia_core.Entities;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IFileRepository
    {
        ResponseModel<List<string>> GetFileList(List<AIAFIle> allFiles);
    }
    public class FileRepository : BaseRepository, IFileRepository
    {
        public FileRepository(IHttpContextAccessor httpContext
            , IAzureStorageService azureStorage
            , IErrorCodeProvider errorCodeProvider
            , IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
        }

        ResponseModel<List<string>> IFileRepository.GetFileList(List<AIAFIle> allFiles)
        {
            var productCoverImages = unitOfWork.GetRepository<Product>()
                .Query(x => x.IsActive == true && x.IsDelete == false)
                .Select(x => x.CoverImage)
                .ToList();

            if (productCoverImages?.Count > 0)
            {
                allFiles?
                    .Where(file => productCoverImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Product Cover Image";
                    });
            }

            var productLogoImages = unitOfWork.GetRepository<Product>()
                .Query(x => x.IsActive == true && x.IsDelete == false)
                .Select(x => x.LogoImage)
                .ToList();

            if (productLogoImages?.Count > 0)
            {
                allFiles?
                    .Where(file => productLogoImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Product Logo Image";
                    });
            }


            var CoverageIcons = unitOfWork.GetRepository<Coverage>()
            .Query(x => x.IsActive == true && x.IsDelete == false)
            .Select(x => x.CoverageIcon)
            .ToList();

            if (CoverageIcons?.Count > 0)
            {
                allFiles?
                    .Where(file => CoverageIcons.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Coverage Icon";
                    });
            }

            var BankLogos = unitOfWork.GetRepository<Bank>()
            .Query(x => x.IsActive == true && x.IsDelete == false)
            .Select(x => x.BankLogo)
            .ToList();

            if (BankLogos?.Count > 0)
            {
                allFiles?
                    .Where(file => BankLogos.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Bank Logo";
                    });
            }

            var BlogCoverImages = unitOfWork.GetRepository<Blog>()
                .Query(x => x.IsActive == true && x.IsDelete == false)
                .Select(x => x.CoverImage)
                .ToList();

            if (BlogCoverImages?.Count > 0)
            {
                allFiles?
                    .Where(file => BlogCoverImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Blog Cover Image";
                    });
            }

            var BlogThumbnailImages = unitOfWork.GetRepository<Blog>()
            .Query(x => x.IsActive == true && x.IsDelete == false)
            .Select(x => x.ThumbnailImage)
            .ToList();

            if (BlogThumbnailImages?.Count > 0)
            {
                allFiles?
                    .Where(file => BlogThumbnailImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Blog Thumbnail Image";
                    });
            }

            var PropositionLogoImages = unitOfWork.GetRepository<Proposition>()
            .Query(x => x.IsActive == true && x.IsDelete == false)
            .Select(x => x.LogoImage)
            .ToList();

            if (PropositionLogoImages?.Count > 0)
            {
                allFiles?
                    .Where(file => PropositionLogoImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Proposition Logo Image";
                    });
            }

            var PropositionBackgroudImages = unitOfWork.GetRepository<Proposition>()
                .Query(x => x.IsActive == true && x.IsDelete == false)
                .Select(x => x.BackgroudImage)
                .ToList();

            if (PropositionBackgroudImages?.Count > 0)
            {
                allFiles?
                    .Where(file => PropositionBackgroudImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Proposition Background Image";
                    });
            }

            var MemberNotificationImages = unitOfWork.GetRepository<MemberNotification>()
                 .Query(x => x.ImageUrl != null)
                 .Select(x => x.ImageUrl)
                 .ToList()
                 .Distinct()
                 .ToList();

            if (MemberNotificationImages?.Count > 0)
            {
                allFiles?
                    .Where(file => MemberNotificationImages.Contains(file.FileName))
                    .ToList()
                    ?.ForEach(file =>
                    {
                        file.FileType = "Member Notification Image";
                    });
            }

            allFiles?
                .RemoveAll(x => string.IsNullOrEmpty(x.FileType));

            //allFiles?.OrderBy(x => x.FileType).ToList();

            var result = allFiles?.OrderByDescending(x => x.SizeInBytes)
                .Select(x => $"{x.FileType}: {x.FileName} {x.SizeInBytes} {x.Size}")
                .ToList();

            return errorCodeProvider.GetResponseModel(ErrorCode.E0, result);
        }
    }

    public class AIAFIle
    {
        public string FileName { get; set; }
        public string Size { get; set; }
        public long SizeInBytes { get; set; }
        public string? FileType { get; set; }
    }
}
