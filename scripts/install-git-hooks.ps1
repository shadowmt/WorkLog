<#
.SYNOPSIS
	.githooks を Git のフックディレクトリとして登録（または解除）します。

.DESCRIPTION
	Git のフックは .git 内にあるためリポジトリに含められません。
	core.hooksPath を .githooks に指し示すことで、フックをリポジトリで
	共有します。クローン後はこのスクリプトを 1 回実行してください。

.EXAMPLE
	.\scripts\install-git-hooks.ps1
	pre-commit フックを有効化。

.EXAMPLE
	.\scripts\install-git-hooks.ps1 -Uninstall
	設定を解除（Git の既定の .git/hooks が使われるようになります）。
#>
[CmdletBinding()]
param(
	[switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

if ($Uninstall) {
	git config --unset core.hooksPath
	Write-Host 'core.hooksPath を解除しました。以降は .git/hooks が使われます。' -ForegroundColor Green
	exit 0
}

# フックは sh で実行されるため LF 必須。checkout 時に LF へ正規化されます。
git add --renormalize .githooks 2>$null

git config core.hooksPath .githooks

$configured = git config --get core.hooksPath
Write-Host "core.hooksPath = $configured" -ForegroundColor Green

foreach ($hook in Get-ChildItem .githooks -File) {
	Write-Host ("  登録済み: {0}" -f $hook.Name)
}

Write-Host ''
Write-Host 'pre-commit は scripts/check.ps1 を実行します。' -ForegroundColor Cyan
Write-Host 'スキップする場合: git commit --no-verify（非推奨）' -ForegroundColor DarkYellow
