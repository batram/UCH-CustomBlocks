# Every screenshot must be backed by an on-screen assertion.
#
# The suite used to "verify" screenshots with Check("...", saved is not null) —
# which asserts that a file was written, not what is in it. Combined with a
# page-turn helper that raced an animation, that produced five committed book
# screenshots of which two were the same page and none was the page the
# scenario existed to photograph. Both defects survived a fully green run.
#
# Two rules are enforced here:
#   1. Never assert that a screenshot FILE exists. It proves only that the
#      renderer produced bytes, and it passes just as happily when the camera
#      is pointed at the wrong page.
#   2. The Step() containing a capture must assert something about the state
#      being photographed first — Require/RequireOn for a view, or a
#      Golden/Check on the state that the picture illustrates.
#
# What a linter cannot check is GRANULARITY: "the block list is visible" is not
# the same claim as "the block list is settled on page 5 of 5". For anything
# reached by navigation, assert the identity of the view — see AGENTS.md.

$ErrorActionPreference = 'Stop'
$suites = Join-Path $PSScriptRoot 'suites'
$problems = @()

foreach ($file in Get-ChildItem -Path $suites -Recurse -Filter '*.fleet.csx') {
    $lines = Get-Content -LiteralPath $file.FullName
    $rel = $file.FullName.Substring((Split-Path $PSScriptRoot -Parent).Length + 1)

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch 'SaveScreenshot\s*\(') { continue }

        # Walk back to the start of this Step block.
        $start = 0
        for ($j = $i - 1; $j -ge 0; $j--) {
            if ($lines[$j] -match '^\s*Step\s*\(') { $start = $j; break }
        }

        $window = $lines[$start..$i] -join "`n"
        if ($window -notmatch '\b(Require|RequireOn|Golden|GoldenOn|Check)\s*\(') {
            $problems += "{0}:{1}: SaveScreenshot in a Step that asserts nothing - the picture is not evidence of anything" -f $rel, ($i + 1)
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'saved\s+is\s+not\s+null|Shot\s+is\s+not\s+null|shot\s+is\s+not\s+null') {
            $problems += "{0}:{1}: asserting a screenshot file exists is not evidence - assert the screen state instead" -f $rel, ($i + 1)
        }
    }
}

if ($problems.Count -gt 0) {
    Write-Host "screenshot guard: $($problems.Count) problem(s)" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "screenshot guard: OK - every SaveScreenshot is preceded by an on-screen assertion" -ForegroundColor Green
exit 0
