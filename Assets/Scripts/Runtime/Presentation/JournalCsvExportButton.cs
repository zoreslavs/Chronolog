using Chronolog.Persistence;
using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;

namespace Chronolog.Presentation
{
    public sealed class JournalCsvExportButton : MonoBehaviour
    {
        private const string ReadyText = "Export to CSV";
        private const string PreparingText = "Preparing...";

        [SerializeField] private Text label;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private JournalCsvExporter exporter;
        [SerializeField] private JournalSyncService syncService;
        [SerializeField, Range(0f, 1f)] private float inactiveAlpha = 0.35f;

        private IJournalRecordRepository repository;

        private void Awake()
        {
            repository = new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath());
        }

        private void OnEnable()
        {
            UpdateButton();
        }

        private void Start()
        {
            button.onClick.AddListener(Export);
            syncService.StatusChanged += OnSyncStatusChanged;
            syncService.SyncCompleted += OnSyncCompleted;
            UpdateButton();
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(Export);
            syncService.StatusChanged -= OnSyncStatusChanged;
            syncService.SyncCompleted -= OnSyncCompleted;
        }

        private void Export()
        {
            if (!button.interactable)
                return;

            SetInteractable(false);
            label.text = PreparingText;
            exporter.Export(OnExportCompleted, OnExportFailed);
        }

        private void OnSyncStatusChanged(JournalSyncStatus status)
        {
            UpdateButton();
        }

        private void OnSyncCompleted()
        {
            UpdateButton();
        }

        private void OnExportCompleted(string filePath)
        {
            UpdateButton();
        }

        private void OnExportFailed(string errorMessage)
        {
            UpdateButton();
        }

        private void UpdateButton()
        {
            if (exporter.IsExporting)
                return;

            SetInteractable(JournalCsvExportAvailability.CanExport(syncService.Status, HasRecords()));
            label.text = ReadyText;
        }

        private void SetInteractable(bool isInteractable)
        {
            button.interactable = isInteractable;
            canvasGroup.alpha = isInteractable ? 1f : inactiveAlpha;
        }

        private bool HasRecords()
        {
            return repository.GetAll().Any(record => !record.IsDeleted);
        }
    }
}