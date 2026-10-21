# MainGameScene 配置メモ

> **2026-10-16 注記:** 本書の配置図は初期版。現行のMainGameSceneには背景、保存・タイトル・メッセージ欄、会話選択フェーズがあり、自転車メニューのボタン・ページ送り・立ち絵は実行時に生成する（`MainSceneViewer.cs`）。レースは `MiniGamePanel` ではなく行動パネル側で表示する。

日付HUDは既存の `DateText` を使い、年間版では `4月1日（春・平日）`、季節締切では `6月30日（春・平日・特別日）` の形式で表示する。専用GameObjectの追加は不要。

この文書は、`MainGameScene` を仮UIで動かすために、
Unity エディタ上で何を置いて、何を Inspector で結線するかをまとめたメモです。

## 目的

- `ActionSelection`
- `Novel`
- `MiniGameAction`
- `DailySummary`

の4フェイズを、`MainGameScene` 1つの中で切り替えて確認できるようにする。

## 追加したスクリプト

- [MainSceneViewer.cs](../../../Assets/Scripts/Scene/MainGrand/MainSceneViewer.cs)

## シーン上で必要な構成

`MainGameScene` に以下のような GameObject を置く。

```text
MainScenePresenter
MainSceneViewer
Canvas
  BaseHUD
    PhaseText
    DateText
    TimingText
    PlayerStatusText
    DebugText
  ActionSelectionPanel
    TitleText
    DescriptionText
    ActionButtonPrimary
      Label
    ActionButtonSecondary
      Label
    ActionButtonTertiary
      Label
  NovelPanel
    TitleText
    BodyText
    ContinueButton
  MiniGamePanel
    TitleText
    BodyText
    ContinueButton
  SummaryPanel
    TitleText
    BodyText
    ContinueButton
```

## 参照アサイン

### MainScenePresenter

- `View`
  - `MainSceneViewer` を持つ GameObject をアサイン

### MainSceneViewer

Base HUD:

- `Phase Text`
- `Date Text`
- `Timing Text`
- `Player Status Text`
- `Debug Text`

Panels:

- `Action Selection Panel`
- `Novel Panel`
- `Mini Game Panel`
- `Summary Panel`

Action Selection:

- `Action Title Text`
- `Action Description Text`
- `Action Button Primary`
- `Action Button Primary Label`
- `Action Button Secondary`
- `Action Button Secondary Label`
- `Action Button Tertiary`
- `Action Button Tertiary Label`

Novel:

- `Novel Title Text`
- `Novel Body Text`
- `Novel Continue Button`

Mini Game:

- `Mini Game Title Text`
- `Mini Game Body Text`
- `Mini Game Continue Button`

Summary:

- `Summary Title Text`
- `Summary Body Text`
- `Summary Continue Button`

## 推奨コンポーネント

- テキスト: `TextMeshProUGUI`
- ボタン: `Button`
- パネル: `GameObject` または `RectTransform` を持つ通常 UI

## 最初の確認ポイント

1. `InitScene` の Start から入る
2. `MainGameScene` が開く
3. フェイズに応じて Panel が1つだけ表示される
4. `ActionSelection` でいずれかのボタンを押せる
5. `Novel` で Continue を押せる
6. `Summary` で Continue を押せる

## いまの注意点

- 見た目はまだ仮でよい
- ボタンの文言も仮でよい
- 配色やレイアウト調整より、まず進行がつながることを優先する
- 本番表示の解像度、セーフエリア、素材寸法は [25_macOS表示・素材制作仕様.md](../素材/25_macOS表示・素材制作仕様.md) に従う
