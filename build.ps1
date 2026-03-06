param (
    [switch] $debugBuild,
    [switch] $deploy,
    [string] $nugetServer = $env:LOCAL_NUGET_URL
)

$configuration = $debugBuild ? "Debug" : "Release"

$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot/src/BlazorNestedCss.Tasks/BlazorNestedCss.Tasks.csproj" -c $configuration
dotnet test "$PSScriptRoot/src/BlazorNestedCss.Tests/BlazorNestedCss.Tests.csproj" -c $configuration
dotnet pack "$PSScriptRoot/src/BlazorNestedCss.Tasks/BlazorNestedCss.Tasks.csproj" -c $configuration

if ($deploy) {
    if (-not $nugetServer) {
        Write-Error "No nuget server defined for deployment"
        exit
    }
    
    dotnet nuget push -s $nugetServer "$PSScriptRoot/packages/BlazorNestedCss.Tasks.1.0.0.nupkg"
}