using Chronolog.Domain;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalScreenNavigator : MonoBehaviour
    {
        [SerializeField] private GameObject listScreen;
        [SerializeField] private GameObject formScreen;

        public void ShowList()
        {
            listScreen.SetActive(true);
            formScreen.SetActive(false);
        }

        public void ShowForm()
        {
            listScreen.SetActive(false);
            formScreen.SetActive(true);
        }

        public void ShowForm(JournalRecord record)
        {
            ShowForm();
            formScreen.GetComponent<JournalRecordFormScreen>().Open(record);
        }
    }
}