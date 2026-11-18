# WorkLog

作業時間を記録・集計するためのデスクトップアプリです。WinUI 3 クライアントから ASP.NET Core Web API を経由して PostgreSQL に保存する 3 層構成で、**学習目的で作成していますが、実用に耐える構成を目指しています**。

> [!NOTE]
> このリポジトリは学習用サンプルです。本番運用する場合は、認証・認可、HTTPS 証明書、バックアップ、ログの秘匿設定などを追加してください。

## 現状のステータス

| 領域 | 状態 | 備考 |
| --- | --- | --- |
| プロジェクト構成（Client / Api / Infrastructure） | ✅ 実装済み | 3 プロジェクト + `Directory.Packages.props` によるバージョン一元管理 |
| Web API のホスト構成 | ✅ 実装済み | Controllers + OpenAPI + Npgsql 接続の DI 登録まで |
| データベースモデル | ⬜ 未実装 | `WorkLogDbContext` は空の実装 |
| EF Core マイグレーション | ⬜ 未実装 | 下記「マイグレーション」参照 |
| 作業時間の記録機能 | ⬜ 未実装 | Client は WinUI テンプレートのサンプル（カウンター）のみ |
| 集計・レポート機能 | ⬜ 未実装 | |
| 認証・認可 | ⬜ 未実装 | |
| テスト | ⬜ 未整備 | |

## 機能

### 実装済み

- WinUI 3 によるデスクトップ UI の雛形（`WorkLog.Client/`）
- ASP.NET Core Web API のホスト（Controllers + OpenAPI、Development で `/openapi/v1.json`）
- PostgreSQL 向け EF Core プロバイダの初期設定（`AddInfrastructure()`）

### 計画（TODO）

- [ ] 作業ログの CRUD（プロジェクト / タスク / 開始・終了時刻 / メモ）
- [ ] 日次・週次・月次の集計ビュー
- [ ] 分類別（タグ・プロジェクト別）の工数集計
- [ ] CSV / JSON でのエクスポート
- [ ] オフライン編集と同期
- [ ] 認証（ユーザー単位的数据分離）
- [ ] テスト（単体 + API 統合）

## アーキテクチャ

```mermaid
graph LR
	A[WorkLog<br/>WinUI 3 クライアント] -->|HTTP / JSON| B[WorkLog.Api<br/>ASP.NET Core Web API]
	B --> C[WorkLog.Infrastructure<br/>EF Core + Npgsql]
	C --> D[(PostgreSQL)]
```

| プロジェクト | 種別 | 役割 |
| --- | --- | --- |
| `WorkLog.Client/` | WinUI 3 アプリ | UI と ViewModel。HTTP クライアントのみを持ち、DB には直接接続しない |
| `WorkLog.Api/` | ASP.NET Core Web API | HTTP エンドポイント、認証・認可、入力検証 |
| `WorkLog.Infrastructure/` | クラスライブラリ | `DbContext`、エンティティ、リポジトリ、`AddInfrastructure()` |

> [!IMPORTANT]
> EF Core と Npgsql は **Infrastructure に閉じ込めてください**。Client は Release で `PublishTrimmed=True` が有効になっており、リフレクションを多用する EF Core を Client に載せるのは推奨されません。

## 技術スタック

| 区分 | 技術 | バージョン |
| --- | --- | --- |
| クライアント TFM | `net10.0-windows10.0.26100.0`（最小 `10.0.17763.0`） | .NET 10 |
| API / Infrastructure TFM | `net10.0` | .NET 10 |
| UI フレームワーク | WinUI 3（Windows App SDK） | 2.6.5 |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 |
| O/R マッパー | Entity Framework Core | 10.0.15 |
| DB プロバイダ | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| OpenAPI | Microsoft.AspNetCore.OpenApi | 10.0.15 |
| DI 抽象 | Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.15 |
| データベース | PostgreSQL | <!-- TODO: 対象バージョンを決める（例: 16 / 17） --> |

バージョンはすべて [`Directory.Packages.props`](Directory.Packages.props) で一元管理しています。

## 必須環境

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 以降 + **ASP.NET and web development** および **.NET Desktop Development** ワークロード
- Windows 10 バージョン 1809（ビルド 17763）以降
- PostgreSQL サーバー（ローカル開発用で可）

## クイックスタート

### 1. 依存関係の復元

```powershell
dotnet restore WorkLog.slnx
```

### 2. 接続文字列の設定

認証情報はリポジトリにコミットせず、User Secrets または環境変数で設定してください。

```powershell
# API プロジェクトで User Secrets を初期化（初回のみ）
dotnet user-secrets init --project WorkLog.Api

# 接続文字列を設定
dotnet user-secrets set "ConnectionStrings:WorkLog" "Host=localhost;Port=5432;Database=worklog;Username=<user>;Password=<password>" --project WorkLog.Api
```

環境変数で代用する場合:

```powershell
$env:ConnectionStrings__WorkLog = "Host=localhost;Port=5432;Database=worklog;Username=<user>;Password=<password>"
```

接続文字列が未設定のまま API を起動すると、`ConnectionStrings:WorkLog` が不足している旨の例外で即座に失敗します。

### 3. API の起動

```powershell
dotnet run --project WorkLog.Api
```

- 既定の起動 URL: `https://localhost:54386` / `http://localhost:54387`（`WorkLog.Api/Properties/launchSettings.json`）
- Development 環境では OpenAPI ドキュメントが `https://localhost:54386/openapi/v1.json` で参照できます

詳細は [WorkLog.Api/README.md](WorkLog.Api/README.md) を参照してください。

### 4. クライアントの起動

Visual Studio で `WorkLog.Client` をスタートアッププロジェクトに設定して F5、または:

```powershell
dotnet run --project WorkLog.Client
```

## マイグレーション

EF Core のマイグレーションには、**起動プロジェクト（`WorkLog.Api`）に `Microsoft.EntityFrameworkCore.Design` への参照が必要**です。現状 `WorkLog.Api.csproj` は未参照のため、初回は 1 行追加してください。

```xml
<!-- WorkLog.Api/WorkLog.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
	<PrivateAssets>all</PrivateAssets>
	<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

ツールは EF Core と同じ系列のバージョンに固定します。

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15

# 初期マイグレーションの作成
dotnet ef migrations add InitialCreate `
  --project WorkLog.Infrastructure `
  --startup-project WorkLog.Api

# 適用
dotnet ef database update `
  --project WorkLog.Infrastructure `
  --startup-project WorkLog.Api
```

## ビルド

```powershell
dotnet build WorkLog.slnx
```

### 動作確認済みのコマンド

```powershell
# MSIX パッケージ生成を無効化してビルド（この環境では成功を確認済み）
dotnet build WorkLog.slnx -p:EnableMsixTooling=false -p:Platform=x64
```

### 既知の問題

| 事象 | 内容 | 回避策 / 状態 |
| --- | --- | --- |
| `APPX0002` / `MSB4018` | MSIX パッケージ生成タスク `WinAppSdkGenerateAppxPackageRecipe` が「指定されたモジュールが見つかりません」で失敗する | `-p:EnableMsixTooling=false` で回避。**環境側のツールセットに起因する可能性が高く、未調査** |
| `MVVMTK0045`（警告 2 件） | `[ObservableProperty]` が WinRT の AOT シナリオで非対応 | WinUI 3 では partial プロパティへ移行することが MVVM Toolkit のドキュメントで推奨されている。**未対応** |

## パッケージバージョン管理の方針

- すべてのバージョンは `Directory.Packages.props`（Central Package Management）で管理し、`.csproj` には `Version` 属性を書きません。
- **EF Core / Npgsql は .NET 10 対応版にピン留めしています**（EF Core `10.0.15`、Npgsql `10.0.3`）。nuget.org 上の最新安定版は EF Core `11.0.0` ですが、**EF Core 11 は `net11.0` 専用アセットのため .NET 10 では使用できません**。
- Npgsql プロバイダは EF Core を `[10.0.4, 11.0.0)` の範囲で要求するため、EF Core を 11 系に上げると解決に失敗します。EF Core 11 へ移行する場合は、Npgsql の安定版リリースを待ってください。
- DB 系パッケージに `Version="*"`（フロート）は使用しません。

## 開発ルール（TODO: 要決定）

- ブランチ戦略: <!-- TODO: 例 trunk-based / GitHub Flow -->
- コミット規約: <!-- TODO: 例 Conventional Commits -->
- コードスタイル: `dotnet format` を統一ツールとして使用
- レビュー: <!-- TODO: PR ルール -->

## セキュリティ上の注意

- 接続文字列・API キーをソースコードにコミットしないでください。User Secrets（開発時）と環境変数（運用時）を使用します。
- API の入出力には DTO を使用し、エンティティを直接公開しないでください（over-posting 対策）。
- Swagger / OpenAPI UI は Development 環境でのみ有効化しています。本番で構造を公開しないでください。
- <!-- TODO: 脆弱性報告の手順（SECURITY.md）を決める -->

## テスト（未整備）

<!-- TODO: テストプロジェクトを追加する。EF Core のテストでは InMemory プロバイダは本番用途・統合テストの代替として推奨されていない点に注意。
	 参考: https://learn.microsoft.com/ef/core/testing/ -->

| プロジェクト | 計画 |
| --- | --- |
| `WorkLog.Tests` | ViewModel / 変換ロジックの単体テスト |
| `WorkLog.Api.Tests` | エンドポイントの統合テスト |
| `WorkLog.Infrastructure.Tests` | `DbContext` のクエリテスト |

## ライセンス

<!-- TODO: ライセンスを選択してください（例: MIT）。未定の間はこの行を削除しないでください。 -->

未定 / Undecided

## 参考ドキュメント

- [Tutorial: Create a controller-based web API with ASP.NET Core](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0)
- [Create web APIs with ASP.NET Core（`[ApiController]` の挙動）](https://learn.microsoft.com/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Routing to controller actions in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing?view=aspnetcore-10.0)
- [What's new in EF Core 10](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/index.html)
- [Introduction to the MVVM Toolkit](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [.NET dependency injection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
