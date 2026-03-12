[CmdletBinding()]
param (
    [switch] $debugBuild,
    [string] $deploy
)

$configuration = $debugBuild ? "Debug" : "Release"

$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot/src/BlazorNestedCss.Tasks/BlazorNestedCss.Tasks.csproj" -c $configuration
dotnet test "$PSScriptRoot/src/BlazorNestedCss.Tests/BlazorNestedCss.Tests.csproj" -c $configuration
dotnet pack "$PSScriptRoot/src/BlazorNestedCss.Tasks/BlazorNestedCss.Tasks.csproj" -c $configuration

if ($deploy) {
    [xml]$proj = Get-Content "$PSScriptRoot/src/BlazorNestedCss.Tasks/BlazorNestedCss.Tasks.csproj"
    $version = $proj.SelectSingleNode("//Version").InnerText
    Write-Host "Publshing packages/BlazorNestedCss.Tasks.$version.nupkg..."
    dotnet nuget push -s $deploy "$PSScriptRoot/packages/BlazorNestedCss.Tasks.$version.nupkg" --allow-insecure-connections
}