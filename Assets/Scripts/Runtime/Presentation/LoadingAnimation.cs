using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class LoadingAnimation : MonoBehaviour
    {
        [SerializeField] private int spokeCount = 12;
        [SerializeField] private float stepsPerSecond = 10f;

        private float stepAngle;
        private float timer;

        private void OnEnable()
        {
            stepAngle = 360f / spokeCount;
            timer = 0f;
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var interval = 1f / stepsPerSecond;
            if (timer < interval)
                return;

            timer -= interval;
            transform.Rotate(0f, 0f, -stepAngle);
        }
    }
}