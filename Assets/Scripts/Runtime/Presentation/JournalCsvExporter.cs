using UnityEngine.Networking;
using System.Collections;
using UnityEngine;
using System.IO;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalCsvExporter : MonoBehaviour
    {
        private string deviceId;

        public bool IsExporting { get; private set; }

        private void Awake()
        {
            deviceId = JournalDeviceId.Get();
        }

        public void Export(Action<string> completed, Action<string> failed)
        {
            if (IsExporting)
                return;

            IsExporting = true;
            StartCoroutine(ExportCsv(completed, failed));
        }

        private IEnumerator ExportCsv(Action<string> completed, Action<string> failed)
        {
            using var request = UnityWebRequest.Get($"{JournalApiConfig.BaseUrl.TrimEnd('/')}/export.csv");
            request.SetRequestHeader(JournalApiConfig.DeviceIdHeader, deviceId);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                CompleteWithFailure(failed, $"CSV export failed: {request.responseCode} {request.error}");
                yield break;
            }

            string filePath;
            try
            {
                filePath = JournalCsvExportFile.Save(
                    Path.Combine(Application.temporaryCachePath, "exports"),
                    request.downloadHandler.text,
                    DateTimeOffset.Now);
            }
            catch (Exception exception)
            {
                CompleteWithFailure(failed, $"CSV file could not be saved: {exception.Message}");
                yield break;
            }

            Debug.Log($"Journal CSV export saved to {filePath}");
            new NativeShare()
                .AddFile(filePath, "text/csv")
                .SetSubject("Chronolog journal export")
                .SetTitle("Export to CSV")
                .Share();

            IsExporting = false;
            completed?.Invoke(filePath);
        }

        private void CompleteWithFailure(Action<string> failed, string errorMessage)
        {
            IsExporting = false;
            Debug.LogWarning(errorMessage);
            failed?.Invoke(errorMessage);
        }
    }
}