# msbuild /p:Configuration=Release
nuget pack PoshCode.PoshConsole\PoshCode.PoshConsole.csproj -Symbols -IncludeReferencedProjects
# nuget push .\PoshCode.PoshConsole.0.6.2.0.nupkg