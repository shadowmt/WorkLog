<#
.SYNOPSIS
	コミット前に通すチェックハーネス（リンター → ビルド → テスト）。

.DESCRIPTION
	失敗したステップがあっても残りのステップを実行し、最後にまとめて結果を表示します。
	1 つでも失敗があれば終了コード 1 を返すため、git の pre-commit フックは
	その終了コードでコミットを中断できます。

	対象は CI（.github/workflows/worklog-ci.yml）と同じプロジェクトに揃えています。
	WorkLog.Client は MSIX / Windows SDK のビルドが必要なため既定では除外し、
	-FullSolution を指定したときだけソリューション全体をビルドします。

	統合テスト（WorkLog.Infrastructure.Tests）はテスト用 PostgreSQL が必要です。
	接続情報が無い、または DB に到達できない場合は SKIP として扱います
	（-Strict を指定すると SKIP も失敗扱いになります）。

.EXAMPLE
	.\scripts\check.ps1
	通常チェック。整形違反・ビルドエラー・テスト失敗があれば非 0 で終了。

.EXAMPLE
	.\scripts\check.ps1 -Fix
	dotnet format で整形と未使用の using 削除を自動適用してからビルド・テストを実行。

.EXAMPLE
	.\scripts\check.ps1 -SkipTests
	リンターとビルドだけ実行する。

.EXAMPLE
	.\scripts\check.ps1 -Strict -FullSolution
	CI と同等の厳しいチェック。統合テストが実行できなければ失敗させる。
#>
[CmdletBinding()]
param(
	[switch]$Fix,
	[switch]$SkipFormat,
	[switch]$SkipBuild,
	[switch]$SkipTests,
	[switch]$Strict,
	[switch]$FullSolution
)

$ErrorActionPreference = 'Stop'
$startedAt = Get-Date
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

# CI と同じ対象プロジェクト
$LintProjects = @(
	'WorkLog.Api/WorkLog.Api.csproj',
	'WorkLog.Domain/WorkLog.Domain.csproj',
	'WorkLog.Infrastructure/WorkLog.Infrastructure.csproj',
	'tests/WorkLog.Domain.Tests/WorkLog.Domain.Tests.csproj',
	'tests/WorkLog.Infrastructure.Tests/WorkLog.Infrastructure.Tests.csproj'
)

$UnitTestProjects = @(
	'tests/WorkLog.Domain.Tests/WorkLog.Domain.Tests.csproj'
)

# テスト用 PostgreSQL が必要なプロジェクト
$IntegrationTestProjects = @(
	'tests/WorkLog.Infrastructure.Tests/WorkLog.Infrastructure.Tests.csproj'
)

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
	param(
		[string]$Name,
		[ValidateSet('PASS', 'FAIL', 'SKIP')]
		[string]$Status,
		[string]$Message = ''
	)

	$results.Add([pscustomobject]@{
			Name   = $Name
			Status = $Status
			Message = $Message
		}) | Out-Null

	$color = switch ($Status) {
		'PASS' { 'Green' }
		'FAIL' { 'Red' }
		default { 'DarkYellow' }
	}

	Write-Host ("[{0}] {1} {2}" -f $Status, $Name, $Message) -ForegroundColor $color
}

function Invoke-Step {
	param(
		[string]$Name,
		[string[]]$Arguments
	)

	Write-Host ''
	Write-Host ("==> dotnet {0}" -f ($Arguments -join ' ')) -ForegroundColor Cyan

	& dotnet @Arguments
	$exitCode = $LASTEXITCODE

	if ($exitCode -eq 0) {
		Add-Result $Name 'PASS'
	}
	else {
		Add-Result $Name 'FAIL' ("exit code {0}" -f $exitCode)
	}

	return $exitCode
}

function Read-EnvFile {
	# .env を読み、キーと値のハッシュテーブルを返す。
	# カレントディレクトリではなく、リポジトリルートの .env を参照する。
	$path = Join-Path $RepoRoot '.env'
	$values = @{}

	if (-not (Test-Path -LiteralPath $path)) {
		return $values
	}

	foreach ($line in Get-Content -LiteralPath $path -Encoding UTF8) {
		if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
			$values[$matches[1]] = $matches[2].Trim().Trim('"', "'")
		}
	}

	return $values
}

function Test-IntegrationTestDatabase {
	# テスト用 PostgreSQL に接続できるかを調べ、接続文字列と環境変数を設定する。
	$envFile = Read-EnvFile

	$port = if ($envFile.ContainsKey('WORKLOG_TEST_PORT') -and $envFile['WORKLOG_TEST_PORT']) {
		$envFile['WORKLOG_TEST_PORT']
	}
	else { '5435' }

	$database = if ($envFile.ContainsKey('WORKLOG_TEST_DB') -and $envFile['WORKLOG_TEST_DB']) {
		$envFile['WORKLOG_TEST_DB']
	}
	else { 'worklog_test' }

	$username = if ($envFile.ContainsKey('WORKLOG_TEST_USER') -and $envFile['WORKLOG_TEST_USER']) {
		$envFile['WORKLOG_TEST_USER']
	}
	else { 'worklog_user' }

	$connectionString = $env:WORKLOG_TEST_CONNECTION_STRING

	if ([string]::IsNullOrWhiteSpace($connectionString)) {
		$password = $envFile['WORKLOG_TEST_PASSWORD']

		if ([string]::IsNullOrWhiteSpace($password)) {
			Write-Host ("参照した .env: {0}" -f (Join-Path $RepoRoot '.env')) -ForegroundColor DarkYellow
			Write-Host '.env に WORKLOG_TEST_PASSWORD がないため統合テストをスキップします。' -ForegroundColor DarkYellow
			return $false
		}

		# TestDatabaseFixture の安全ガードは Host=localhost を要求する。
		$connectionString =
			"Host=localhost;Port=$port;Database=$database;Username=$username;Password=$password"
	}

	# フィクスチャは期待値を環境変数から導出するため、.env の値をプロセスへも渡す。
	$env:WORKLOG_TEST_PORT = $port
	$env:WORKLOG_TEST_DB = $database
	$env:WORKLOG_TEST_USER = $username
	$env:WORKLOG_TEST_CONNECTION_STRING = $connectionString

	# 接続文字列が正しくても DB が起動していなければ失敗するため、先に疎通を見る。
	$portNumber = 0
	if (-not [int]::TryParse($port, [ref]$portNumber)) {
		Write-Host "WORKLOG_TEST_PORT が数値ではありません: $port" -ForegroundColor DarkYellow
		return $false
	}

	$tcp = New-Object System.Net.Sockets.TcpClient
	try {
		$async = $tcp.BeginConnect('127.0.0.1', $portNumber, $null, $null)
		$reachable = $async.AsyncWaitHandle.WaitOne(500) -and $tcp.Connected
	}
	finally {
		$tcp.Close()
	}

	if (-not $reachable) {
		Write-Host ("127.0.0.1:{0} に接続できないため統合テストをスキップします。" -f $portNumber) -ForegroundColor DarkYellow
		Write-Host '  起動: docker compose up -d --wait postgres-test' -ForegroundColor DarkYellow
		return $false
	}

	return $true
}

Write-Host '===== WorkLog チェックハーネス =====' -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# 1. リンター（dotnet format）
#    .editorconfig の IDE0005 により、未使用の using もここで検出されます。
# ---------------------------------------------------------------------------
if ($SkipFormat) {
	Add-Result 'lint (dotnet format)' 'SKIP' '-SkipFormat'
}
else {
	foreach ($project in $LintProjects) {
		$name = "lint: $project"

		if ($Fix) {
			Invoke-Step -Name "$name (fix)" -Arguments @('format', $project) | Out-Null
		}
		else {
			Invoke-Step -Name $name -Arguments @('format', $project, '--verify-no-changes') | Out-Null
		}
	}
}

# ---------------------------------------------------------------------------
# 2. ビルド
# ---------------------------------------------------------------------------
if ($SkipBuild) {
	Add-Result 'build' 'SKIP' '-SkipBuild'
}
elseif ($FullSolution) {
	# クライアントは RID がプラットフォームに紐付くため -p:Platform が必要です。
	Invoke-Step -Name 'build: WorkLog.slnx' `
		-Arguments @('build', 'WorkLog.slnx', '-c', 'Release', '-p:Platform=x64') | Out-Null
}
else {
	# テストプロジェクトはビルド済みアセットを再利用できるよう先にビルドしておきます。
	foreach ($project in @($UnitTestProjects + $IntegrationTestProjects)) {
		Invoke-Step -Name "build: $project" -Arguments @('build', $project, '-c', 'Release') | Out-Null
	}

	# CI と同じ理由: API 層のコンパイル破綻をテスト前に検出する
	Invoke-Step -Name 'build: WorkLog.Api' `
		-Arguments @('build', 'WorkLog.Api/WorkLog.Api.csproj', '-c', 'Release') | Out-Null
}

# ---------------------------------------------------------------------------
# 3. テスト
# ---------------------------------------------------------------------------
if ($SkipTests) {
	Add-Result 'tests' 'SKIP' '-SkipTests'
}
else {
	$integrationAvailable = $false

	if ($IntegrationTestProjects.Count -gt 0) {
		$integrationAvailable = Test-IntegrationTestDatabase
	}

	foreach ($project in $UnitTestProjects) {
		Invoke-Step -Name "test: $project" `
			-Arguments @('test', $project, '-c', 'Release', '--nologo') | Out-Null
	}

	foreach ($project in $IntegrationTestProjects) {
		if (-not $integrationAvailable) {
			$message = if ($Strict) {
				'テスト用 DB に接続できません（-Strict）'
			}
			else {
				'テスト用 DB が未起動'
			}

			Add-Result "test: $project" $(if ($Strict) { 'FAIL' } else { 'SKIP' }) $message
			continue
		}

		Invoke-Step -Name "test: $project" `
			-Arguments @('test', $project, '-c', 'Release', '--nologo') | Out-Null
	}
}

# ---------------------------------------------------------------------------
# 結果
# ---------------------------------------------------------------------------
$failed = @($results | Where-Object { $_.Status -eq 'FAIL' })
$skipped = @($results | Where-Object { $_.Status -eq 'SKIP' })
$elapsed = [int]((Get-Date) - $startedAt).TotalSeconds

Write-Host ''
Write-Host '===== 結果 =====' -ForegroundColor Cyan
$results | ForEach-Object {
	Write-Host ("  {0,-5} {1}" -f $_.Status, $_.Name)
}
Write-Host ("  合計 {0} 件 / 失敗 {1} 件 / スキップ {2} 件 / {3} 秒" -f `
		$results.Count, $failed.Count, $skipped.Count, $elapsed) -ForegroundColor Cyan

if ($failed.Count -gt 0) {
	Write-Host 'チェックが失敗したため、この結果はコミットに含められません。' -ForegroundColor Red
	exit 1
}

if ($skipped.Count -gt 0) {
	Write-Host 'スキップしたチェックがあります（-Strict で失敗扱いにできます）。' -ForegroundColor DarkYellow
}

Write-Host 'すべてのチェックを通過しました。' -ForegroundColor Green
exit 0
