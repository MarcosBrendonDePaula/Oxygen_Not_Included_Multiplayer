using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using System;
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

        public GoogleDriveUploader(DriveService service)
        {
            _service = service;
        }

        public void UploadFile(string localFilePath, string driveFolderId = null)
        {
            if (!GoogleDrive.Instance.IsInitialized)
            {
                DebugConsole.LogError($"GoogleDriveUploader: Google drive not initialized!", false);
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
                OnUploadStarted?.Invoke();
                MultiplayerOverlay.Show("Starting upload...");

                var metadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(localFilePath),
                    Parents = driveFolderId != null ? new[] { driveFolderId } : null
                };

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var request = _service.Files.Create(metadata, fs, "application/octet-stream");
                    request.Fields = "id";

                    request.ProgressChanged += progress =>
                    {
                        double percent = progress.BytesSent * 100.0 / fs.Length;
                        MultiplayerOverlay.Show($"Uploading world: {percent:0.##}%");
                    };

                    var result = request.Upload();

                    if (result.Status != UploadStatus.Completed)
                    {
                        DebugConsole.LogError($"GoogleDriveUploader: Upload failed - {result.Exception}", false);
                        OnUploadFailed?.Invoke(result.Exception);
                        MultiplayerOverlay.Show($"Upload failed: {result.Exception?.Message}");
                        return;
                    }

                    var uploadedFileId = request.ResponseBody.Id;

                    // grant public read access (Only accessible by people with the link)
                    var permission = new Google.Apis.Drive.v3.Data.Permission
                    {
                        Type = "anyone",
                        Role = "reader"
                    };
                    _service.Permissions.Create(permission, uploadedFileId).Execute();

                    // build shareable link
                    string link = $"https://drive.google.com/uc?id={uploadedFileId}&export=download";

                    DebugConsole.Log($"GoogleDriveUploader: Upload successful. Shareable link: {link}");

                    // pass link to event
                    OnUploadFinished?.Invoke(link);

                    MultiplayerOverlay.Show("Upload complete!");
                }
            }
            catch (Exception ex)
            {
                OnUploadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Upload failed: {ex.Message}");
            }
        }
    }
}
