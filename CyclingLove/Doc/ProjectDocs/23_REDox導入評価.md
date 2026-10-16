# REDox導入評価

確認日: 2026-10-16。公開mainの資料・構成と現在のプロジェクトを比較した静的評価。導入、Unityでのビルド、性能計測は行っていない。

## 判断

現在のゲーム実行側への採用は見送り。将来、Unityとは別プロセスで動くデータ変換ツールの候補として再評価する。現時点ではJSON編集を維持し、読み込み口の分離を優先する。

| 観点 | 確認内容と判断 |
| --- | --- |
| 実行環境 | REDoxは.NET 10以降を要求。現在のUnity 6000.6.1f1では生成されたGame.Domain.csprojがnetstandard2.1。公開パッケージをそのまま追加する構成は適合しない。独自移植を維持する負担は今の用途に見合わない |
| AOT | コアcsprojにはIsAotCompatible=trueの宣言がある。ただし、それだけでUnity IL2CPPへの対応を意味しない。AOT全般が非対応とは断定せず、Unity向け対応と実機ビルドは未確認とする |
| 現在の規模 | Contentの4 JSONは合計5,862 bytes。CampaignContentLoaderでResourcesから読み、JsonUtilityで復元・検証する。読み込みが性能問題であるという計測結果はない |
| 将来の用途 | 多形式の読み書き・編集・変換を行う制作ツールでは候補になる。採用するならUnity外の.NETツールに閉じ込め、Unityが読める形式を出力する。バイナリ出力には別途Unity対応の読み手が必要 |
| 配布要件 | シリアライズ形式の選択だけでは、AssetBundleの依存解決、取得、キャッシュ、解放、暗号鍵管理、真正性検証は完成しない。D01〜D03の設計は別途必要 |

根拠: [REDox README](https://github.com/CAPCOM-TD-OSS/REDox)、[共通ビルド設定](https://github.com/CAPCOM-TD-OSS/REDox/blob/main/src/Directory.Build.props)、[コアcsproj](https://github.com/CAPCOM-TD-OSS/REDox/blob/main/src/REDox/REDox.csproj)、[Unity 6000.6の.NET互換性](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/overview-of-dot-net-in-unity/dotnet-profile-support)。

公開ベンチマークは.NET 10のJIT環境でSystem.Text.Jsonと比較したもの。今のJsonUtilityやUnity Playerに同じ高速化率を適用できない。また、独自DOX形式は信頼済み入力専用と明記されている。外部配布に使う場合は、復号だけで信頼済みと扱わず、出所・真正性の検証を解析前に行う設計が必要。[REDoxの性能条件・Security節](https://github.com/CAPCOM-TD-OSS/REDox)

## 今整備する境界

1. ContentIdによる非同期取得と、JSON等の復元・検証を分ける。
2. Domainへ渡す型にREDox固有の型や配布場所を持ち込まない。
3. スキーマ版、コンテンツ版、セーブ互換性の判定を分ける。
4. 取得失敗時に現在のセッションを維持し、読み込んだ資源の解放責任を明確にする。

上記は整備方針であり、この評価で実装済みになった項目ではない。

## 再評価する条件

本番相当のイベント・機材・台詞データが揃い、起動・ロード時間やメモリに課題が出た時点で測定する。まず現行JSONの初回読み込み時間、割当量、ピークメモリ、圧縮後容量を取得する。変換ツールを試す場合も、日本語・数値・ID・参照・版が往復で維持されることと、Unity側の読み込みを確認する。

ゲーム側への再検討にはUnity互換ターゲットの提供、対象PCでのビルド成功、使用する場合はIL2CPPとコード除去への対応、実データでの改善を条件とする。現時点ではパッケージ追加やDOX形式への移行を行わない。
