using Chronolog.Persistence;
using UnityEngine.UI;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalListScreen : MonoBehaviour
    {
        [SerializeField] private GameObject emptyState;
        [SerializeField] private Button newRecordButton;
        [SerializeField] private JournalScreenNavigator navigator;
        [SerializeField] private RectTransform recordsContainer;
        [SerializeField] private JournalListRecordView recordViewPrefab;
        private JournalListData journalListData;

        private void Start()
        {
            newRecordButton.onClick.AddListener(navigator.ShowForm);
            Reload();
        }

        private void OnEnable()
        {
            if (journalListData != null)
                Reload();
        }

        private void Reload()
        {
            journalListData = JournalListData.Create(new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath()));
            Render();
        }

        private void OnDestroy()
        {
            if (newRecordButton != null)
            {
                newRecordButton.onClick.RemoveListener(navigator.ShowForm);
            }
        }

        private void Render()
        {
            ClearRecordViews();
            emptyState.SetActive(journalListData.IsEmpty);

            foreach (var record in journalListData.Records)
            {
                var recordView = Instantiate(recordViewPrefab, recordsContainer);
                recordView.Init(record);
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