param(
    [double]$SimilarityThreshold = 0.85
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
$files += Get-ChildItem -Path . -Filter "*.md" -File -ErrorAction SilentlyContinue | Where-Object { $_.DirectoryName -eq (Get-Location).Path }

# Exclude .specify and specs folders completely
$files = $files | Where-Object { $_.FullName -notmatch "[\\/]\.specify[\\/]" -and $_.FullName -notmatch "[\\/]specs[\\/]" }

if (-not $files -or $files.Count -eq 0) {
    Write-Host "No markdown files found for scanning."
    exit 0
}

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

    # extract title (first heading) and first paragraph snippet
    $title = ($content -split "`n" | Where-Object { $_ -match '^#+' } | Select-Object -First 1)
    if (-not $title) { $title = "(no heading)" }
    $snippet = ($content -split "`n`n" | Select-Object -First 1) -replace "\r|\n", ' '

    # word set
    $words = ($content -split '\W+') | Where-Object { $_ } | ForEach-Object { $_.ToLower() } | Select-Object -Unique

    $items += [PSCustomObject]@{
        Path = $f.FullName
        RelativePath = (Resolve-Path $f.FullName).Path.Replace((Resolve-Path .).Path + '\\','')
        Title = $title.Trim()
        Snippet = if ($snippet.Length -gt 200) { $snippet.Substring(0,200) + '...' } else { $snippet }
        Sha1 = $sha1
        Words = $words
        WordCount = $words.Count
    }
}

# Pairwise comparisons
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
            $group += [PSCustomObject]@{ Path = $b.Path; Sha1 = $b.Sha1; Jaccard = $jaccard; IsExact = $isExact }
            $visited[$b.Path] = $true
        }
    }
    if ($group.Count -gt 1) {
        $groups += ,$group
        foreach ($g in $group) { $visited[$g.Path] = $true }
    }
}

# Prepare report
$reportLines = @()
$reportLines += "# Docs Cleanup Dry-Run Report"
$reportLines += "Generated: $(Get-Date -Format o)"
$reportLines += "Similarity threshold: $SimilarityThreshold"
$reportLines += ""
$reportLines += "## Scanned files"
foreach ($it in $items) {
    $reportLines += ("- {0} - SHA1: {1} - Words: {2} - Title: {3}" -f $it.RelativePath, $it.Sha1, $it.WordCount, $it.Title)
}
$reportLines += ""
$reportLines += "## Duplicate / Near-duplicate groups (candidates)"
if ($groups.Count -eq 0) {
    $reportLines += "No duplicates or near-duplicates found with the current threshold."
} else {
    $idx = 1
    foreach ($g in $groups) {
        $reportLines += "`n### Group $idx"
        # determine canonical candidate: choose the file with largest WordCount (heuristic)
        $canonical = $g | Sort-Object -Property @{Expression = { if ($_.PSObject.Properties['WordCount']) { $_.WordCount } else { 0 } }} -Descending | Select-Object -First 1
        $reportLines += ("Canonical candidate: {0}" -f $canonical.Path)
        foreach ($member in $g) {
            if ($member.PSObject.Properties.Name -contains 'WordCount') {
                $reportLines += ("- {0} - SHA1: {1} - Words: {2}" -f $member.Path, $member.Sha1, $member.WordCount)
            } else {
                # fallback for entries created during grouping
                $j = if ($member.PSObject.Properties.Name -contains 'Jaccard') { $member.Jaccard } else { '' }
                $e = if ($member.PSObject.Properties.Name -contains 'IsExact') { $member.IsExact } else { '' }
                $reportLines += ("- {0} - SHA1: {1} - Jaccard: {2} - Exact: {3}" -f $member.Path, $member.Sha1, $j, $e)
            }
        }
        $idx++
    }
}

$reportLines += ""
$reportLines += "## Recommendations (dry-run)"
$reportLines += "- Review each group above. For trivial exact duplicates consider archiving non-canonical copies. For near-duplicates review differences manually."
$reportLines += "- DO NOT modify files in `specs/` or `.specify/` (these were excluded)."
$reportLines += "- If you want a staged PR, run the full cleanup script (non-dry-run) which will move files to docs/_archive/<timestamp>/ and create a branch."

# Ensure docs folder exists
if (-not (Test-Path "docs")) { New-Item -ItemType Directory -Path "docs" | Out-Null }
$reportPath = Join-Path (Resolve-Path docs).Path "cleanup-report.md"
$reportLines | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "Dry-run complete. Report written to: $reportPath"
