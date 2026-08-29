using UnityEngine.UI;
using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalDeleteRecordPopup : MonoBehaviour
    {
        private const string DeleteConfirmationMessage = "Are you sure you want to permanently delete this record?";

        [SerializeField] private Text confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button deleteButton;

        public Action<bool> OnDeleteConfirmed;

        private void OnEnable()
        {
            confirmLabel.text = DeleteConfirmationMessage;
            cancelButton.onClick.AddListener(Cancel);
            deleteButton.onClick.AddListener(Delete);
        }

        private void OnDisable()
        {
            cancelButton.onClick.RemoveListener(Cancel);
            deleteButton.onClick.RemoveListener(Delete);
        }

        private void Cancel()
        {
            OnDeleteConfirmed?.Invoke(false);
        }

        private void Delete()
        {
            OnDeleteConfirmed?.Invoke(true);
        }
    }
}