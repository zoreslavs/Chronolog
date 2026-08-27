using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalKeyboardController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private InputField contentInput;
        [SerializeField] private Button doneButton;
        [SerializeField] private GameObject menuButtons;

        private RectTransform formScreen;
        private Vector2 initialScreenPosition;
        private float unobscuredFrameBottomPixels;

        private void Awake()
        {
            formScreen = (RectTransform)transform;
            initialScreenPosition = formScreen.anchoredPosition;
            SetKeyboardLayout(false, 0f);
        }

        private void OnEnable()
        {
            doneButton.onClick.AddListener(Done);
        }

        private void OnDisable()
        {
            doneButton.onClick.RemoveListener(Done);
            SetKeyboardLayout(false, 0f);
        }

        private void Update()
        {
            GetAndroidWindowFrame(out var visibleFrameBottomPixels, out var windowHeightPixels);
            CaptureUnobscuredFrameBottom(visibleFrameBottomPixels, windowHeightPixels);

            var keyboardHeightPixels = GetKeyboardHeight(visibleFrameBottomPixels);
            var isKeyboardVisible = contentInput.isFocused && keyboardHeightPixels > 0f;
            SetKeyboardLayout(isKeyboardVisible, keyboardHeightPixels);
            SetPlaceholderVisible();
        }

        private static void GetAndroidWindowFrame(out float visibleDisplayFrameBottomPixels, out float windowHeightPixels)
        {
            visibleDisplayFrameBottomPixels = 0f;
            windowHeightPixels = 0f;

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var window = currentActivity.Call<AndroidJavaObject>("getWindow"))
            using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
            using (var visibleDisplayFrame = new AndroidJavaObject("android.graphics.Rect"))
            {
                decorView.Call("getWindowVisibleDisplayFrame", visibleDisplayFrame);
                visibleDisplayFrameBottomPixels = visibleDisplayFrame.Get<int>("bottom");
                windowHeightPixels = decorView.Call<int>("getHeight");
            }
#endif
        }

        private void Done()
        {
            contentInput.DeactivateInputField();
            
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            SetKeyboardLayout(false, 0f);
        }

        private void CaptureUnobscuredFrameBottom(float visibleFrameBottomPixels, float windowHeightPixels)
        {
            if (visibleFrameBottomPixels <= 0f)
                return;

            if (!contentInput.isFocused)
                unobscuredFrameBottomPixels = visibleFrameBottomPixels;

            if (unobscuredFrameBottomPixels <= 0f && windowHeightPixels > 0f)
                unobscuredFrameBottomPixels = windowHeightPixels;
        }

        private float GetKeyboardHeight(float visibleFrameBottomPixels)
        {
            if (unobscuredFrameBottomPixels <= 0f || visibleFrameBottomPixels <= 0f)
                return 0f;

            return Mathf.Max(0f, unobscuredFrameBottomPixels - visibleFrameBottomPixels);
        }

        private void SetKeyboardLayout(bool isKeyboardVisible, float keyboardHeightPixels)
        {
            formScreen.anchoredPosition = isKeyboardVisible
                ? GetScreenPosition(initialScreenPosition, keyboardHeightPixels, canvas.scaleFactor)
                : initialScreenPosition;
            menuButtons.SetActive(!isKeyboardVisible);
            doneButton.gameObject.SetActive(isKeyboardVisible);
        }

        private static Vector2 GetScreenPosition(Vector2 initialPosition, float keyboardHeightPixels, float canvasScaleFactor)
        {
            if (canvasScaleFactor <= 0f)
                return initialPosition;

            return initialPosition + Vector2.up * (keyboardHeightPixels / canvasScaleFactor);
        }

        private void SetPlaceholderVisible()
        {
            if (contentInput.placeholder == null)
                return;

            contentInput.placeholder.gameObject.SetActive(ShouldShowPlaceholder(contentInput.isFocused, contentInput.text));
        }

        private static bool ShouldShowPlaceholder(bool isFocused, string text)
        {
            return !isFocused && string.IsNullOrEmpty(text);
        }
    }
}