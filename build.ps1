if($args.Count -eq 0)
{
    $target=$PSScriptRoot+"\ClashServiceWrapper.sln"
}
elseif ($args.Count -eq 1)
{
    if($args[0] -eq "all")
    {
        $target=$PSScriptRoot+"\ClashServiceWrapper.sln"
    }
    elseif($args[0] -eq "host")
    {
        $target=$PSScriptRoot+"\Host\Host.csproj"
    }
    elseif($args[0] -eq "client")
    {
        $target=$PSScriptRoot+"\Client\Client.csproj"
    }
}
if (Test-Path variable:target)
{
    $optdir=$PSScriptRoot+"\publish"
    dotnet publish -r win-x64 -c Release --self-contained $target -o $optdir
}
else
{
    Write-Host "params specified is invalid."
}