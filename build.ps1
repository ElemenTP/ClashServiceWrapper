if($args.Count -eq 0)
{
    $target=$PSScriptRoot+"\ClashServiceWrapper.sln"
}
elseif ($args.Count -eq 1)
{
    $name = $args[0]
    $target=$PSScriptRoot+"\$name\$name.csproj"
}
if (Test-Path -Path $target)
{
    $optdir=$PSScriptRoot+"\publish"
    dotnet publish -r win-x64 -c Release --self-contained $target -o $optdir
}
else
{
    Write-Host "params specified is invalid."
}
