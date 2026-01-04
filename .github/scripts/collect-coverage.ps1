#!/usr/bin/env pwsh

param(
    [string]$CoverageReportPath = "./coverage-reports",
    [string]$OutputPath = "./Documentation/statistics/coverage-data.js",
    [string]$Version = "",
    [string]$CommitSha = "",
    [string]$CommitMessage = ""
)

$ErrorActionPreference = "Stop"

Write-Host "Collecting coverage data from: $CoverageReportPath"

# Read existing data or create new structure
$data = @{
    lastUpdate = 0
    repoUrl = "https://github.com/Cratis/Chronicle"
    entries = @{}
}

if (Test-Path $OutputPath) {
    $existingContent = Get-Content $OutputPath -Raw
    $jsonStart = $existingContent.IndexOf("{")
    if ($jsonStart -ge 0) {
        $jsonContent = $existingContent.Substring($jsonStart)
        # Remove trailing semicolon and whitespace
        $jsonContent = $jsonContent.TrimEnd().TrimEnd(';')
        try {
            # Check PowerShell version for ConvertFrom-Json compatibility
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $data = ConvertFrom-Json $jsonContent -AsHashtable
            } else {
                $tempData = ConvertFrom-Json $jsonContent
                # Convert to hashtable manually for older PowerShell versions
                $data = @{
                    lastUpdate = $tempData.lastUpdate
                    repoUrl = $tempData.repoUrl
                    entries = @{}
                }
                foreach ($prop in $tempData.entries.PSObject.Properties) {
                    $data.entries[$prop.Name] = @($prop.Value)
                }
            }
        } catch {
            Write-Host "Warning: Could not parse existing data, starting fresh"
        }
    }
}

# Find the main Summary.json file (reportgenerator creates one at the root)
$summaryFile = Join-Path $CoverageReportPath "Summary.json"

if (-not (Test-Path $summaryFile)) {
    Write-Host "Warning: No Summary.json found in $CoverageReportPath"
    # Still update the timestamp
    $data.lastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $jsonOutput = ConvertTo-Json $data -Depth 10 -Compress
    $output = "window.COVERAGE_DATA = $jsonOutput;"
    Set-Content -Path $OutputPath -Value $output
    Write-Host "Updated timestamp in coverage data (no coverage report found)"
    exit 0
}

Write-Host "Processing coverage report: $summaryFile"

try {
    $coverage = Get-Content $summaryFile | ConvertFrom-Json
    
    $currentDate = Get-Date -Format "yyyy-MM-dd"
    # Calculate week number - use a simple approach based on date
    # This uses the year and month to create a consistent weekly bucket
    $year = (Get-Date).Year
    $month = (Get-Date).Month
    $day = (Get-Date).Day
    # Calculate week as: year + month*4 + week of month approximation
    # This ensures all dates in the same week get the same identifier
    $weekOfMonth = [Math]::Ceiling($day / 7)
    $currentWeek = "$year-M$($month.ToString('00'))-W$weekOfMonth"
    
    # Process each assembly in the coverage report
    foreach ($assembly in $coverage.coverage.assemblies) {
        $projectName = $assembly.name
        
        # Skip test assemblies and third-party libraries
        if ($projectName -match "\.Specs$" -or 
            $projectName -match "^xunit" -or 
            $projectName -match "^NSubstitute" -or
            $projectName -match "^testhost" -or
            $projectName -match "^coverlet") {
            Write-Host "  Skipping: $projectName"
            continue
        }
        
        # Get coverage percentage
        $lineCoverage = [math]::Round([double]$assembly.coverage, 2)
        
        Write-Host "  Project: $projectName, Coverage: $lineCoverage%"
        
        # Initialize project entry if needed
        if (-not $data.entries.ContainsKey($projectName)) {
            $data.entries[$projectName] = @()
        }
        
        # Check if we already have an entry for this week
        $existingEntry = $data.entries[$projectName] | Where-Object { $_.week -eq $currentWeek }
        
        if ($null -eq $existingEntry) {
            # Add new entry
            $entry = @{
                date = $currentDate
                week = $currentWeek
                lineCoverage = $lineCoverage
                version = $Version
                commit = if ($CommitSha.Length -gt 0) { $CommitSha.Substring(0, [Math]::Min(7, $CommitSha.Length)) } else { "" }
            }
            
            $data.entries[$projectName] += $entry
            Write-Host "    Added new entry for week $currentWeek"
        } else {
            Write-Host "    Entry already exists for week $currentWeek, updating..."
            # Update existing entry with latest values
            $existingEntry.lineCoverage = $lineCoverage
            $existingEntry.version = $Version
            $existingEntry.commit = if ($CommitSha.Length -gt 0) { $CommitSha.Substring(0, [Math]::Min(7, $CommitSha.Length)) } else { "" }
        }
    }
    
} catch {
    Write-Host "Error processing coverage report: $_"
    Write-Host $_.ScriptStackTrace
    exit 1
}

# Update timestamp
$data.lastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

# Keep only last 52 weeks of data per project (1 year)
foreach ($projectName in $data.entries.Keys) {
    $entries = $data.entries[$projectName] | Sort-Object -Property date -Descending
    if ($entries.Count -gt 52) {
        $data.entries[$projectName] = $entries | Select-Object -First 52
    }
}

# Convert to JSON and write to file
$jsonOutput = ConvertTo-Json $data -Depth 10 -Compress
$output = "window.COVERAGE_DATA = $jsonOutput;"
Set-Content -Path $OutputPath -Value $output

Write-Host "Coverage data updated successfully"
Write-Host "Output written to: $OutputPath"
Write-Host "Total projects tracked: $($data.entries.Keys.Count)"
