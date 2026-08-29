using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Chronolog.Presentation;

namespace Chronolog.Tests
{
    public sealed class JournalNetworkMonitorTests
    {
        [UnityTest]
        public IEnumerator AddComponent_InitializesReachabilityImmediately()
        {
            yield return new EnterPlayMode();

            var gameObject = new GameObject();

            var monitor = gameObject.AddComponent<JournalNetworkMonitor>();

            Assert.That(monitor.IsReachable, Is.EqualTo(Application.internetReachability != NetworkReachability.NotReachable));

            Object.DestroyImmediate(gameObject);
            yield return new ExitPlayMode();
        }
    }
}
