param (
    [string]$runtimeIdentifier = "",
    [ValidateSet("Debug", "Release")]
    [string]$buildType = "Release",
    [string]$outputDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-DefaultRuntimeIdentifier {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()

    if ($architecture -eq "arm64") {
        $architecture = "arm64"
    }
    elseif ($architecture -eq "x64") {
        $architecture = "x64"
    }
    else {
        throw "Unsupported architecture '$architecture'. Pass -runtimeIdentifier explicitly."
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
            [System.Runtime.InteropServices.OSPlatform]::Windows)) {
        return "win-$architecture"
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
            [System.Runtime.InteropServices.OSPlatform]::Linux)) {
        return "linux-$architecture"
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
            [System.Runtime.InteropServices.OSPlatform]::OSX)) {
        return "osx-$architecture"
    }

    throw "Unsupported operating system. Pass -runtimeIdentifier explicitly."
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($runtimeIdentifier)) {
    $runtimeIdentifier = Get-DefaultRuntimeIdentifier
}

$repoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath "..")).Path
$projectPath = Join-Path -Path $repoRoot -ChildPath "src/AtomUI.Cli/AtomUI.Cli.csproj"

if ([string]::IsNullOrWhiteSpace($outputDir)) {
    $outputDir = Join-Path -Path $repoRoot -ChildPath "output/native/$buildType/$runtimeIdentifier"
}

New-Item -Path $outputDir -ItemType Directory -Force | Out-Null

Invoke-DotNet -Arguments @(
    "publish",
    $projectPath,
    "--configuration",
    $buildType,
    "--runtime",
    $runtimeIdentifier,
    "--self-contained",
    "true",
    "-p:PublishAot=true",
    "-p:PublishDir=$outputDir/"
)

Write-Host "Native AOT output: $outputDir" -ForegroundColor Green
