using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ReTune.Scenes
{
    [DefaultExecutionOrder(-10000)]
    public sealed class ReTuneExitDialog : MonoBehaviour
    {
        static ReTuneExitDialog instance;
        static int closedFrame = -1;
        GameObject shield;
        Button continueButton;
        Font font;
        bool open, previousAudioPause;
        float previousTimeScale;
        GameObject previousSelection;
        public static bool IsOpen => instance != null && instance.open || closedFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; closedFrame = -1; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() { if (instance == null) new GameObject("Exit Confirmation").AddComponent<ReTuneExitDialog>(); }
        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject); Build();
        }
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (open) Continue(); else RequestExit();
        }
        public static void RequestExit()
        {
            if (instance == null) Bootstrap();
            if (instance.open) return;
            foreach (var controller in FindObjectsOfType<ReTuneSceneController>())
                if (controller.StoryTransitionLocked) return;
            instance.Open();
        }
        void Open()
        {
            previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            open = true; Time.timeScale = 0; AudioListener.pause = true; shield.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        public void Continue()
        {
            if (!open) return;
            open = false; closedFrame = Time.frameCount; shield.SetActive(false);
            Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
        }
        public void End()
        {
            if (!open) return;
            // Dialogue and installed parts already save their checkpoints; do not overwrite the dialogue page with zero.
            PlayerPrefs.Save(); Continue();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        {
            if (instance != this) return;
            if (open) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; }
            instance = null;
        }
        RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            go.GetComponent<Image>().color = color; return rect;
        }
        Text Label(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.sizeDelta = bounds; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = color; text.text = value;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        Button Action(Transform parent, string label, float x, UnityEngine.Events.UnityAction action)
        {
            var rect = Box(label, parent, new Vector2(260, 70), new Vector2(x, -100), Color.clear);
            var button = rect.gameObject.AddComponent<Button>();
            Label("Label", rect, label, 40, Vector2.zero, rect.sizeDelta, ReTuneUITheme.Ink);
            button.onClick.AddListener(action); ReTuneUITheme.StyleButton(button); return button;
        }
        void Build()
        {
            font = Resources.Load<Font>("Fonts/LeMiShiguang");
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f; gameObject.AddComponent<GraphicRaycaster>();
            var backdrop = Box("Exit Veil", transform, Vector2.zero, Vector2.zero, ReTuneUITheme.Veil);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            shield = backdrop.gameObject;
            var panel = Box("Exit Panel", backdrop, new Vector2(860, 350), Vector2.zero, ReTuneUITheme.Surface);
            panel.gameObject.AddComponent<ReTuneSubtitleGradient>();
            Label("Title", panel, "End the game?", 46, new Vector2(0, 90), new Vector2(780, 70), ReTuneUITheme.Accent);
            Label("Message", panel, "You can continue from your last checkpoint.", 32, new Vector2(0, 10), new Vector2(780, 80), ReTuneUITheme.Ink);
            continueButton = Action(panel, "Continue", -180, Continue); var endButton = Action(panel, "End", 180, End);
            var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = endButton, selectOnRight = endButton, selectOnUp = continueButton, selectOnDown = continueButton };
            continueButton.navigation = navigation;
            navigation.selectOnLeft = navigation.selectOnRight = continueButton; navigation.selectOnUp = navigation.selectOnDown = endButton;
            endButton.navigation = navigation;
            shield.SetActive(false);
        }
    }
}
