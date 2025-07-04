using Google.Apis.Drive.v3;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    public class GoogleDriveDownloader
    {
        private readonly DriveService _service;

        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();

        public GoogleDriveDownloader(DriveService service)
        {
            _service = service;
        }

        /// <summary>
        /// Downloads a file by fileId using the Drive API.
        /// </summary>
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

        /// <summary>
        /// Downloads from a public Google Drive shareable link to a known location.
        /// </summary>
        public void DownloadFromShareLink(string shareLink, string fileName)
        {
            try
            {
                OnDownloadStarted?.Invoke();
                MultiplayerOverlay.Show("Starting download from host...");

                var savePath = SaveLoader.GetCloudSavesDefault()
                    ? SaveLoader.GetCloudSavePrefix()
                    : SaveLoader.GetSavePrefixAndCreateFolder();

                var targetFile = SecurePath.Combine(
                    savePath,
                    Path.GetFileNameWithoutExtension(fileName),
                    $"{Path.GetFileNameWithoutExtension(fileName)}.sav"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                using (var web = new System.Net.WebClient())
                {
                    web.DownloadProgressChanged += (s, e) =>
                    {
                        MultiplayerOverlay.Show($"Downloading world from host: {e.ProgressPercentage}%");
                    };

                    web.DownloadFileCompleted += (s, e) =>
                    {
                        if (e.Error != null)
                        {
                            MultiplayerOverlay.Show($"Download failed: {e.Error.Message}");
                            DebugConsole.LogError($"[GoogleDriveDownloader] Download failed: {e.Error.Message}");
                            OnDownloadFailed?.Invoke(e.Error);
                            return;
                        }

                        MultiplayerOverlay.Show("Download complete!");
                        DebugConsole.Log($"[GoogleDriveDownloader] Downloaded to: {targetFile}");
                        OnDownloadFinished?.Invoke(Path.GetFileName(targetFile));
                    };

                    web.DownloadFileAsync(new Uri(shareLink), targetFile);

                    // Wait for completion synchronously to match your pattern:
                    while (web.IsBusy)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                MultiplayerOverlay.Show($"Download failed: {ex.Message}");
                DebugConsole.LogError($"[GoogleDriveDownloader] Download failed: {ex.Message}");
                OnDownloadFailed?.Invoke(ex);
            }
        }
    }
}
