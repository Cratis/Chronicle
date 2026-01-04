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
            $data = ConvertFrom-Json $jsonContent -AsHashtable
        } catch {
            Write-Host "Warning: Could not parse existing data, starting fresh"
        }
    }
}

# Find all coverage files
$coverageFiles = Get-ChildItem -Path $CoverageReportPath -Filter "Summary.json" -Recurse -ErrorAction SilentlyContinue

if ($coverageFiles.Count -eq 0) {
    Write-Host "Warning: No coverage files found in $CoverageReportPath"
    # Still update the timestamp
    $data.lastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $jsonOutput = ConvertTo-Json $data -Depth 10 -Compress
    $output = "window.COVERAGE_DATA = $jsonOutput;"
    Set-Content -Path $OutputPath -Value $output
    Write-Host "Updated timestamp in coverage data (no coverage reports found)"
    exit 0
}

Write-Host "Found $($coverageFiles.Count) coverage report(s)"

$currentDate = Get-Date -Format "yyyy-MM-dd"
$currentWeek = Get-Date -UFormat "%Y-W%V"

foreach ($file in $coverageFiles) {
    Write-Host "Processing: $($file.FullName)"
    
    try {
        $coverage = Get-Content $file.FullName | ConvertFrom-Json
        
        # Extract project name from path
        $projectPath = $file.Directory.Parent.Name
        $projectName = $projectPath
        
        # Get coverage percentage
        $lineCoverage = 0
        if ($coverage.summary -and $coverage.summary.linecoverage) {
            $lineCoverage = [math]::Round([double]$coverage.summary.linecoverage, 2)
        }
        
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
                commit = $CommitSha.Substring(0, [Math]::Min(7, $CommitSha.Length))
            }
            
            $data.entries[$projectName] += $entry
            Write-Host "  Added new entry for week $currentWeek"
        } else {
            Write-Host "  Entry already exists for week $currentWeek, skipping"
        }
        
    } catch {
        Write-Host "Error processing $($file.FullName): $_"
    }
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
