# Test Script to Generate Traffic for Observability Demo
# This script will make requests to your GameStore API to generate both fast and slow requests

param(
    [Parameter(Mandatory=$true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory=$false)]
    [string]$AccessToken = "",

    [Parameter(Mandatory=$false)]
    [int]$RequestCount = 50
)

Write-Host "🎮 GameStore API Load Test Generator" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Define test search terms - mixed to create varied traffic
$searchTerms = @("Sonic", "Mario", "Skyrim", "Zelda", "Super", "Halo", "Street", "Pokemon",
                 "Starcraft", "Final", "Sims", "Call", "Spider", "Grand", "Red", "Mortal",
                 "Sunset", "Dragon", "Metal", "Portal", "Spore", "Crash", "Resident", "Silent")

# Setup headers
$headers = @{
    "Accept" = "application/json"
}

if ($AccessToken -ne "") {
    $headers["Authorization"] = "Bearer $AccessToken"
}

Write-Host "📊 Test Configuration:" -ForegroundColor Yellow
Write-Host "   API URL: $ApiBaseUrl" -ForegroundColor White
Write-Host "   Total Requests: $RequestCount" -ForegroundColor White
Write-Host ""

$results = @()

Write-Host "🚀 Starting test run..." -ForegroundColor Green
Write-Host ""

for ($i = 1; $i -le $RequestCount; $i++) {
    # Randomly select a search term (mixed fast and slow)
    $searchTerm = $searchTerms | Get-Random

    $url = "$ApiBaseUrl/games?name=$searchTerm&pageNumber=1&pageSize=10"

    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri $url -Method Get -Headers $headers -ErrorAction Stop
        $stopwatch.Stop()

        $duration = $stopwatch.ElapsedMilliseconds

        # Show all requests in the same color - don't reveal pattern
        Write-Host "✓ Request $i/$RequestCount | Query: '$searchTerm' | Time: ${duration}ms" -ForegroundColor Gray

        $results += [PSCustomObject]@{
            RequestNumber = $i
            SearchTerm = $searchTerm
            Duration = $duration
            Success = $true
        }

    } catch {
        Write-Host "✗ Request $i/$RequestCount | Query: '$searchTerm' | FAILED: $($_.Exception.Message)" -ForegroundColor Yellow

        $results += [PSCustomObject]@{
            RequestNumber = $i
            SearchTerm = $searchTerm
            Duration = 0
            Success = $false
        }
    }

    # Small delay between requests to avoid overwhelming the API
    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "📈 Test Results Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$successfulRequests = $results | Where-Object { $_.Success -eq $true }
$avgDuration = ($successfulRequests | Measure-Object -Property Duration -Average).Average
$maxDuration = ($successfulRequests | Measure-Object -Property Duration -Maximum).Maximum
$minDuration = ($successfulRequests | Measure-Object -Property Duration -Minimum).Minimum

Write-Host "Total Requests: $RequestCount" -ForegroundColor White
Write-Host "Successful: $($successfulRequests.Count)" -ForegroundColor Green
Write-Host "Failed: $($RequestCount - $successfulRequests.Count)" -ForegroundColor Red
Write-Host ""
Write-Host "Performance Metrics:" -ForegroundColor Yellow
Write-Host "   Average Duration: $([math]::Round($avgDuration, 0))ms" -ForegroundColor White
Write-Host "   Min Duration: ${minDuration}ms" -ForegroundColor White
Write-Host "   Max Duration: ${maxDuration}ms" -ForegroundColor White
Write-Host ""

Write-Host "✅ Test complete! Data sent to Application Insights." -ForegroundColor Green
Write-Host "   💡 Tip: Wait 2-3 minutes for telemetry to appear in Azure Portal" -ForegroundColor Cyan
Write-Host "   � Now investigate the performance using Application Insights!" -ForegroundColor Cyan
