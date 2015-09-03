[CmdletBinding()]param(
    $ProjectName = "PoshCode.PoshConsole"
)


pushd $PSScriptRoot
msbuild /p:Configuration=Release
nuget pack "${ProjectName}\${ProjectName}.csproj" -Symbols -IncludeReferencedProjects 
nuget pack "${ProjectName}\${ProjectName}.csproj" -IncludeReferencedProjects -Exclude "**\*.pdb;**\*.cs"
$package = ls "$PSScriptRoot\${ProjectName}.*[0-9].nupkg" | Sort LastWriteTime -Desc | Select -First 1

$RelativePath = (Resolve-Path $package -Relative)
if(Test-Path $RelativePath) {
    if($PSCmdlet.ShouldContinue("Are you sure you want to publish $RelativePath", "Publish $($package.Name)")) {
        $package = (Resolve-Path $package)
        nuget push $package
        mkdir "${PSScriptRoot}\Releases" -Force
        mv "$PSScriptRoot\${ProjectName}*.nupkg" "${PSScriptRoot}\Releases"
    }
}
popd