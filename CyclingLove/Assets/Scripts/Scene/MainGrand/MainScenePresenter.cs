using System;
using System.Collections.Generic;
using System.Flow;
using System.GameSession;
using System.Manager;
using System.Service;
using CyclingLove.Equipment;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.MainGrand
{
    public sealed class MainScenePresenter : MonoBehaviour
    {
        [SerializeField] private MainSceneViewer view;
        private bool _busy;
        private int _shownRevision;
        private UtilityScreen _utilityScreen;

        private enum UtilityScreen
        {
            None,
            Hub,
            Shop,
            Equipment,
            Races,
            RaceResult
        }

        private async void Start()
        {
            try
            {
                await GameFlowManager.Instance.ResumeCurrentFlow();
                view.Init(HandleSelection,
                    () => Advance(GameFlowCompletionType.NovelClosed).Forget(),
                    () => { }, () => Advance(GameFlowCompletionType.SummaryClosed).Forget());
                view.BindSessionControls(() => Save().Forget(), () => ReturnToTitle().Forget(), ShowBikeHub);
                StandardUIManager.Instance?.HideLoading();
                Render();
            }
            catch (Exception ex) { Debug.LogException(ex); view?.SetMessage("画面を開始できませんでした。"); }
        }

        private void Render()
        {
            var request = GameFlowManager.Instance.BuildNextRequest();
            _shownRevision = request.Revision;
            view.Render(request, GameSession.PlayerData, GameFlowManager.Instance.GetActionOptions(), "");
        }

        private void HandleSelection(string id)
        {
            if (_busy) return;
            if (_utilityScreen != UtilityScreen.None) HandleUtilitySelection(id);
            else AdvanceSelection(id).Forget();
        }

        private UniTask AdvanceSelection(string id) =>
            GameFlowManager.Instance.CurrentPhase == GameFlowPhase.NovelChoice
                ? Advance(GameFlowCompletionType.NovelChoiceSelected, choice: id)
                : Advance(GameFlowCompletionType.ActionSelected, action: id);

        private void HandleUtilitySelection(string id)
        {
            if (id == "menu:close") { CloseUtility(); return; }
            if (id == "menu:hub") { ShowBikeHub(); return; }
            if (id == "menu:shop") { ShowShop(); return; }
            if (id == "menu:equipment") { ShowEquipment(); return; }
            if (id == "menu:races") { ShowRaces(); return; }
            if (id.StartsWith("buy:", StringComparison.Ordinal))
            {
                var result = GameFlowManager.Instance.PurchasePart(id.Substring(4));
                view.SetMessage(EquipmentMessage(result, "購入しました。"));
                ShowShop();
                return;
            }
            if (id.StartsWith("equip:", StringComparison.Ordinal))
            {
                var result = GameFlowManager.Instance.EquipPart(id.Substring(6));
                view.SetMessage(EquipmentMessage(result, "装備を変更しました。"));
                ShowEquipment();
                return;
            }
            if (id.StartsWith("race:", StringComparison.Ordinal))
                RunRace(id.Substring(5));
        }

        private void ShowBikeHub()
        {
            if (_busy) return;
            _utilityScreen = UtilityScreen.Hub;
            var performance = GameFlowManager.Instance.GetBikePerformance();
            var options = new List<GameActionOption>
            {
                Option("menu:shop", "ショップ", "機材を購入する"),
                Option("menu:equipment", "装備・性能", "所持パーツを比較して装備する"),
                Option("menu:races", "レース", "現在の能力と機材でレースに挑戦する"),
                Option("menu:close", "ゲームへ戻る", "現在の進行画面へ戻る")
            };
            ShowUtility("自転車", BikeOverview(performance), options);
        }

        private void ShowShop()
        {
            _utilityScreen = UtilityScreen.Shop;
            var data = GameSession.PlayerData;
            var options = new List<GameActionOption>();
            foreach (var part in GameFlowManager.Instance.GetBikeParts())
            {
                var owned = data.OwnedPartIds.Contains(part.Id);
                var comparison = GameFlowManager.Instance.CompareBikePart(part.Id);
                options.Add(new GameActionOption
                {
                    ActionId = "buy:" + part.Id,
                    Label = owned ? "所持済み｜" + part.Name : "購入｜" + part.Name,
                    Description = $"{SlotName(part.Slot)}　{part.Price:N0}円\n交換差分 {Stats(comparison.Delta, true)}" +
                                  (!owned && data.Money < part.Price ? "\n資金不足" : ""),
                    CanExecute = !owned && data.Money >= part.Price
                });
            }
            options.Add(Option("menu:hub", "戻る", "自転車メニューへ戻る"));
            ShowUtility("機材ショップ", $"所持金　{data.Money:N0}円", options);
        }

        private void ShowEquipment()
        {
            _utilityScreen = UtilityScreen.Equipment;
            var data = GameSession.PlayerData;
            var options = new List<GameActionOption>();
            foreach (var part in GameFlowManager.Instance.GetBikeParts())
            {
                if (!data.OwnedPartIds.Contains(part.Id)) continue;
                var equipped = IsEquipped(data.EquippedBike, part);
                var comparison = GameFlowManager.Instance.CompareBikePart(part.Id);
                options.Add(new GameActionOption
                {
                    ActionId = "equip:" + part.Id,
                    Label = equipped ? "装備中｜" + part.Name : "装備｜" + part.Name,
                    Description = $"{SlotName(part.Slot)}\n交換差分 {Stats(comparison.Delta, true)}",
                    CanExecute = !equipped
                });
            }
            options.Add(Option("menu:hub", "戻る", "自転車メニューへ戻る"));
            ShowUtility("装備・性能比較", BikeOverview(GameFlowManager.Instance.GetBikePerformance()), options);
        }

        private void ShowRaces()
        {
            _utilityScreen = UtilityScreen.Races;
            var data = GameSession.PlayerData;
            var options = new List<GameActionOption>();
            foreach (var race in GameFlowManager.Instance.GetRaces())
            {
                var preview = GameFlowManager.Instance.PreviewRace(race.Id);
                var attemptId = RaceAttemptId(data, race.Id);
                var attemptedToday = data.RaceAttempts?.Exists(record =>
                    record != null && record.AttemptId == attemptId) == true;
                options.Add(new GameActionOption
                {
                    ActionId = "race:" + race.Id,
                    Label = (attemptedToday ? "本日挑戦済み｜" : "挑戦｜") + race.Name,
                    Description = $"{CourseName(race.CourseType)} {race.DistanceKm}km　予測 {preview.Score} / 目標 {preview.TargetScore}\n成功報酬 {race.SuccessMoney:N0}円",
                    CanExecute = !attemptedToday
                });
            }
            options.Add(Option("menu:hub", "戻る", "自転車メニューへ戻る"));
            ShowUtility("レース", "挑戦すると結果が記録されます。同じレースへの挑戦は1日1回です。失敗してもゲームは終了しません。", options);
        }

        private void RunRace(string raceId)
        {
            var attemptId = RaceAttemptId(GameSession.PlayerData, raceId);
            var result = GameFlowManager.Instance.RunRace(raceId, attemptId);
            _utilityScreen = UtilityScreen.RaceResult;
            var title = result.Succeeded ? "レース成功" : "レース完走・目標未達";
            var body = $"スコア　{result.Score} / {result.TargetScore}\n" +
                       (result.Succeeded ? $"報酬　{result.RewardMoney:N0}円" : "報酬　なし") +
                       "\n結果は保存データと物語条件へ反映されました。";
            view.SetMessage(result.Succeeded ? "レース報酬を受け取りました。" : "結果を記録しました。翌日以降に再挑戦できます。");
            ShowUtility(title, body, new[]
            {
                Option("menu:races", "レース一覧へ", "別のレースまたは再挑戦を選ぶ"),
                Option("menu:close", "ゲームへ戻る", "現在の進行画面へ戻る")
            });
        }

        private void CloseUtility()
        {
            _utilityScreen = UtilityScreen.None;
            Render();
        }

        private void ShowUtility(string title, string body, IReadOnlyList<GameActionOption> options)
        {
            Render();
            view.ShowOptionPanel(title, body, options);
        }

        private static GameActionOption Option(string id, string label, string description) =>
            new() { ActionId = id, Label = label, Description = description, CanExecute = true };

        private static bool IsEquipped(PlayerBikeEquipment bike, BikePartDefinition part) => part.Slot switch
        {
            BikePartSlots.Frame => bike.FramePartId == part.Id,
            BikePartSlots.Components => bike.ComponentsPartId == part.Id,
            BikePartSlots.Wheels => bike.WheelPartId == part.Id,
            _ => false
        };

        private static string BikeOverview(BikePerformanceResult performance) =>
            $"総合性能　{Stats(performance.TotalPerformance, false)}\n熟練度　{performance.MasteryExperience} / 100（Lv.{performance.MasteryLevel}）\n脚質　{StyleName(performance.RiderStyle)}";

        private static string Stats(BikePerformanceStats stats, bool signed)
        {
            string F(int value) => signed ? value.ToString("+0;-0;0") : value.ToString();
            return $"剛性 {F(stats.Stiffness)} / 軽さ {F(stats.Lightness)} / 快適 {F(stats.Comfort)} / 操作 {F(stats.Handling)} / 空力 {F(stats.Aerodynamics)}";
        }

        private static string SlotName(string slot) => slot switch
        {
            BikePartSlots.Frame => "フレーム",
            BikePartSlots.Components => "コンポーネント",
            BikePartSlots.Wheels => "ホイール",
            _ => slot
        };

        private static string CourseName(string course) => course == "climb" ? "登坂" : "平坦";
        private static string RaceAttemptId(GamePlayerData data, string raceId) =>
            $"ui_{raceId}_{data.Year:0000}{data.Month:00}{data.Day:00}";
        private static string StyleName(RiderStyle style) => style switch
        {
            RiderStyle.Sprinter => "スプリンター",
            RiderStyle.Climber => "クライマー",
            RiderStyle.Endurance => "エンデュランス",
            RiderStyle.Technician => "テクニシャン",
            _ => "オールラウンド"
        };

        private static string EquipmentMessage(EquipmentOperationResult result, string success) => result.Status switch
        {
            EquipmentOperationStatus.Success => success,
            EquipmentOperationStatus.InsufficientFunds => "資金が不足しています。",
            EquipmentOperationStatus.AlreadyOwned => "すでに所持しています。",
            EquipmentOperationStatus.AlreadyEquipped => "すでに装備しています。",
            EquipmentOperationStatus.NotOwned => "所持していない機材です。",
            _ => "機材データを確認できませんでした。"
        };

        private async UniTask Advance(GameFlowCompletionType completion, string action = null, string choice = null)
        {
            if (_busy) return;
            if (GameFlowManager.Instance.CurrentPhase == GameFlowPhase.Finished)
            {
                await ReturnToTitle(); return;
            }
            _busy = true;
            view.SetBusy(true);
            try
            {
                GameFlowManager.Instance.SubmitSceneResult(new GameSceneResult
                {
                    CompletionType = completion, SelectedActionId = action, SelectedChoiceId = choice, ExpectedRevision = _shownRevision
                });
                Render();
                // One click cannot cascade through newly displayed panels in the same frame.
                await UniTask.Delay(150, ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException) { }
            finally { _busy = false; if (view != null) view.SetBusy(false); }
        }

        private async UniTask Save()
        {
            if (_busy) return;
            _busy = true; view.SetBusy(true);
            try { await GameSession.Save(); view.SetMessage("保存しました。続きから再開できます。"); }
            catch (Exception ex) { Debug.LogWarning($"Save failed: {ex.GetType().Name}"); view.SetMessage("保存できませんでした。空き容量などを確認して再度お試しください。"); }
            finally { _busy = false; if (view != null) view.SetBusy(false); }
        }

        private async UniTask ReturnToTitle()
        {
            if (_busy) return;
            _busy = true; view.SetBusy(true);
            await SceneManager.LoadSceneAsync("InitScene");
        }
    }
}
