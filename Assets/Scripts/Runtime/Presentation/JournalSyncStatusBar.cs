using UnityEngine.UI;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalSyncStatusBar : MonoBehaviour
    {
        [SerializeField] private JournalSyncService syncService;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Color syncedColor = new(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color syncingColor = new(1f, 0.76f, 0.03f);
        [SerializeField] private Color offlineColor = new(0.62f, 0.62f, 0.62f);
        [SerializeField] private Color failedColor = new(0.96f, 0.26f, 0.21f);

        private void Start()
        {
            syncService.StatusChanged += OnStatusChanged;
            OnStatusChanged(syncService.Status);
        }

        private void OnDestroy()
        {
            syncService.StatusChanged -= OnStatusChanged;
        }

        private void OnStatusChanged(JournalSyncStatus status)
        {
            switch (status)
            {
                case JournalSyncStatus.Synced:
                    SetState(syncedColor, "Synced");
                    break;
                case JournalSyncStatus.Syncing:
                    SetState(syncingColor, "Syncing...");
                    break;
                case JournalSyncStatus.Offline:
                    SetState(offlineColor, "Offline");
                    break;
                case JournalSyncStatus.Failed:
                    SetState(failedColor, "Sync failed");
                    break;
            }
        }

        private void SetState(Color color, string text)
        {
            statusIcon.color = color;
            statusLabel.text = text;
        }
    }
}