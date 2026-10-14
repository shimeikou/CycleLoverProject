using System.Collections.Generic;
using System.Data.Schedule;
using System.Flow;
using System.GameSession;
using System.Linq;

namespace System.Service
{
    // A bounded, playable prototype. Annual schedule content remains separate.
    public sealed class ThreeDayGameService
    {
        public const int Version = 1;
        public const string Training = "act_training_roadwork_001";
        public const string Town = "act_town_walk_001";
        public const string Rest = "act_rest_home_001";
        public static readonly string[] Actions = { Training, Town, Rest };

        public GamePlayerData NewGame()
        {
            var data = new GamePlayerData { FlowVersion = Version, SavedPhase = GameFlowPhase.Novel };
            data.DayStartStats = Stats(data);
            return data;
        }

        public void Validate(GamePlayerData data)
        {
            if (data == null || data.FlowVersion != Version)
                throw new InvalidOperationException("この試作に対応したセーブデータではありません。");
            if (data.Year != 2026 || data.Month != 4 || data.Day < 1 || data.Day > 4 ||
                data.NarrativePage < 0 || data.NarrativePage > 1 || data.Revision < 0 ||
                data.Stamina < 0 || data.Stamina > 100 || data.Money < 0 ||
                data.DayStartStats == null || data.DayStartStats.Length != 7 ||
                data.ClearedEventKeys == null || data.ActiveRouteFlags == null || data.MetHeroineIds == null)
                throw new InvalidOperationException("セーブデータの内容を確認できませんでした。");
            bool valid = data.SavedPhase switch
            {
                GameFlowPhase.Novel => data.Day <= 3 && (data.Timing == ActionTiming.Morning ||
                    data.Timing == ActionTiming.Afternoon && Actions.Contains(data.SelectedActionId)),
                GameFlowPhase.ActionSelection => data.Day <= 3 && data.Timing == ActionTiming.Afternoon && data.NarrativePage == 0,
                GameFlowPhase.DailySummary => data.Day <= 3 && data.Timing == ActionTiming.Evening && data.NarrativePage == 0,
                GameFlowPhase.Finished => data.Day == 4 && data.Timing == ActionTiming.Morning && data.NarrativePage == 0,
                _ => false
            };
            if (!valid) throw new InvalidOperationException("セーブデータの進行位置が不正です。");
        }

        public bool CanChoose(GamePlayerData data, string action) => action switch
        {
            Training => data.Stamina >= 20,
            Town => data.Stamina >= 10 && data.Money >= 100,
            Rest => true,
            _ => false
        };

        public GameSceneRequest Request(GamePlayerData data)
        {
            Validate(data);
            var phase = data.SavedPhase;
            var title = phase switch
            {
                GameFlowPhase.ActionSelection => "午後は、何をしよう？",
                GameFlowPhase.DailySummary => $"{data.Day}日目の振り返り",
                GameFlowPhase.Finished => "新しい暮らしの、はじまり",
                _ => data.Timing == ActionTiming.Morning ? new[] { "祖母の家で迎える朝", "暮らしのリズム", "少しずつ、この町へ" }[data.Day - 1] : ActionName(data.SelectedActionId)
            };
            return new GameSceneRequest
            {
                Phase = phase, SceneType = phase == GameFlowPhase.ActionSelection ? GameFlowSceneType.ActionSelection :
                    phase == GameFlowPhase.DailySummary || phase == GameFlowPhase.Finished ? GameFlowSceneType.Summary : GameFlowSceneType.Novel,
                UnitySceneName = "MainGameScene", Timing = data.Timing,
                CurrentDateText = $"4月{data.Day}日", DisplayName = title, Revision = data.Revision,
                EventKey = $"trial_day{data.Day}_{data.Timing}", ActionId = data.SelectedActionId,
                BodyText = phase switch
                {
                    GameFlowPhase.Novel => Narrative(data)[data.NarrativePage],
                    GameFlowPhase.DailySummary => Summary(data),
                    GameFlowPhase.Finished => "祖父のロードバイクと、この町で出会った人たち。\n新しい毎日は、まだ始まったばかり。\n\n3日間の試作はここまでです。\n保存すると、この終了地点から再開できます。",
                    _ => "練習で体を鍛える。町で人とつながる。家でゆっくり休む。\n今日の選択が、明日の自分を少し変えていく。"
                },
                ContinueLabel = phase == GameFlowPhase.DailySummary ? "翌日へ" : phase == GameFlowPhase.Finished ? "タイトルへ" : data.NarrativePage == 0 ? "次へ" : "読み終える"
            };
        }

        public bool Submit(GamePlayerData data, GameSceneResult input)
        {
            Validate(data);
            if (input == null || input.ExpectedRevision != data.Revision) return false;
            switch (data.SavedPhase)
            {
                case GameFlowPhase.ActionSelection:
                    if (input.CompletionType != GameFlowCompletionType.ActionSelected || !CanChoose(data, input.SelectedActionId)) return false;
                    data.SelectedActionId = input.SelectedActionId;
                    data.SavedPhase = GameFlowPhase.Novel;
                    break;
                case GameFlowPhase.Novel:
                    if (input.CompletionType != GameFlowCompletionType.NovelClosed) return false;
                    if (data.NarrativePage == 0) { data.NarrativePage = 1; break; }
                    CompleteEvent(data);
                    data.NarrativePage = 0;
                    if (data.Timing == ActionTiming.Morning)
                    {
                        data.Timing = ActionTiming.Afternoon;
                        data.SavedPhase = GameFlowPhase.ActionSelection;
                    }
                    else
                    {
                        data.Timing = ActionTiming.Evening;
                        data.SavedPhase = GameFlowPhase.DailySummary;
                    }
                    break;
                case GameFlowPhase.DailySummary:
                    if (input.CompletionType != GameFlowCompletionType.SummaryClosed) return false;
                    // Overnight recovery belongs to the next day's opening, not the displayed daily delta.
                    data.Day++;
                    data.Timing = ActionTiming.Morning;
                    data.SelectedActionId = null;
                    data.Stamina = Math.Min(100, data.Stamina + 20);
                    data.DayStartStats = Stats(data);
                    data.SavedPhase = data.Day > 3 ? GameFlowPhase.Finished : GameFlowPhase.Novel;
                    break;
                default: return false;
            }
            data.Revision++;
            return true;
        }

        private static void CompleteEvent(GamePlayerData d)
        {
            string key = $"trial_day{d.Day}_{d.Timing}";
            if (d.ClearedEventKeys.Contains(key)) return;
            if (d.Timing == ActionTiming.Morning) { d.Money += 500; d.Smart++; }
            else
            {
                switch (d.SelectedActionId)
                {
                    case Training: d.Physical += 2; d.Toughness++; d.Stamina -= 20; break;
                    case Town: d.Empathy += 2; d.Appeal++; d.Money -= 100; d.Stamina -= 10; break;
                    case Rest: d.Toughness++; d.Stamina = Math.Min(100, d.Stamina + 30); break;
                }
                if (d.Day == 3)
                {
                    if (!d.MetHeroineIds.Contains("hero_bikeshop")) d.MetHeroineIds.Add("hero_bikeshop");
                    if (!d.ActiveRouteFlags.Contains("route_bikeshop_intro_started")) d.ActiveRouteFlags.Add("route_bikeshop_intro_started");
                }
            }
            d.ClearedEventKeys.Add(key);
        }

        private static string[] Narrative(GamePlayerData d)
        {
            if (d.Timing == ActionTiming.Morning)
                return new[] {
                    new[] { "窓の向こうから、鳥の声が聞こえる。\n祖母の家で迎えた朝は、都会で過ごした日々よりも静かだった。\n\n祖母「急がなくていいよ。まずは朝ごはんにしようか」", "食事を終え、持ってきたパソコンを開く。\n顔なじみの取引先から届いた依頼を、一つずつ片づけていく。\n\n今日の仕事を終えた。午後は、自分のために使おう。\n［資金 +500 ／ スマート +1］" },
                    new[] { "朝の光が、机の上を照らしている。\n昨日見つけた祖父のロードバイクが、少し気になった。\n\nまずは仕事。午後にまた、あの自転車を見てみよう。", "予定していた修正を終え、取引先に成果を送った。\n祖母が置いてくれたお茶は、まだ温かい。\n\nこういう暮らしも、悪くない。\n［資金 +500 ／ スマート +1］" },
                    new[] { "この家の朝にも、少し慣れてきた。\n窓を開けると、遠くに山の景色が見える。\n\nいつか、あの向こうまで走れるだろうか。", "午前の依頼を仕上げて、パソコンを閉じる。\n祖母から、自転車屋へ寄ってみるよう勧められた。\n\n今日は、町に新しい知り合いができるかもしれない。\n［資金 +500 ／ スマート +1］" }
                }[d.Day - 1];
            string first = d.Day switch
            {
                1 => "物置の奥で、ほこりをかぶったロードバイクを見つけた。\n祖母「おじいちゃんが、大切にしていた自転車だよ」\n\nすぐには乗れそうにない。まずは、自分にできることから始めよう。",
                2 => "祖父の自転車を拭いていると、細かな傷に気づいた。\nどんな景色を、一緒に見てきたのだろう。\n\n修理のことは専門の人に聞くとして、今日の午後を過ごそう。",
                _ => "祖母に教わった自転車屋に寄ると、娘さんが顔を上げた。\n\n娘「古い自転車？ まずは状態を見せてください。乗る前に点検です」\n\n真剣な声にうなずく。新しい一歩を踏み出せた気がした。"
            };
            string last = d.SelectedActionId switch
            {
                Training => "自転車に乗る準備として、川沿いで基礎トレーニングをした。\n息は上がったけれど、風が心地いい。\n\n昨日より、少し遠くまで行けそうだ。\n［フィジカル +2 ／ タフネス +1 ／ 体力 -20］",
                Town => "町を歩き、店先で地元の人と話した。\n百円のおやつを買って、教えてもらった道を覚える。\n\n知らなかった町に、顔なじみが増えていく。\n［エンパシー +2 ／ アピール +1 ／ 資金 -100 ／ 体力 -10］",
                _ => "縁側に座って、ゆっくりと体を休めた。\n何もしない時間にも、意味があるのかもしれない。\n\n明日は、また自分のペースで動こう。\n［タフネス +1 ／ 体力 +30（上限100）］"
            };
            return new[] { first, last };
        }

        public static string ActionName(string id) => id switch { Training => "基礎練習", Town => "町へ行く", Rest => "家で休む", _ => "自由行動" };
        private static int[] Stats(GamePlayerData d) => new[] { d.Money, d.Physical, d.Toughness, d.Smart, d.Empathy, d.Appeal, d.Stamina };
        private static string Summary(GamePlayerData d)
        {
            var now = Stats(d);
            var names = new[] { "資金", "フィジカル", "タフネス", "スマート", "エンパシー", "アピール", "体力" };
            var lines = new List<string> { "午後の行動：" + ActionName(d.SelectedActionId), "" };
            for (int i = 0; i < now.Length; i++) { int delta = now[i] - d.DayStartStats[i]; if (delta != 0) lines.Add($"{names[i]}　{delta:+0;-0;0}　（現在 {now[i]}）"); }
            if (d.Day == 3) lines.Add("\n自転車屋の娘と知り合った。");
            lines.Add("\n翌朝は体力が20回復します（上限100）。");
            return string.Join("\n", lines);
        }
    }
}
