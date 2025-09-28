<#
PowerShell script to detect duplicate/near-duplicate markdown files and optionally apply cleanup:
- Moves top-level docs into docs/
- Excludes .specify/ and specs/
- For duplicate groups: choose a canonical file (prefer file already under docs/ then largest word count), merge unique content from duplicates into canonical under a 'Merged from' heading, then archive the original file to docs/_archive/<timestamp>/
- Generates docs/cleanup-report.md with actions taken

Usage examples:
  # dry-run
  .\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85 -Apply:$false

  # apply changes
  .\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85 -Apply
#>
param(
    [double]$SimilarityThreshold = 0.85,
    [switch]$Apply
)

$repoRoot = Resolve-Path .
Write-Host "Repo root: $repoRoot"

# Collect markdown files: docs/**/*.md, README*.md, top-level *.md
$files = @()
if (Test-Path "docs") {
    $files += Get-ChildItem -Path "docs" -Recurse -Filter "*.md" -File -ErrorAction SilentlyContinue
}
$files += Get-ChildItem -Path . -Filter "README*.md" -File -ErrorAction SilentlyContinue
# top-level md files (in repo root)
$top = Get-ChildItem -Path . -Filter "*.md" -File -ErrorAction SilentlyContinue | Where-Object { $_.DirectoryName -eq (Get-Location).Path }
$files += $top

# Exclude .specify and specs folders completely
$files = $files | Where-Object { $_.FullName -notmatch "[\\/]\.specify[\\/]" -and $_.FullName -notmatch "[\\/]specs[\\/]" }

if (-not $files -or $files.Count -eq 0) {
    Write-Host "No markdown files found for scanning."
    exit 0
}

# Normalize list and ensure docs folder exists
if (-not (Test-Path "docs")) { New-Item -ItemType Directory -Path "docs" | Out-Null }

Add-Type -AssemblyName System.Security
$items = @()
foreach ($f in $files | Sort-Object FullName) {
    try {
        $content = Get-Content -Raw -LiteralPath $f.FullName -ErrorAction Stop
    } catch {
        Write-Warning "Failed to read $($f.FullName): $_"
        continue
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $sha1 = [System.BitConverter]::ToString([System.Security.Cryptography.SHA1]::Create().ComputeHash($bytes)).Replace("-","" ).ToLower()

    $words = ($content -split '\W+') | Where-Object { $_ } | ForEach-Object { $_.ToLower() } | Select-Object -Unique

    $items += [PSCustomObject]@{
        Path = $f.FullName
        RelativePath = (Resolve-Path $f.FullName).Path.Replace((Resolve-Path .).Path + '\\','')
        Content = $content
        Sha1 = $sha1
        Words = $words
        WordCount = $words.Count
        InDocs = (Split-Path $f.FullName -Parent) -like "*\docs*"
    }
}

# Move top-level files (not under docs/) into docs/ unless they are already in docs/
$movedTop = @()
foreach ($it in $items) {
    if (-not $it.InDocs) {
        $src = $it.Path
        $fileName = Split-Path $src -Leaf
        $dest = Join-Path (Resolve-Path docs).Path $fileName
        if ($Apply) {
            try {
                Move-Item -LiteralPath $src -Destination $dest -Force
                $movedTop += @{ From = $it.RelativePath; To = (Resolve-Path $dest).Path }
                Write-Host "Moved $($it.RelativePath) -> docs/$fileName"
            } catch {
                Write-Warning "Failed to move ${src}: $_"
            }
        } else {
            $movedTop += @{ From = $it.RelativePath; To = "docs/$fileName (preview)" }
        }
    }
}

if ($Apply -and $movedTop.Count -gt 0) {
    # refresh items list because some moved
    Start-Sleep -Milliseconds 200
    $files = Get-ChildItem -Path "docs" -Recurse -Filter "*.md" -File -ErrorAction SilentlyContinue
    $files += Get-ChildItem -Path . -Filter "README*.md" -File -ErrorAction SilentlyContinue | Where-Object { $_.DirectoryName -eq (Get-Location).Path }
    $files = $files | Where-Object { $_.FullName -notmatch "[\\/]\.specify[\\/]" -and $_.FullName -notmatch "[\\/]specs[\\/]" }
    $items = @()
    foreach ($f in $files | Sort-Object FullName) {
        try {
            $content = Get-Content -Raw -LiteralPath $f.FullName -ErrorAction Stop
        } catch { continue }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
        $sha1 = [System.BitConverter]::ToString([System.Security.Cryptography.SHA1]::Create().ComputeHash($bytes)).Replace("-","" ).ToLower()
        $words = ($content -split '\W+') | Where-Object { $_ } | ForEach-Object { $_.ToLower() } | Select-Object -Unique
        $items += [PSCustomObject]@{
            Path = $f.FullName
            RelativePath = (Resolve-Path $f.FullName).Path.Replace((Resolve-Path .).Path + '\\','')
            Content = $content
            Sha1 = $sha1
            Words = $words
            WordCount = $words.Count
            InDocs = $true
        }
    }
}

# Pairwise comparisons to find groups
$groups = @()
$visited = @{}
for ($i=0; $i -lt $items.Count; $i++) {
    $a = $items[$i]
    if ($visited.ContainsKey($a.Path)) { continue }
    $group = @($a)
    for ($j=$i+1; $j -lt $items.Count; $j++) {
        $b = $items[$j]
        if ($visited.ContainsKey($b.Path)) { continue }
        $isExact = ($a.Sha1 -eq $b.Sha1)
        $intersection = ($a.Words | Where-Object { $b.Words -contains $_ }).Count
        $union = ($a.Words + $b.Words | Select-Object -Unique).Count
        $jaccard = if ($union -eq 0) { 0 } else { [math]::Round($intersection / $union, 4) }
        if ($isExact -or $jaccard -ge $SimilarityThreshold) {
            $group += [PSCustomObject]@{ Path = $b.Path; Sha1 = $b.Sha1; Jaccard = $jaccard; IsExact = $isExact; WordCount = $b.WordCount; Content = $b.Content }
            $visited[$b.Path] = $true
        }
    }
    if ($group.Count -gt 1) {
        $groups += ,$group
        foreach ($g in $group) { $visited[$g.Path] = $true }
    }
}

# Prepare action lists
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$archiveRoot = Join-Path (Resolve-Path docs).Path "_archive\$timestamp"
$actions = @()
if ($Apply) { New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null }

foreach ($g in $groups) {
    # canonical selection: prefer a file already under docs/ with smallest path length, otherwise highest wordcount
    $candidates = $g | Where-Object { $_.Path -like "*\docs\*" }
    if ($candidates.Count -gt 0) {
        $canonical = $candidates | Sort-Object { $_.Path.Length } | Select-Object -First 1
    } else {
        $canonical = $g | Sort-Object -Property @{Expression = { if ($_.PSObject.Properties['WordCount']) { $_.WordCount } else { 0 } }} -Descending | Select-Object -First 1
    }
    $canonicalPath = $canonical.Path
    $merged = @()
    foreach ($member in $g) {
        if ($member.Path -eq $canonicalPath) { continue }
        # compute unique lines not present in canonical
        $canonicalLines = $canonical.Content -split "`n"
        $memberLines = $member.Content -split "`n"
        $unique = $memberLines | Where-Object { $canonicalLines -notcontains $_ } | Select-Object -Unique
        if ($unique.Count -gt 0) {
            $merged += @{ From = $member.Path; UniqueLines = $unique }
            if ($Apply) {
                # append unique lines under a heading
                $heading = "`n\n---\n\n**Merged from: $($member.RelativePath)**\n\n"
                Add-Content -LiteralPath $canonicalPath -Value $heading
                Add-Content -LiteralPath $canonicalPath -Value ($unique -join "`n")
            }
        }
        # archive the original file
        $archivePath = Join-Path $archiveRoot (Split-Path $member.Path -Leaf)
        if ($Apply) {
            try {
                Move-Item -LiteralPath $member.Path -Destination $archivePath -Force
            } catch {
                Write-Warning "Failed to archive $($member.Path): $_"
            }
        }
    }
    if ($merged.Count -gt 0) {
        $actions += @{ Canonical = $canonicalPath; Merged = $merged }
    } else {
        # no unique content; simply archive non-canonical
        foreach ($member in $g) {
            if ($member.Path -eq $canonicalPath) { continue }
            $archivePath = Join-Path $archiveRoot (Split-Path $member.Path -Leaf)
            if ($Apply) { Move-Item -LiteralPath $member.Path -Destination $archivePath -Force }
            $actions += @{ Canonical = $canonicalPath; Archived = $member.Path }
        }
    }
}

# Write report
$report = @()
$report += "# Docs Cleanup Report"
$report += "Generated: $(Get-Date -Format o)"
$report += "Apply mode: $Apply"
$report += "Similarity threshold: $SimilarityThreshold"
$report += ""
$report += "## Moved top-level files"
if ($movedTop.Count -eq 0) { $report += "- (none)" } else { foreach ($m in $movedTop) { $report += ("- {0} -> {1}" -f $m.From, $m.To) } }
$report += ""
$report += "## Duplicate groups processed"
if ($groups.Count -eq 0) { $report += "- (none)" } else {
    $idx = 1
    foreach ($a in $actions) {
        $report += ""
        $report += ("### Group {0}" -f $idx)
        $report += ("Canonical: {0}" -f $a.Canonical)
        if ($a.Merged) {
            foreach ($m in $a.Merged) {
                $report += ("- Merged from: {0} (unique lines: {1})" -f $m.From, $m.UniqueLines.Count)
            }
        }
        if ($a.Archived) { $report += ("- Archived: {0}" -f $a.Archived) }
        $idx++
    }
}
$report += ""
$report += "## Archive location"
$report += ("{0}" -f $archiveRoot)
$report += ""
$report += "## Recommendations"
$report += "- Review canonical files for formatting and merge edits."
$report += "- If any unwanted moves occurred, restore from archive."

$reportPath = Join-Path (Resolve-Path docs).Path "cleanup-report.md"
$report | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "Cleanup complete. Report: $reportPath"
if ($Apply) { Write-Host "Archive root: $archiveRoot" }

if ($Apply) {
    git add docs cleanup-report.md
    git add "docs/_archive" -A
    # commit if there are staged changes
    $status = git status --porcelain
    if ($status) {
        git commit -m "chore(docs): cleanup duplicates and collect docs into docs/"
    } else {
        Write-Host "No changes to commit."
    }
}
