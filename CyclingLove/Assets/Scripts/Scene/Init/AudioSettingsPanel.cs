using System.GameSession;
using TMPro;
using UnityEngine;

namespace Scene.Init
{
    public sealed class AudioSettingsPanel : MonoBehaviour, UnityEngine.EventSystems.ICancelHandler
    {
        private static readonly Color OverlayColor = new(0.02f, 0.03f, 0.05f, 0.88f);
        private static readonly Color PanelColor = new(0.075f, 0.10f, 0.14f, 0.98f);
        private static readonly Color AccentColor = new(0.20f, 0.72f, 0.82f, 1f);
        private static readonly Color ButtonColor = new(0.14f, 0.20f, 0.26f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.96f, 0.98f, 1f);
        private static readonly Color SubTextColor = new(0.72f, 0.78f, 0.84f, 1f);

        private UnityEngine.UI.Slider _masterSlider;
        private UnityEngine.UI.Slider _bgmSlider;
        private UnityEngine.UI.Slider _ambientSlider;
        private UnityEngine.UI.Slider _seSlider;
        private TextMeshProUGUI _masterValue;
        private TextMeshProUGUI _bgmValue;
        private TextMeshProUGUI _ambientValue;
        private TextMeshProUGUI _seValue;
        private UnityEngine.UI.Toggle _muteToggle;
        private UnityEngine.UI.Button _applyButton;
        private AudioSettingsData _working;
        private global::System.Action<AudioSettingsData> _onPreview;
        private global::System.Action<AudioSettingsData> _onApply;
        private global::System.Action _onCancel;
        private bool _suppressEvents;

        public bool IsOpen => gameObject.activeSelf;

        public static AudioSettingsPanel Create(Transform canvasTransform)
        {
            var root = new GameObject(
                "AudioSettingsPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image),
                typeof(AudioSettingsPanel));
            root.transform.SetParent(canvasTransform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var blocker = root.GetComponent<UnityEngine.UI.Image>();
            blocker.color = OverlayColor;
            blocker.raycastTarget = true;

            var panel = root.GetComponent<AudioSettingsPanel>();
            panel.Build();
            root.SetActive(false);
            return panel;
        }

        public void Show(
            AudioSettingsData settings,
            global::System.Action<AudioSettingsData> onPreview,
            global::System.Action<AudioSettingsData> onApply,
            global::System.Action onCancel)
        {
            _working = (settings ?? new AudioSettingsData()).Snapshot();
            _working.Normalize();
            _onPreview = onPreview;
            _onApply = onApply;
            _onCancel = onCancel;

            _suppressEvents = true;
            _masterSlider.value = _working.MasterVolume;
            _bgmSlider.value = _working.BgmVolume;
            _ambientSlider.value = _working.AmbientVolume;
            _seSlider.value = _working.SeVolume;
            _muteToggle.isOn = _working.Muted;
            _suppressEvents = false;
            UpdateValueLabels();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _applyButton.Select();
        }

        private void Build()
        {
            var content = CreateObject("Content", transform, typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.VerticalLayoutGroup));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(760f, 620f);
            content.GetComponent<UnityEngine.UI.Image>().color = PanelColor;
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 34, 34);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(content.transform, "Title", "サウンド設定", 34f, FontStyles.Bold, TextAlignmentOptions.Center, 48f, TextColor);
            CreateText(content.transform, "Description", "音量はすぐに反映されます。適用すると次回起動時にも引き継ぎます。",
                18f, FontStyles.Normal, TextAlignmentOptions.Center, 42f, SubTextColor);

            (_masterSlider, _masterValue) = CreateSliderRow(content.transform, "Master", "全体", value =>
            {
                _working.MasterVolume = value;
                NotifyPreview();
            });
            (_bgmSlider, _bgmValue) = CreateSliderRow(content.transform, "Bgm", "BGM", value =>
            {
                _working.BgmVolume = value;
                NotifyPreview();
            });
            (_ambientSlider, _ambientValue) = CreateSliderRow(content.transform, "Ambient", "環境音", value =>
            {
                _working.AmbientVolume = value;
                NotifyPreview();
            });
            (_seSlider, _seValue) = CreateSliderRow(content.transform, "Se", "効果音", value =>
            {
                _working.SeVolume = value;
                NotifyPreview();
            });

            _muteToggle = CreateMuteRow(content.transform);
            CreateText(content.transform, "Hint", "ボイス音量はボイス実装時に追加します。",
                16f, FontStyles.Normal, TextAlignmentOptions.Center, 28f, SubTextColor);

            var buttons = CreateObject("Buttons", content.transform, typeof(UnityEngine.UI.HorizontalLayoutGroup),
                typeof(UnityEngine.UI.LayoutElement));
            ((RectTransform)buttons.transform).sizeDelta = new Vector2(0f, 58f);
            buttons.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58f;
            var buttonLayout = buttons.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            buttonLayout.spacing = 14f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = true;

            CreateButton(buttons.transform, "DefaultsButton", "初期値へ戻す", RestoreDefaults, ButtonColor);
            CreateButton(buttons.transform, "CancelButton", "キャンセル", Cancel, ButtonColor);
            _applyButton = CreateButton(buttons.transform, "ApplyButton", "適用", Apply, AccentColor);
        }

        private (UnityEngine.UI.Slider, TextMeshProUGUI) CreateSliderRow(
            Transform parent, string objectName, string label, global::System.Action<float> onChanged)
        {
            var row = CreateObject(objectName + "Row", parent, typeof(UnityEngine.UI.HorizontalLayoutGroup),
                typeof(UnityEngine.UI.LayoutElement));
            ((RectTransform)row.transform).sizeDelta = new Vector2(0f, 62f);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 62f;
            var rowLayout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            rowLayout.spacing = 18f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var labelText = CreateText(row.transform, objectName + "Label", label, 22f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, 42f, TextColor);
            var labelLayout = labelText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            labelLayout.preferredWidth = 150f;
            labelLayout.preferredHeight = 42f;
            ((RectTransform)labelText.transform).sizeDelta = new Vector2(150f, 42f);

            var sliderObject = CreateObject(objectName + "Slider", row.transform, typeof(UnityEngine.UI.Slider),
                typeof(UnityEngine.UI.LayoutElement));
            var sliderLayout = sliderObject.GetComponent<UnityEngine.UI.LayoutElement>();
            sliderLayout.preferredWidth = 410f;
            sliderLayout.preferredHeight = 42f;
            ((RectTransform)sliderObject.transform).sizeDelta = new Vector2(410f, 42f);
            var slider = sliderObject.GetComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            var background = CreateObject("Background", sliderObject.transform, typeof(UnityEngine.UI.Image));
            SetHorizontalBar((RectTransform)background.transform, 8f, 0f);
            background.GetComponent<UnityEngine.UI.Image>().color = new Color(0.20f, 0.24f, 0.29f, 1f);

            var fillArea = CreateObject("Fill Area", sliderObject.transform);
            SetHorizontalBar((RectTransform)fillArea.transform, 8f, 10f);
            var fill = CreateObject("Fill", fillArea.transform, typeof(UnityEngine.UI.Image));
            Stretch((RectTransform)fill.transform);
            fill.GetComponent<UnityEngine.UI.Image>().color = AccentColor;

            var handleArea = CreateObject("Handle Slide Area", sliderObject.transform);
            Stretch((RectTransform)handleArea.transform, 11f, 11f, 0f, 0f);
            var handle = CreateObject("Handle", handleArea.transform, typeof(UnityEngine.UI.Image));
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(22f, 30f);
            handle.GetComponent<UnityEngine.UI.Image>().color = Color.white;

            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(value => onChanged(value));

            var valueText = CreateText(row.transform, objectName + "Value", "100%", 20f, FontStyles.Normal,
                TextAlignmentOptions.MidlineRight, 42f, TextColor);
            var valueLayout = valueText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            valueLayout.preferredWidth = 68f;
            valueLayout.preferredHeight = 42f;
            ((RectTransform)valueText.transform).sizeDelta = new Vector2(68f, 42f);
            return (slider, valueText);
        }

        private UnityEngine.UI.Toggle CreateMuteRow(Transform parent)
        {
            var row = CreateObject("MuteRow", parent, typeof(UnityEngine.UI.HorizontalLayoutGroup),
                typeof(UnityEngine.UI.LayoutElement));
            ((RectTransform)row.transform).sizeDelta = new Vector2(0f, 52f);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52f;
            var layout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var toggleObject = CreateObject("MuteToggle", row.transform, typeof(UnityEngine.UI.Toggle),
                typeof(UnityEngine.UI.LayoutElement));
            var toggleLayout = toggleObject.GetComponent<UnityEngine.UI.LayoutElement>();
            toggleLayout.preferredWidth = 34f;
            toggleLayout.preferredHeight = 34f;
            ((RectTransform)toggleObject.transform).sizeDelta = new Vector2(34f, 34f);
            var background = CreateObject("Background", toggleObject.transform, typeof(UnityEngine.UI.Image));
            Stretch((RectTransform)background.transform);
            background.GetComponent<UnityEngine.UI.Image>().color = new Color(0.20f, 0.24f, 0.29f, 1f);
            var checkmark = CreateObject("Checkmark", background.transform, typeof(UnityEngine.UI.Image));
            Stretch((RectTransform)checkmark.transform, 6f, 6f, 6f, 6f);
            checkmark.GetComponent<UnityEngine.UI.Image>().color = AccentColor;

            var toggle = toggleObject.GetComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = background.GetComponent<UnityEngine.UI.Image>();
            toggle.graphic = checkmark.GetComponent<UnityEngine.UI.Image>();
            toggle.onValueChanged.AddListener(value =>
            {
                if (_working == null) return;
                _working.Muted = value;
                NotifyPreview();
            });

            var label = CreateText(row.transform, "MuteLabel", "すべて消音", 22f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, 42f, TextColor);
            var labelLayout = label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            labelLayout.preferredWidth = 180f;
            labelLayout.preferredHeight = 42f;
            ((RectTransform)label.transform).sizeDelta = new Vector2(180f, 42f);
            return toggle;
        }

        private UnityEngine.UI.Button CreateButton(
            Transform parent, string objectName, string label, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var buttonObject = CreateObject(objectName, parent, typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button));
            var image = buttonObject.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText(buttonObject.transform, "Label", label, 20f, FontStyles.Bold,
                TextAlignmentOptions.Center, 50f, objectName == "ApplyButton" ? Color.black : TextColor, true);
            return button;
        }

        private TextMeshProUGUI CreateText(
            Transform parent, string objectName, string value, float size, FontStyles style,
            TextAlignmentOptions alignment, float height, Color color, bool stretch = false)
        {
            var textObject = CreateObject(objectName, parent, typeof(TextMeshProUGUI));
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            if (stretch) Stretch((RectTransform)textObject.transform, 8f, 8f, 4f, 4f);
            else ((RectTransform)textObject.transform).sizeDelta = new Vector2(0f, height);
            return label;
        }

        private static GameObject CreateObject(string objectName, Transform parent, params global::System.Type[] components)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            foreach (var component in components) gameObject.AddComponent(component);
            return gameObject;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetHorizontalBar(RectTransform rect, float height, float horizontalInset)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(horizontalInset, -height * 0.5f);
            rect.offsetMax = new Vector2(-horizontalInset, height * 0.5f);
        }

        private void NotifyPreview()
        {
            if (_suppressEvents || _working == null) return;
            _working.Normalize();
            UpdateValueLabels();
            _onPreview?.Invoke(_working.Snapshot());
        }

        private void UpdateValueLabels()
        {
            _masterValue.text = Percent(_working.MasterVolume);
            _bgmValue.text = Percent(_working.BgmVolume);
            _ambientValue.text = Percent(_working.AmbientVolume);
            _seValue.text = Percent(_working.SeVolume);
        }

        private void RestoreDefaults()
        {
            _working = new AudioSettingsData();
            _suppressEvents = true;
            _masterSlider.value = _working.MasterVolume;
            _bgmSlider.value = _working.BgmVolume;
            _ambientSlider.value = _working.AmbientVolume;
            _seSlider.value = _working.SeVolume;
            _muteToggle.isOn = false;
            _suppressEvents = false;
            NotifyPreview();
        }

        private void Apply()
        {
            _onApply?.Invoke(_working.Snapshot());
            Hide();
        }

        private void Cancel()
        {
            _onCancel?.Invoke();
            Hide();
        }

        public void OnCancel(UnityEngine.EventSystems.BaseEventData eventData)
        {
            if (IsOpen) Cancel();
        }

        private void Hide()
        {
            _onPreview = null;
            _onApply = null;
            _onCancel = null;
            gameObject.SetActive(false);
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    }
}
