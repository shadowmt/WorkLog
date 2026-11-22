## 日時・時刻の取り扱い（TimeProvider）

日時や時刻に依存する処理では、テストの再現性を確保するため、必ず .NET の `TimeProvider` を使用すること。

### 基本ルール

- アプリケーションコードや業務ロジックから、`DateTime.Now`、`DateTime.UtcNow`、`DateTimeOffset.Now`、`DateTimeOffset.UtcNow` などを直接呼び出さない。
- 現在日時を取得する必要がある場合は、`TimeProvider` をDI経由で注入し、`GetUtcNow()` などのメソッドを使用する。
- 本番環境では `TimeProvider.System` をDIに登録する。
- タイムゾーンを考慮する場合は、JSTなどの対象タイムゾーンを明示的に指定して変換する。暗黙のローカルタイムゾーンに依存しないこと。
- 日時に依存する処理では、現在日時を取得する箇所を明確にし、必要以上に複数回取得しないこと。

### テストルール

- 日時に依存するテストでは、実際のシステム時計を使用しない。
- `Microsoft.Extensions.TimeProvider.Testing` パッケージの `FakeTimeProvider` を使用する。
- テストごとに基準日時を明示的に設定し、同じテストを何度実行しても同じ結果になるようにする。
- 必要に応じて `FakeTimeProvider.Advance()` を使用し、時間経過を再現する。
- 月末、年末、年またぎ、閏年、タイムゾーン変換などの境界条件も、固定した日時を使って検証する。
- テスト用の `FakeTimeProvider` は本番コードに登録しない。

### AIエージェントへの要求

新しいコードの実装・既存コードの修正・コードレビューでは、必ずこのルールを確認すること。

- システム時計への直接依存があれば、`TimeProvider` を使用する設計に修正すること。
- 日時に依存するテストが実際の現在日時に依存していないか確認すること。
- 新しい日時依存処理を追加する場合は、`FakeTimeProvider` でテスト可能な設計にすること。
- このルールに反する実装を、理由なく許容しないこと。

## エンティティの状態変更（部分更新の禁止）

エンティティの変更系メソッドでは、検証と値の計算をすべて完了してから、状態への代入を開始すること。検証が途中で失敗した場合、エンティティは呼び出し元に渡された状態のまま変更されていなければならない。

### 基本ルール

- 検証・正規化・計算をすべて終えてから、プロパティへの代入を開始する。
- 代入の途中で例外になり得る処理（時刻の正規化、範囲チェック、外部値の検証など）をメソッド末尾に置いてはいけない。
- 代入の途中で失敗すると、一部だけが変わった不正な状態のエンティティが呼び出し元に渡る。この状態は呼び出し側が例外を拾っても気づきにくく、テストでも見逃されやすい。
- 計算結果はローカル変数に退避し、直前に代入したプロパティを読み戻して再計算しない。後続の正規化変更で静かに壊れる原因になる。
- コンストラクタは例外時にオブジェクトが呼び出し元へ渡らないため部分更新の実害はないが、変更系メソッドと同じ形に揃える。
- 既存の `WorkLogEntry.Validate()` のように、検証をメソッド冒頭に集約する形が基準となる。

### 悪い例

```csharp
public void Update(string name, string? description, DateTimeOffset now)
{
    Name = NormalizeName(name);
    NormalizedName = Name.ToUpperInvariant();
    Description = NormalizeDescription(description);

    // ここで例外になると、Name と Description はすでに変更済みになる
    UpdatedAt = ToStoredUtc(now);
}
```

### 良い例

```csharp
public void Update(string name, string? description, DateTimeOffset now)
{
    var normalizedName = NormalizeName(name);
    var normalizedDescription = NormalizeDescription(description);
    var updatedAt = ToStoredUtc(now);

    Name = normalizedName;
    NormalizedName = normalizedName.ToUpperInvariant();
    Description = normalizedDescription;
    UpdatedAt = updatedAt;
}
```

### AIエージェントへの要求

新しいコードの実装・既存コードの修正・コードレビューでは、必ずこのルールを確認すること。

- 変更系メソッドで、検証が代入より前に行われているか確認すること。
- 代入の途中で例外になり得る呼び出しが混在していないか確認すること。
- 部分更新が起こり得る実装を見つけた場合は、計算と代入を分離して修正すること。
- 例外時に状態が変わっていないことを検証するテストを追加できる場合は、追加すること。
- このルールに反する実装を、理由なく許容しないこと。
