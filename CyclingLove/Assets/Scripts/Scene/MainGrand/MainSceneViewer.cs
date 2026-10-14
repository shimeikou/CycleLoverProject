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
        private GamePlayerData _playerData;
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
            if (_playerData == null) return;
            var flow = new System.Service.ThreeDayGameService();
            if (actionButtonPrimary != null) actionButtonPrimary.interactable = !_busy && flow.CanChoose(_playerData, System.Service.ThreeDayGameService.Training);
            if (actionButtonSecondary != null) actionButtonSecondary.interactable = !_busy && flow.CanChoose(_playerData, System.Service.ThreeDayGameService.Town);
            if (actionButtonTertiary != null) actionButtonTertiary.interactable = !_busy;
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

        public void Render(GameSceneRequest request, GamePlayerData playerData, IReadOnlyList<MainSceneActionOption> actionOptions, string progressState)
        {
            _playerData = playerData;
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
                phaseText.text = request?.Phase == GameFlowPhase.Finished ? "3日間の試作・終了" : $"3日間の試作　{playerData.Day} / 3日";
            }

            if (dateText != null)
            {
                dateText.text = request?.Phase == GameFlowPhase.Finished ? "試作クリア" : $"4月{playerData.Day}日";
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
                debugText.text = playerData.MetHeroineIds.Contains("hero_bikeshop") ? "出会い：自転車屋の娘" : "この町で、新しい暮らしを。";
            }
        }

        private void ShowActionSelection(GameSceneRequest request, IReadOnlyList<MainSceneActionOption> actionOptions)
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

            ApplyActionOption(actionButtonPrimary, actionButtonPrimaryLabel, actionOptions, 0);
            ApplyActionOption(actionButtonSecondary, actionButtonSecondaryLabel, actionOptions, 1);
            ApplyActionOption(actionButtonTertiary, actionButtonTertiaryLabel, actionOptions, 2);
            UpdateActionAvailability();
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

        private void ApplyActionOption(Button button, TextMeshProUGUI label, IReadOnlyList<MainSceneActionOption> actionOptions, int index)
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
