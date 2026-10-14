using System.Collections.Generic;
using System.Flow;
using System.GameSession;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Scene.MainGrand
{
    public sealed class MainSceneViewer : MonoBehaviour
    {
        private UnityAction<string> _onSelectAction;
        private bool _busy;
        private int _actionPage;
        private Button _previousActions, _nextActions;
        private IReadOnlyList<GameActionOption> _actionOptions;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private TextMeshProUGUI messageText;

        public void BindSessionControls(UnityAction save, UnityAction title)
        {
            RebindButton(saveButton, save);
            RebindButton(titleButton, title);
            SetMessage("タイトルに戻る前に、保存してください。");
        }

        public void SetMessage(string message) { if (messageText != null) messageText.text = message; }
        public void SetBusy(bool busy)
        {
            _busy = busy;
            foreach (var b in new[] { saveButton, titleButton, novelContinueButton, miniGameContinueButton, summaryContinueButton })
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

            HideAllPanels();
        }

        public void Render(GameSceneRequest request, GamePlayerData playerData, IReadOnlyList<GameActionOption> actionOptions, string progressState)
        {
            _actionOptions = actionOptions;
            UpdateHud(request, playerData, progressState);
            HideAllPanels();

            if (request == null)
            {
                return;
            }

            switch (request.Phase)
            {
                case GameFlowPhase.ActionSelection:
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
                dateText.text = request?.Phase == GameFlowPhase.Finished ? "試作クリア" : request?.CurrentDateText;
            }

            if (timingText != null)
            {
                timingText.text = request?.Phase == GameFlowPhase.Finished ? "" : playerData.Timing switch { System.Data.Schedule.ActionTiming.Morning => "午前・仕事", System.Data.Schedule.ActionTiming.Afternoon => "午後・自由時間", _ => "夜・振り返り" };
            }

            if (playerStatusText != null && playerData != null)
            {
                playerStatusText.text =
                    $"資金　{playerData.Money} 円\n体力　{playerData.Stamina} / 100\n\n" +
                    $"フィジカル　{playerData.Physical}\nタフネス　{playerData.Toughness}\nスマート　{playerData.Smart}\nエンパシー　{playerData.Empathy}\nアピール　{playerData.Appeal}";
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
        }

        private void HideAllPanels()
        {
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
