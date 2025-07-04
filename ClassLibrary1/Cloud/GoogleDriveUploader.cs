using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

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

        public void UploadFile(string localFilePath, string driveFolderId = null)
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
                var existingFiles = listRequest.Execute();

                if (existingFiles.Files != null && existingFiles.Files.Count > 0)
                {
                    OverwriteFile(localFilePath, existingFiles.Files[0].Id, driveFolderId);
                }
                else
                {
                    UploadNewFile(localFilePath, driveFolderId);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Upload failed: {ex.Message}");
                CoroutineRunner.RunOne(WaitAndHide());
            }
        }

        private void OverwriteFile(string localFilePath, string existingFileId, string driveFolderId)
        {
            try
            {
                var metadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(localFilePath),
                    Parents = driveFolderId != null ? new[] { driveFolderId } : null
                };

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var updateRequest = _service.Files.Update(metadata, existingFileId, fs, "application/octet-stream");
                    updateRequest.Fields = "id";

                    updateRequest.ProgressChanged += progress =>
                    {
                        double percent = progress.BytesSent * 100.0 / fs.Length;
                        MultiplayerOverlay.Show($"Uploading world: {percent:0.##}%");
                    };

                    var result = updateRequest.Upload();

                    if (result.Status != UploadStatus.Completed)
                    {
                        DebugConsole.LogError($"GoogleDriveUploader: Overwrite failed - {result.Exception}", false);
                        OnUploadFailed?.Invoke(result.Exception);
                        MultiplayerOverlay.Show($"Upload failed: {result.Exception?.Message}");
                        CoroutineRunner.RunOne(WaitAndHide());
                        return;
                    }

                    GrantPublicAccessAndFinish(updateRequest.ResponseBody.Id);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Overwrite failed: {ex.Message}");
                CoroutineRunner.RunOne(WaitAndHide());
            }
        }

        private void UploadNewFile(string localFilePath, string driveFolderId)
        {
            try
            {
                var metadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(localFilePath),
                    Parents = driveFolderId != null ? new[] { driveFolderId } : null
                };

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var createRequest = _service.Files.Create(metadata, fs, "application/octet-stream");
                    createRequest.Fields = "id";

                    createRequest.ProgressChanged += progress =>
                    {
                        double percent = progress.BytesSent * 100.0 / fs.Length;
                        MultiplayerOverlay.Show($"Uploading world: {percent:0.##}%");
                    };

                    var result = createRequest.Upload();

                    if (result.Status != UploadStatus.Completed)
                    {
                        DebugConsole.LogError($"GoogleDriveUploader: Upload failed - {result.Exception}", false);
                        OnUploadFailed?.Invoke(result.Exception);
                        MultiplayerOverlay.Show($"Upload failed: {result.Exception?.Message}");
                        CoroutineRunner.RunOne(WaitAndHide());
                        return;
                    }

                    GrantPublicAccessAndFinish(createRequest.ResponseBody.Id);
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Upload failed: {ex.Message}");
                CoroutineRunner.RunOne(WaitAndHide());
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

            string link = $"https://drive.google.com/uc?id={uploadedFileId}&export=download";

            DebugConsole.Log($"GoogleDriveUploader: Upload successful. Shareable link: {link}");

            OnUploadFinished?.Invoke(link);
            MultiplayerOverlay.Show("Upload complete!");
            CoroutineRunner.RunOne(WaitAndHide());
        }

        IEnumerator WaitAndHide()
        {
            yield return new WaitForSeconds(1f);
            MultiplayerOverlay.Close();
            IsUploading = false;
        }
    }
}
