param(
  [Parameter(Mandatory=$true, Position=0)][string]$Prompt,
  [Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining
)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& python "$ScriptDir/audited_codex.py" $Prompt @Remaining
exit $LASTEXITCODE
