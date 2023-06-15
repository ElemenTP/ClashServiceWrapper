param (
    [string]$Target = "ClashServiceWrapper",
    [string]$Runtime = "win-x64"
)
$target = "$PSScriptRoot\$Target\$Target.csproj"
if (Test-Path -Path $target)
{
    $optdir=$PSScriptRoot+"\publish"
    dotnet publish -r $Runtime -c Release --self-contained $target -o $optdir
}
else
{
    Write-Host "params specified is invalid."
}
