using System.Collections.Generic;
using System.Linq;
using CyclingLove.Calendar;
using CyclingLove.Equipment;
using CyclingLove.Flow;
using CyclingLove.State;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace Scene.MainGrand
{
    public sealed class MainSceneViewer : MonoBehaviour
    {
        private UnityAction<string> _onSelectAction;
        private UnityAction<string> _onPreviewBikePart;
        private bool _busy;
        private int _actionPage;
        private Button _previousActions, _nextActions;
        private IReadOnlyList<GameActionOption> _actionOptions;
        private UnityEngine.UI.Image _backgroundImage;
        private AspectRatioFitter _backgroundAspect;
        private Sprite _defaultBackgroundSprite;
        private Color _defaultBackgroundColor;
        private UnityEngine.UI.Image _characterImage;
        private Vector2 _novelBodyOriginalSize;
        private Button _bikeMenuButton;
        private UnityEngine.UI.Button _skipButton;
        private GameObject _bikePreviewContainer;
        private BikePreviewController _bikePreview;
        private TextMeshProUGUI _bikePreviewCaption;
        private UnityEngine.UI.Button _bikePreviewResetButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private TextMeshProUGUI messageText;

        public void BindSessionControls(UnityAction save, UnityAction title, UnityAction bikeMenu, UnityAction skip)
        {
            RebindButton(saveButton, save);
            RebindButton(titleButton, title);
            EnsureBikeMenuButton(bikeMenu);
            EnsureSkipButton(skip);
            SetMessage("タイトルに戻る前に、保存してください。");
        }

        public void BindBikePreview(UnityAction<string> previewPart) => _onPreviewBikePart = previewPart;

        private void EnsureBikeMenuButton(UnityAction callback)
        {
            if (_bikeMenuButton == null && saveButton != null)
            {
                _bikeMenuButton = Instantiate(saveButton, saveButton.transform.parent);
                _bikeMenuButton.name = "BikeMenuButton";
                var rect = (RectTransform)_bikeMenuButton.transform;
                rect.anchoredPosition = ((RectTransform)saveButton.transform).anchoredPosition + new Vector2(-144, 0);
                var label = _bikeMenuButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "自転車";
            }
            RebindButton(_bikeMenuButton, callback);
            if (_bikeMenuButton != null) _bikeMenuButton.gameObject.SetActive(false);
        }

        private void EnsureSkipButton(UnityAction callback)
        {
            if (_skipButton == null && saveButton != null)
            {
                _skipButton = Instantiate(saveButton, saveButton.transform.parent);
                _skipButton.name = "SkipDaysButton";
                var rect = (RectTransform)_skipButton.transform;
                rect.anchoredPosition = ((RectTransform)saveButton.transform).anchoredPosition + new Vector2(-288, 0);
                var label = _skipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "日程";
            }
            RebindButton(_skipButton, callback);
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);
        }

        public void SetBikeMenuControl(bool visible)
        {
            if (_bikeMenuButton == null) return;
            _bikeMenuButton.gameObject.SetActive(visible);
            _bikeMenuButton.interactable = visible && !_busy;
        }

        public void SetSkipControl(bool visible)
        {
            if (_skipButton == null) return;
            _skipButton.gameObject.SetActive(visible);
            _skipButton.interactable = visible && !_busy;
        }

        public void SetMessage(string message) { if (messageText != null) messageText.text = message; }
        public void SetBusy(bool busy)
        {
            _busy = busy;
            foreach (var b in new[] { saveButton, titleButton, _bikeMenuButton, _skipButton, _bikePreviewResetButton, novelContinueButton, miniGameContinueButton, summaryContinueButton })
                if (b != null) b.interactable = !busy;
            UpdateActionAvailability();
        }
        private void UpdateActionAvailability()
        {
            if (_previousActions != null) _previousActions.interactable = !_busy;
            if (_nextActions != null) _nextActions.interactable = !_busy;
            var buttons = new[] { actionButtonPrimary, actionButtonSecondary, actionButtonTertiary };
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null) buttons[i].interactable = !_busy && _actionOptions != null && i + _actionPage * 3 < _actionOptions.Count && _actionOptions[i + _actionPage * 3].CanExecute;
        }

        private void EnsureActionPaging()
        {
            if (_nextActions != null || actionButtonPrimary == null) return;
            _previousActions = CreatePageButton("PreviousActions", "前の行動", 32, -1);
            _nextActions = CreatePageButton("NextActions", "次の行動", 490, 1);
        }
        private Button CreatePageButton(string name, string text, float x, int direction)
        {
            var button = Instantiate(actionButtonPrimary, actionButtonPrimary.transform.parent);
            button.name = name;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -530);
            rect.sizeDelta = new Vector2(400, 32);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) { label.text = text; label.fontSize = 18; }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (_busy) return; _actionPage += direction; RenderActionPage(); });
            return button;
        }
        private void RenderActionPage()
        {
            EnsureActionPaging();
            ApplyActionOption(actionButtonPrimary, actionButtonPrimaryLabel, _actionOptions, _actionPage * 3);
            ApplyActionOption(actionButtonSecondary, actionButtonSecondaryLabel, _actionOptions, _actionPage * 3 + 1);
            ApplyActionOption(actionButtonTertiary, actionButtonTertiaryLabel, _actionOptions, _actionPage * 3 + 2);
            if (_previousActions != null) _previousActions.gameObject.SetActive(_actionPage > 0);
            if (_nextActions != null) _nextActions.gameObject.SetActive(_actionOptions != null && (_actionPage + 1) * 3 < _actionOptions.Count);
            UpdateActionAvailability();
        }

        [Header("Base HUD")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI timingText;
        [SerializeField] private TextMeshProUGUI playerStatusText;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Panels")]
        [SerializeField] private GameObject actionSelectionPanel;
        [SerializeField] private GameObject novelPanel;
        [SerializeField] private GameObject miniGamePanel;
        [SerializeField] private GameObject summaryPanel;

        [Header("Action Selection")]
        [SerializeField] private TextMeshProUGUI actionTitleText;
        [SerializeField] private TextMeshProUGUI actionDescriptionText;
        [SerializeField] private Button actionButtonPrimary;
        [SerializeField] private TextMeshProUGUI actionButtonPrimaryLabel;
        [SerializeField] private Button actionButtonSecondary;
        [SerializeField] private TextMeshProUGUI actionButtonSecondaryLabel;
        [SerializeField] private Button actionButtonTertiary;
        [SerializeField] private TextMeshProUGUI actionButtonTertiaryLabel;

        [Header("Novel")]
        [SerializeField] private TextMeshProUGUI novelTitleText;
        [SerializeField] private TextMeshProUGUI novelBodyText;
        [SerializeField] private Button novelContinueButton;

        [Header("Mini Game")]
        [SerializeField] private TextMeshProUGUI miniGameTitleText;
        [SerializeField] private TextMeshProUGUI miniGameBodyText;
        [SerializeField] private Button miniGameContinueButton;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryBodyText;
        [SerializeField] private Button summaryContinueButton;

        public void Init(
            UnityAction<string> onSelectAction,
            UnityAction onNovelContinue,
            UnityAction onMiniGameContinue,
            UnityAction onSummaryContinue)
        {
            _onSelectAction = onSelectAction;
            BindActionButton(actionButtonPrimary, null);
            BindActionButton(actionButtonSecondary, null);
            BindActionButton(actionButtonTertiary, null);

            RebindButton(novelContinueButton, onNovelContinue);
            RebindButton(miniGameContinueButton, onMiniGameContinue);
            RebindButton(summaryContinueButton, onSummaryContinue);

            EnsureVisualLayers();

            HideAllPanels();
        }

        public void Render(GameSceneRequest request, GamePlayerData playerData, IReadOnlyList<GameActionOption> actionOptions, string progressState)
        {
            _actionOptions = actionOptions;
            UpdateHud(request, playerData, progressState);
            RenderVisuals(request);
            HideAllPanels();

            if (request == null)
            {
                return;
            }

            switch (request.Phase)
            {
                case GameFlowPhase.ActionSelection:
                case GameFlowPhase.NovelChoice:
                    ShowActionSelection(request, actionOptions);
                    break;
                case GameFlowPhase.Novel:
                    ShowNovel(request);
                    break;
                case GameFlowPhase.MiniGameAction:
                    ShowMiniGame(request);
                    break;
                case GameFlowPhase.Finished:
                case GameFlowPhase.DailySummary:
                    ShowSummary(request);
                    break;
            }
        }

        public void ShowOptionPanel(string title, string description, IReadOnlyList<GameActionOption> options)
        {
            _actionOptions = options;
            HideAllPanels();
            if (actionSelectionPanel != null) actionSelectionPanel.SetActive(true);
            if (actionTitleText != null) actionTitleText.text = title;
            if (actionDescriptionText != null) actionDescriptionText.text = description;
            _actionPage = 0;
            RenderActionPage();
        }

        public void ShowBikePreview(PlayerBikeEquipment loadout, string caption)
        {
            if (loadout == null || actionSelectionPanel == null) return;
            EnsureBikePreview();
            SetBikePreviewLayout(true);
            _bikePreviewContainer.SetActive(true);
            _bikePreview.SetLoadout(loadout);
            if (_bikePreviewCaption != null)
                _bikePreviewCaption.text = (caption ?? "現在の装備") + "\nドラッグ：回転　ホイール：拡大　ダブルクリック：リセット";
        }

        public void HideBikePreview()
        {
            if (_bikePreviewContainer != null) Destroy(_bikePreviewContainer);
            _bikePreviewContainer = null;
            _bikePreview = null;
            _bikePreviewCaption = null;
            _bikePreviewResetButton = null;
            SetBikePreviewLayout(false);
        }

        private void EnsureBikePreview()
        {
            if (_bikePreviewContainer != null) return;
            _bikePreviewContainer = new GameObject("BikePreviewPanel", typeof(RectTransform));
            _bikePreviewContainer.transform.SetParent(actionSelectionPanel.transform, false);
            var containerRect = (RectTransform)_bikePreviewContainer.transform;
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-24f, -82f);
            containerRect.sizeDelta = new Vector2(330f, 430f);

            var imageObject = new GameObject("BikePreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
            imageObject.transform.SetParent(_bikePreviewContainer.transform, false);
            var imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = new Vector2(0f, 1f);
            imageRect.anchorMax = new Vector2(1f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(0f, 330f);
            var rawImage = imageObject.GetComponent<UnityEngine.UI.RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = true;
            _bikePreview = imageObject.AddComponent<BikePreviewController>();
            _bikePreview.Initialize(rawImage);

            var captionObject = new GameObject("BikePreviewCaption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            captionObject.transform.SetParent(_bikePreviewContainer.transform, false);
            var captionRect = (RectTransform)captionObject.transform;
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 1f);
            captionRect.pivot = new Vector2(0.5f, 1f);
            captionRect.anchoredPosition = new Vector2(0f, -338f);
            captionRect.sizeDelta = new Vector2(330f, 54f);
            _bikePreviewCaption = captionObject.GetComponent<TextMeshProUGUI>();
            _bikePreviewCaption.font = actionTitleText != null ? actionTitleText.font : null;
            _bikePreviewCaption.fontSize = 14f;
            _bikePreviewCaption.alignment = TextAlignmentOptions.Center;
            _bikePreviewCaption.color = new Color(0.86f, 0.92f, 0.96f);
            _bikePreviewCaption.raycastTarget = false;

            if (actionButtonPrimary != null)
            {
                _bikePreviewResetButton = Instantiate(actionButtonPrimary, _bikePreviewContainer.transform);
                _bikePreviewResetButton.name = "BikePreviewResetButton";
                var resetRect = (RectTransform)_bikePreviewResetButton.transform;
                resetRect.anchorMin = resetRect.anchorMax = new Vector2(0.5f, 1f);
                resetRect.pivot = new Vector2(0.5f, 1f);
                resetRect.anchoredPosition = new Vector2(0f, -394f);
                resetRect.sizeDelta = new Vector2(160f, 32f);
                var label = _bikePreviewResetButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) { label.text = "視点をリセット"; label.fontSize = 15f; }
                var resetTrigger = _bikePreviewResetButton.GetComponent<EventTrigger>();
                if (resetTrigger != null) resetTrigger.triggers?.Clear();
                RebindButton(_bikePreviewResetButton, _bikePreview.ResetView);
            }

            _bikePreviewContainer.SetActive(false);
        }

        private void SetBikePreviewLayout(bool visible)
        {
            var width = visible ? 500f : 858f;
            if (actionTitleText != null) actionTitleText.rectTransform.sizeDelta = new Vector2(width, actionTitleText.rectTransform.sizeDelta.y);
            if (actionDescriptionText != null) actionDescriptionText.rectTransform.sizeDelta = new Vector2(width, actionDescriptionText.rectTransform.sizeDelta.y);
            foreach (var button in new[] { actionButtonPrimary, actionButtonSecondary, actionButtonTertiary })
                if (button != null) ((RectTransform)button.transform).sizeDelta = new Vector2(width, ((RectTransform)button.transform).sizeDelta.y);
            if (_previousActions != null)
            {
                var rect = (RectTransform)_previousActions.transform;
                rect.anchoredPosition = new Vector2(32f, -530f);
                rect.sizeDelta = new Vector2(visible ? 230f : 400f, 32f);
            }
            if (_nextActions != null)
            {
                var rect = (RectTransform)_nextActions.transform;
                rect.anchoredPosition = new Vector2(visible ? 302f : 490f, -530f);
                rect.sizeDelta = new Vector2(visible ? 230f : 400f, 32f);
            }
        }

        private void EnsureVisualLayers()
        {
            if (_characterImage != null || novelPanel == null) return;
            var canvas = GetComponentInParent<Canvas>();
            _backgroundImage = canvas == null ? null : canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .FirstOrDefault(image => image.name == "PrototypeBackground");
            if (_backgroundImage != null)
            {
                _defaultBackgroundSprite = _backgroundImage.sprite;
                _defaultBackgroundColor = _backgroundImage.color;
            }
            var go = new GameObject("CharacterPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(novelPanel.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(560, -70);
            rect.sizeDelta = new Vector2(310, 440);
            _characterImage = go.GetComponent<UnityEngine.UI.Image>();
            _characterImage.preserveAspect = true;
            _characterImage.raycastTarget = false;
            rect.SetSiblingIndex(1);
            if (novelBodyText != null) _novelBodyOriginalSize = novelBodyText.rectTransform.sizeDelta;
            go.SetActive(false);
        }

        private void RenderVisuals(GameSceneRequest request)
        {
            EnsureVisualLayers();
            if (_backgroundImage != null)
            {
                var background = VisualResourceResolver.LoadBackground(request?.BackgroundId);
                _backgroundImage.sprite = background != null ? background : _defaultBackgroundSprite;
                _backgroundImage.color = background != null ? Color.white : _defaultBackgroundColor;
                _backgroundImage.preserveAspect = false;
                if (background != null)
                {
                    _backgroundAspect ??= _backgroundImage.GetComponent<AspectRatioFitter>() ??
                        _backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
                    _backgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    _backgroundAspect.aspectRatio = background.rect.width / background.rect.height;
                    _backgroundAspect.enabled = true;
                }
                else if (_backgroundAspect != null) _backgroundAspect.enabled = false;
            }
            if (_characterImage == null) return;
            var show = request?.Phase == GameFlowPhase.Novel && !string.IsNullOrWhiteSpace(request.CharacterId);
            _characterImage.sprite = show
                ? VisualResourceResolver.LoadCharacter(request.CharacterId, request.OutfitId, request.ExpressionId)
                : null;
            show &= _characterImage.sprite != null;
            _characterImage.gameObject.SetActive(show);
            if (novelBodyText != null)
                novelBodyText.rectTransform.sizeDelta = show ? new Vector2(500, _novelBodyOriginalSize.y) : _novelBodyOriginalSize;
        }

        public void UpdateDebug(string debugMessage)
        {
            if (debugText != null)
            {
                debugText.text = debugMessage ?? "-";
            }
        }

        private void UpdateHud(GameSceneRequest request, GamePlayerData playerData, string progressState)
        {
            if (phaseText != null)
            {
                phaseText.text = request?.Phase == GameFlowPhase.Finished ? "終了" : $"進行　{request?.DayNumber} / {request?.TotalDays}日";
            }

            if (dateText != null)
            {
                dateText.text = request?.Phase == GameFlowPhase.Finished ? "クリア" : request?.DisplayDateText;
            }

            if (timingText != null)
            {
                timingText.text = request?.Phase == GameFlowPhase.Finished ? "" : playerData.Timing switch { CyclingLove.Calendar.ActionTiming.Morning => "午前・仕事", CyclingLove.Calendar.ActionTiming.Afternoon => "午後・自由時間", _ => "夜・振り返り" };
            }

            if (playerStatusText != null && playerData != null)
            {
                var heroine = playerData.HeroineProgresses?.FirstOrDefault(x => x != null && x.IsUnlocked);
                var relationship = heroine == null ? "" : $"\n\n好感度　{heroine.Affection} / 100\nルート段階　{heroine.RouteStep}";
                playerStatusText.text =
                    $"資金　{playerData.Money} 円\n体力　{playerData.Stamina} / {request?.MaxStamina}\n\n" +
                    $"フィジカル　{playerData.Physical}\nタフネス　{playerData.Toughness}\nスマート　{playerData.Smart}\nエンパシー　{playerData.Empathy}\nアピール　{playerData.Appeal}" + relationship;
            }

            if (debugText != null)
            {
                debugText.text = playerData.MetHeroineIds.Count > 0 ? $"出会った人　{playerData.MetHeroineIds.Count}人" : "この町で、新しい暮らしを。";
            }
        }

        private void ShowActionSelection(GameSceneRequest request, IReadOnlyList<GameActionOption> actionOptions)
        {
            if (actionSelectionPanel != null)
            {
                actionSelectionPanel.SetActive(true);
            }

            if (actionTitleText != null)
            {
                actionTitleText.text = request.DisplayName ?? "Action Selection";
            }

            if (actionDescriptionText != null)
            {
                actionDescriptionText.text = request.BodyText;
            }

            _actionPage = 0;
            RenderActionPage();
        }

        private void ShowNovel(GameSceneRequest request)
        {
            if (novelPanel != null)
            {
                novelPanel.SetActive(true);
            }

            if (novelTitleText != null)
            {
                novelTitleText.text = request.DisplayName ?? "Novel";
            }

            if (novelBodyText != null)
            {
                novelBodyText.text = request.BodyText;
                var label = novelContinueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = request.ContinueLabel;
            }
        }

        private void ShowMiniGame(GameSceneRequest request)
        {
            if (miniGamePanel != null)
            {
                miniGamePanel.SetActive(true);
            }

            if (miniGameTitleText != null)
            {
                miniGameTitleText.text = request.DisplayName ?? "Mini Game";
            }

            if (miniGameBodyText != null)
            {
                miniGameBodyText.text =
                    $"ActionId: {request.ActionId ?? "-"}\n\n" +
                    "ここにミニゲームやアクション実行UIを接続します。今は Continue ボタンで完了扱いです。";
            }
        }

        private void ShowSummary(GameSceneRequest request)
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }

            if (summaryTitleText != null)
            {
                summaryTitleText.text = request.DisplayName ?? "Summary";
            }

            if (summaryBodyText != null)
            {
                summaryBodyText.text = request.BodyText;
                var label = summaryContinueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = request.ContinueLabel;
            }
        }

        private void ApplyActionOption(Button button, TextMeshProUGUI label, IReadOnlyList<GameActionOption> actionOptions, int index)
        {
            if (button == null)
            {
                return;
            }

            var hasOption = actionOptions != null && index < actionOptions.Count;
            button.gameObject.SetActive(hasOption);

            if (!hasOption)
            {
                return;
            }

            var option = actionOptions[index];
            if (label != null)
            {
                label.text = option.Label + "\n" + option.Description;
            }

            BindActionButton(button, option.ActionId);
            BindBikePreviewTrigger(button, option.ActionId);
        }

        private void BindBikePreviewTrigger(UnityEngine.UI.Button button, string actionId)
        {
            var trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();
            if (_onPreviewBikePart == null || string.IsNullOrWhiteSpace(actionId)) return;
            var prefixLength = actionId.StartsWith("buy:") ? 4 : actionId.StartsWith("equip:") ? 6 : 0;
            if (prefixLength == 0) return;
            var partId = actionId.Substring(prefixLength);
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => _onPreviewBikePart.Invoke(partId));
            trigger.triggers.Add(entry);
        }

        private void HideAllPanels()
        {
            HideBikePreview();
            if (actionSelectionPanel != null) actionSelectionPanel.SetActive(false);
            if (novelPanel != null) novelPanel.SetActive(false);
            if (miniGamePanel != null) miniGamePanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        private void RebindButton(Button button, UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (callback != null)
            {
                button.onClick.AddListener(callback);
            }
        }

        private void BindActionButton(Button button, string actionId)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (_onSelectAction != null && !string.IsNullOrWhiteSpace(actionId))
            {
                button.onClick.AddListener(() => _onSelectAction.Invoke(actionId));
            }
        }
    }
}
