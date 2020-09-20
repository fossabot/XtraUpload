﻿using System.Threading.Tasks;
using tusdotnet.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IO;
using tusdotnet.Interfaces;
using XtraUpload.Domain;
using XtraUpload.Domain.Infra;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using Microsoft.Extensions.Options;
using System;
using XtraUpload.StorageManager.Common;
using System.Text;
using Microsoft.Extensions.Logging;

namespace XtraUpload.StorageManager.Service
{
    /// <summary>
    /// A singleton class used to handle the tusdotnet file uploads
    /// XtraUpload uses the tus protocol to handle uploading large files efficiently
    /// </summary>
    public class FileUploadService : BaseFileUpload
    {
        public FileUploadService(IServiceProvider serviceProvider) : base(serviceProvider, "/fileupload")
        {
        }

        /// <summary>
        /// Handle the upload completion
        /// </summary>
        protected override async Task OnUploadCompleted(FileCompleteContext ctx)
        {
            try
            {
                UploadOptions uploadOpts = _serviceProvider.GetService<IOptionsMonitor<UploadOptions>>().CurrentValue;

                FileItem file = await PersistMetaData(ctx, uploadOpts);
                MoveFilesToFolder(ctx, file, uploadOpts);
            }
            catch (Exception _ex)
            {
                _logger.LogError(_ex.Message);
                throw _ex;
            }
        }
        /// <summary>
        /// Persist the uploaded file data to db
        /// </summary>
        private async Task<FileItem> PersistMetaData(FileCompleteContext ctx, UploadOptions uploadOpts)
        {
            string userId = ctx.HttpContext.User.GetUserId();
            ITusFile file = await ((ITusReadableStore)ctx.Store).GetFileAsync(ctx.FileId, ctx.CancellationToken);
            var metadata = await file.GetMetadataAsync(ctx.CancellationToken);

            FileItem fileitem = new FileItem()
            {
                Id = Helpers.GenerateUniqueId(),
                TusId = ctx.FileId,
                UserId = userId,
                Size = new FileInfo(Path.Combine(uploadOpts.UploadPath, file.Id)).Length,
                Name = metadata["name"].GetString(Encoding.UTF8),
                MimeType = metadata["contentType"].GetString(Encoding.UTF8),
                // file with no folderid is placed in the virtual root folder
                FolderId = metadata["folderId"].GetString(Encoding.UTF8) == "root"
                                                                                ? null
                                                                                : metadata["folderId"].GetString(Encoding.UTF8),
                Extension = Helpers.GetFileExtension(metadata["contentType"].GetString(Encoding.UTF8)),
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                IsAvailableOnline = true
            };

            // Todo: send file info to the main app
            // Add the uploaded file to db
            //using IServiceScope scope = _serviceProvider.CreateScope();
            //using IUnitOfWork unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
            //unitOfWork.Files.Add(fileitem);

            // Try to save to db
            //await unitOfWork.CompleteAsync();

            return fileitem;
        }

        /// <summary>
        /// Moves the uploaded files to the user folder
        /// tus protocol puts the uploaded files into the store, XtraUpload move those files to the user directory
        /// </summary>
        private void MoveFilesToFolder(FileCompleteContext ctx, FileItem fileItem, UploadOptions uploadOpts)
        {
            string userFolder = Path.Combine(uploadOpts.UploadPath, fileItem.UserId);
            string destFolder = Path.Combine(userFolder, fileItem.Id);
            string newFileFullPath = Path.Combine(destFolder, fileItem.Id);
            // Create user root directory
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }
            // Create a new directory inside the user root dir
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            // move all files to the destination folder
            DirectoryInfo directoryInfo = new DirectoryInfo(uploadOpts.UploadPath);
            foreach (FileInfo file in directoryInfo.GetFiles(ctx.FileId + "*"))
            {
                // Exemple of file names generated by tus are (...69375.metadata, ...69375.uploadlength ...)
                string[] subNames = file.Name.Split('.');
                string subName = subNames.Count() == 2 ? '.' + subNames[1] : string.Empty;
                File.Move(file.FullName, newFileFullPath + subName);
            }

            // Create thumbnails for img file (less than 15mb)
            if (fileItem.MimeType.StartsWith("image") && fileItem.Size < (1024L * 1024L * 15))
            {
                // Todo: move the process of cropping images to a job
                using FileStream smallThumboutStream = new FileStream(newFileFullPath + ".smallthumb.png", FileMode.Create);
                using Image image = Image.Load(File.ReadAllBytes(newFileFullPath), out IImageFormat format);
                if (image.Width >= 800 || image.Height >= 800)
                {
                    int width = 960, height = 640;
                    int aspectRatio = image.Width / image.Height;
                    if (aspectRatio == 0)
                    {
                        height = 960;
                        width = 640;
                    }
                    using FileStream mediumThumboutStream = new FileStream(newFileFullPath + ".mediumthumb.png", FileMode.Create);
                    Image mediumthumbnail = image.Clone(i => i.Resize(width, height).Crop(new Rectangle(0, 0, width, height)));
                    mediumthumbnail.Save(mediumThumboutStream, format);
                }

                Image smallthumbnail = image.Clone(i => i.Resize(128, 128).Crop(new Rectangle(0, 0, 128, 128)));
                smallthumbnail.Save(smallThumboutStream, format);
            }
        }
    }
}