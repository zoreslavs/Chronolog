using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalNetworkMonitor : MonoBehaviour
    {
        [SerializeField] private float checkIntervalSeconds = 2f;

        private bool wasReachable;
        private float timer;

        public bool IsReachable { get; private set; }
        public event Action BecameReachable;
        public event Action BecameUnreachable;

        private void Awake()
        {
            IsReachable = CheckReachability();
            wasReachable = IsReachable;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < checkIntervalSeconds)
                return;

            timer = 0f;
            IsReachable = CheckReachability();

            if (IsReachable && !wasReachable)
                BecameReachable?.Invoke();
            else if (!IsReachable && wasReachable)
                BecameUnreachable?.Invoke();

            wasReachable = IsReachable;
        }

        private static bool CheckReachability()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}