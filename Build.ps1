﻿param (
    [switch]$SetAssemblyVersion,
    [switch]$Build,
    [switch]$UnitTests,
    [switch]$CoverReport,
    [switch]$GenerateDocs,
    [switch]$Pack,
    [switch]$Publish
)

# Load Toolkit
. ".build\BuildToolkit.ps1"

# Set build version to latest 
$MsBuildVersion = "latest"

# Initialize Toolkit
Invoke-Initialize -Version (Get-Content "VERSION");

if ($SetAssemblyVersion) {
    Set-AssemblyVersions;
}

if ($Build) {
    Invoke-Build ".\ClientFramework.sln"
}

if ($UnitTests) {
    Invoke-CoverTests -SearchFilter "*.Tests.csproj"
}

if ($CoverReport) {
    Invoke-CodecovUpload
}

if ($GenerateDocs) {
    Invoke-DocFx
}

if ($Pack) {
    Invoke-PackAll -Symbols
}

if ($Publish) {
    Invoke-Publish
}

Write-Host "Success!"