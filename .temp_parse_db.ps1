if (-not $env:DATABASE_URL) { Write-Output 'DATABASE_URL not set'; exit 0 }
$u = [System.Uri]::new($env:DATABASE_URL)
$ui = $u.UserInfo.Split(':')
Write-Output ("host: " + $u.Host)
Write-Output ("port: " + $u.Port)
Write-Output ("user: " + $ui[0])
