[CmdletBinding()]param()


pushd $PSScriptRoot
msbuild /p:Configuration=Release
nuget pack PoshCode.PoshConsole\PoshCode.PoshConsole.csproj -Symbols -IncludeReferencedProjects
$package = ls .\PoshCode.PoshConsole.*[0-9].nupkg | Sort LastWriteTime -Desc | Select -First 1

$RelativePath = (Resolve-Path $package -Relative)
if($PSCmdlet.ShouldContinue("Are you sure you want to publish $RelativePath", "Publish $($package.Name)")) {
    $package = (Resolve-Path $package)
    nuget push $package
}
popd