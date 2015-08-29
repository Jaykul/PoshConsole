pushd $PSScriptRoot
# msbuild /p:Configuration=Release
nuget pack PoshCode.PoshConsole\PoshCode.PoshConsole.csproj -Symbols -IncludeReferencedProjects

ls .\PoshCode.PoshConsole.*[0-9].nupkg | Sort LastWriteTime -Desc | Select -First 1

# nuget push .\PoshCode.PoshConsole.0.6.2.0.nupkg
# popd