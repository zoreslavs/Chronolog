using Chronolog.Persistence;
using UnityEngine.UI;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalListScreen : MonoBehaviour
    {
        [SerializeField] private JournalScreenNavigator navigator;
        [SerializeField] private JournalSyncService syncService;
        [SerializeField] private ScrollRect recordsScrollRect;
        [SerializeField] private RectTransform recordsContainer;
        [SerializeField] private JournalListRecordView recordViewPrefab;
        [SerializeField] private Button newRecordButton;
        [SerializeField] private GameObject loadingAnimation;
        [SerializeField] private GameObject emptyState;
        private JournalListData journalListData;
        private bool hasLoadedOnce;

        private void Start()
        {
            newRecordButton.onClick.AddListener(navigator.ShowForm);
            syncService.SyncCompleted += OnSyncCompleted;
            syncService.StatusChanged += OnSyncStatusChanged;

            LoadData();
            UpdateScrollAvailability(syncService.Status);

            if (syncService.Status == JournalSyncStatus.Syncing)
            {
                loadingAnimation.SetActive(true);
                emptyState.SetActive(false);
            }
            else
            {
                Render();
            }
        }

        private void OnEnable()
        {
            if (hasLoadedOnce)
            {
                LoadData();
                Render();
            }
        }

        private void OnDestroy()
        {
            newRecordButton.onClick.RemoveListener(navigator.ShowForm);
            syncService.SyncCompleted -= OnSyncCompleted;
            syncService.StatusChanged -= OnSyncStatusChanged;
        }

        private void OnSyncCompleted()
        {
            LoadData();
            Render();
        }

        private void OnSyncStatusChanged(JournalSyncStatus status)
        {
            loadingAnimation.SetActive(status == JournalSyncStatus.Syncing);
            UpdateScrollAvailability(status);

            var showEmptyState = status is JournalSyncStatus.Synced or JournalSyncStatus.Failed or JournalSyncStatus.Offline
                                 && journalListData != null && journalListData.IsEmpty;
            emptyState.SetActive(showEmptyState);
        }

        private void LoadData()
        {
            journalListData = JournalListData.Create(new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath()));
            hasLoadedOnce = true;
        }

        private void UpdateScrollAvailability(JournalSyncStatus status)
        {
            recordsScrollRect.enabled = JournalListScrollAvailability.CanScroll(status);
        }

        private void Render()
        {
            ClearRecordViews();

            var isSyncing = syncService != null && syncService.Status == JournalSyncStatus.Syncing;
            loadingAnimation.SetActive(isSyncing);
            emptyState.SetActive(!isSyncing && journalListData.IsEmpty);

            foreach (var record in journalListData.Records)
            {
                var recordView = Instantiate(recordViewPrefab, recordsContainer);
                recordView.Init(record, navigator.ShowForm);
            }
        }

        private void ClearRecordViews()
        {
            for (var index = recordsContainer.childCount - 1; index >= 0; index--)
            {
                Destroy(recordsContainer.GetChild(index).gameObject);
            }
        }
    }
}