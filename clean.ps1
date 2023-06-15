param (
    [string]$Target = "ClashServiceWrapper",
    [string]$Runtime = "win-x64"
)
$target = "$PSScriptRoot\$Target\$Target.csproj"
if (Test-Path -Path $target)
{
    $optdir="$PSScriptRoot\publish"
    if (Test-Path -Path $optdir)
    {
        Remove-Item ($optdir+"\*")
    }
    dotnet clean -r $Runtime -c Release $target
}
else
{
    Write-Host "params specified is invalid."
}
