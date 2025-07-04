using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using File = System.IO.File;

namespace ONI_MP.Cloud
{
    public class GoogleDriveUploader
    {
        private readonly DriveService _service;

        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();

        public bool IsUploading = false;

        public GoogleDriveUploader(DriveService service)
        {
            _service = service;
        }

        public async void UploadFile(string localFilePath, string driveFolderId = null)
        {
            if (!GoogleDrive.Instance.IsInitialized)
            {
                DebugConsole.LogError($"GoogleDriveUploader: Google Drive not initialized!", false);
                return;
            }

            if (!File.Exists(localFilePath))
            {
                DebugConsole.LogError($"GoogleDriveUploader: file not found at {localFilePath}", false);
                OnUploadFailed?.Invoke(new FileNotFoundException("Upload file missing", localFilePath));
                return;
            }

            try
            {
                IsUploading = true;
                OnUploadStarted?.Invoke();
                MultiplayerOverlay.Show("Starting upload...");

                var listRequest = _service.Files.List();
                listRequest.Q = $"name='{Path.GetFileName(localFilePath)}' and trashed=false";
                listRequest.Fields = "files(id, name)";
                var existingFiles = await listRequest.ExecuteAsync();

                if (existingFiles.Files != null && existingFiles.Files.Count > 0)
                {
                    await OverwriteFileAsync(localFilePath, existingFiles.Files[0].Id, driveFolderId);
                }
                else
                {
                    await UploadNewFileAsync(localFilePath, driveFolderId);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Upload failed: {ex.Message}");
                FinishUploading(3);
            }
        }

        private async Task OverwriteFileAsync(string localFilePath, string existingFileId, string folderId)
        {
            try
            {
                var metadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(localFilePath)
                };

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var updateRequest = _service.Files.Update(metadata, existingFileId, fs, "application/octet-stream");
                    updateRequest.Fields = "id";

                    updateRequest.ProgressChanged += progress =>
                    {
                        int percent = (int) (progress.BytesSent * 100.0 / fs.Length);
                        MultiplayerOverlay.Show($"Uploading world: {percent}%");
                    };

                    var result = await updateRequest.UploadAsync();

                    if (result.Status != UploadStatus.Completed)
                    {
                        DebugConsole.LogError($"GoogleDriveUploader: Overwrite failed - {result.Exception}", false);
                        OnUploadFailed?.Invoke(result.Exception);
                        MultiplayerOverlay.Show($"Upload failed: {result.Exception?.Message}");
                        FinishUploading(3);
                        return;
                    }

                    // after upload, move to correct folder if needed
                    if (!string.IsNullOrEmpty(folderId))
                    {
                        var moveRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), existingFileId);
                        moveRequest.AddParents = folderId;
                        moveRequest.Fields = "id, parents";
                        await moveRequest.ExecuteAsync();
                    }

                    GrantPublicAccessAndFinish(updateRequest.ResponseBody.Id);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Overwrite failed: {ex.Message}");
                FinishUploading(3);
            }
        }

        private async Task UploadNewFileAsync(string localFilePath, string folderId)
        {
            try
            {
                var metadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(localFilePath)
                };

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var createRequest = _service.Files.Create(metadata, fs, "application/octet-stream");
                    createRequest.Fields = "id";

                    createRequest.ProgressChanged += progress =>
                    {
                        int percent = (int) (progress.BytesSent * 100.0 / fs.Length);
                        MultiplayerOverlay.Show($"Uploading world: {percent}%");
                    };

                    var result = await createRequest.UploadAsync();

                    if (result.Status != UploadStatus.Completed)
                    {
                        DebugConsole.LogError($"GoogleDriveUploader: Upload failed - {result.Exception}", false);
                        OnUploadFailed?.Invoke(result.Exception);
                        MultiplayerOverlay.Show($"Upload failed: {result.Exception?.Message}");
                        FinishUploading(3);
                        return;
                    }

                    var uploadedFileId = createRequest.ResponseBody.Id;

                    if (!string.IsNullOrEmpty(folderId))
                    {
                        var moveRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), uploadedFileId);
                        moveRequest.AddParents = folderId;
                        moveRequest.Fields = "id, parents";
                        await moveRequest.ExecuteAsync();
                    }

                    GrantPublicAccessAndFinish(uploadedFileId);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Upload failed: {ex.Message}");
                FinishUploading(3);
            }
        }

        private void GrantPublicAccessAndFinish(string uploadedFileId)
        {
            // grant public read access
            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "anyone",
                Role = "reader"
            };
            _service.Permissions.Create(permission, uploadedFileId).Execute();

            string link = $"https://drive.usercontent.google.com/u/0/uc?id={uploadedFileId}&export=download";

            DebugConsole.Log($"GoogleDriveUploader: Upload successful. Shareable link: {link}");

            OnUploadFinished?.Invoke(link);
            MultiplayerOverlay.Show("Upload complete!");
            FinishUploading();
        }

        public async Task<string> GetOrCreateFolderAsync(string folderName)
        {
            // 1. Search for existing
            var listRequest = _service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            listRequest.Fields = "files(id, name)";
            var folders = await listRequest.ExecuteAsync();

            if (folders.Files != null && folders.Files.Count > 0)
            {
                return folders.Files[0].Id;
            }

            // 2. If not found, create it
            var metadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = _service.Files.Create(metadata);
            createRequest.Fields = "id";
            var folder = await createRequest.ExecuteAsync();

            DebugConsole.Log($"GoogleDriveUploader: Created new folder '{folderName}' with ID {folder.Id}");
            return folder.Id;
        }

        private async void FinishUploading(int seconds = 1)
        {
            await Task.Delay(seconds * 1000); // wait for 1 second
            MultiplayerOverlay.Close();
            IsUploading = false;
        }

    }
}
