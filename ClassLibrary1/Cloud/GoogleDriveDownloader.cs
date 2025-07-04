using Google.Apis.Drive.v3;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using System;
using System.IO;
using UnityEngine;

namespace ONI_MP.Cloud
{
    public class GoogleDriveDownloader
    {
        private readonly DriveService _service;

        public event System.Action OnDownloadStarted;
        public event System.Action<string> OnDownloadFinished;
        public event System.Action<Exception> OnDownloadFailed;

        public GoogleDriveDownloader(DriveService service)
        {
            _service = service;
        }

        public void DownloadFile(string fileId, string localDestinationPath)
        {
            if (!GoogleDrive.Instance.IsInitialized)
            {
                DebugConsole.LogError("GoogleDriveDownloader: Google Drive is not initialized!", false);
                return;
            }

            try
            {
                OnDownloadStarted?.Invoke();
                MultiplayerOverlay.Show("Starting download...");

                using (var fs = new FileStream(localDestinationPath, FileMode.Create))
                {
                    var request = _service.Files.Get(fileId);

                    var fileMetadata = _service.Files.Get(fileId).Execute();
                    long totalSize = fileMetadata.Size ?? 0;

                    request.MediaDownloader.ProgressChanged += progress =>
                    {
                        double percent = progress.BytesDownloaded > 0 && totalSize > 0
                            ? progress.BytesDownloaded * 100.0 / totalSize
                            : 0;

                        MultiplayerOverlay.Show($"Downloading world: {percent:0.##}%");
                    };

                    request.Download(fs);
                }

                DebugConsole.Log($"GoogleDriveDownloader: Download complete to {localDestinationPath}");
                OnDownloadFinished?.Invoke(Path.GetFileName(localDestinationPath));
                MultiplayerOverlay.Show("Download complete!");
            }
            catch (Exception ex)
            {
                OnDownloadFailed?.Invoke(ex);
                MultiplayerOverlay.Show($"Download failed: {ex.Message}");
            }
        }
    }
}
