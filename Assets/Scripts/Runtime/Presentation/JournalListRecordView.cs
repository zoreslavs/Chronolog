using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalListRecordView : MonoBehaviour
    {
        [SerializeField] private Text dateLabel;
        [SerializeField] private Text contentLabel;
        [SerializeField] private Text imageSourceLabel;

        public void Init(JournalRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            dateLabel.text = record.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm");
            contentLabel.text = record.Content;
            imageSourceLabel.text = record.ImageSource.ToString();
        }
    }
}