<#
Simple link checker for markdown files under docs/
- Checks local relative links for target file existence
- Attempts to verify heading anchors (basic slug from headings)
- Writes report to docs/link-check-report.md

Usage:
  .\scripts\check-doc-links.ps1
#>

$repoRoot = Resolve-Path .
$docsDir = Join-Path $repoRoot 'docs'
if (-not (Test-Path $docsDir)) { Write-Host "No docs/ folder found."; exit 0 }

$mdFiles = Get-ChildItem -Path $docsDir -Recurse -Filter "*.md" -File | Sort-Object FullName
$report = @()
$report += "# Docs Link Check Report"
$report += "Generated: $(Get-Date -Format o)"
$report += ""

function Slugify([string]$s) {
    # basic GitHub-style slug (lowercase, replace spaces with -, remove punctuation)
    $slug = $s.ToLower() -replace '[^a-z0-9\- ]','' -replace '\s+','-'
    return $slug.Trim('-')
}

foreach ($f in $mdFiles) {
    $content = Get-Content -Raw -LiteralPath $f.FullName -ErrorAction SilentlyContinue
    if (-not $content) { continue }
    $report += "## $($f.FullName)"

    # collect headings for anchor checks
    $headings = @()
    foreach ($line in $content -split "`n") {
        if ($line -match '^#{1,6}\s*(.+)') { $headings += Slugify($matches[1].Trim()) }
    }

    # find markdown links [text](link) and reference-style or bare relative links
    $linkPattern = '\[[^\]]+\]\(([^)]+)\)'
    $matches = [regex]::Matches($content, $linkPattern)
    $links = @()
    foreach ($m in $matches) { $links += $m.Groups[1].Value }

    # also check common bare links like ./path or docs/others
    foreach ($line in $content -split "`n") {
        if ($line -match '\b(\.?\/?[\w\-\./]+\.md)(#[-a-z0-9]+)?\b') {
            $links += $matches[1].Value
        }
    }

    if ($links.Count -eq 0) {
        $report += "- No local links found."
        $report += ""
        continue
    }

    foreach ($link in $links | Select-Object -Unique) {
        # skip external URLs
        if ($link -match '^[a-z]+://') { continue }
        $parts = $link -split '#'
        $pathPart = $parts[0]
        $anchorPart = if ($parts.Count -gt 1) { $parts[1] } else { $null }

        # resolve relative to the markdown file
        $target = Join-Path (Split-Path $f.FullName -Parent) $pathPart
        $target = (Resolve-Path -LiteralPath $target -ErrorAction SilentlyContinue)
        if (-not $target) {
            # try resolving from repo root
            $target = Join-Path $repoRoot $pathPart
            $target = (Resolve-Path -LiteralPath $target -ErrorAction SilentlyContinue)
        }

        if (-not $target) {
            $report += ("- Missing target file: {0} (link: {1})" -f $pathPart, $link)
            continue
        } else {
            $report += ("- Target exists: {0}" -f $target.Path)
        }

        if ($anchorPart) {
            $anchor = $anchorPart.TrimStart('#') -replace '%20',' ' -replace '%2d','-'
            $anchorSlug = Slugify($anchor)
            if ($headings -contains $anchorSlug) {
                $report += ("  - Anchor exists: #{0}" -f $anchorSlug)
            } else {
                $report += ("  - Missing anchor: #{0} (checked against {1} headings)" -f $anchorSlug, $headings.Count)
            }
        }
    }
    $report += ""
}

$reportPath = Join-Path $docsDir 'link-check-report.md'
$report | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "Link check complete. Report: $reportPath"