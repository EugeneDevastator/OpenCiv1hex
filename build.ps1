param(
	[switch]$Test,
	[string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'src\OpenCivOne.csproj'
$publishDirectory = Join-Path $root "publish\$Runtime"
$artifactDirectory = Join-Path $root 'artifacts'
$archive = Join-Path $artifactDirectory "OpenCivOne-hex-$Runtime.zip"

# A previous game process keeps the executable locked. Ask before killing it.
& (Join-Path $root 'tools\KillRunningInstances.ps1') -Prompt

if ($LASTEXITCODE -ne 0) {
	Write-Host 'Build cancelled.'
	exit $LASTEXITCODE
}

if ($Test) {
	dotnet test (Join-Path $root 'OpenCivOne.sln') --nologo -c Release -p:SkipStopRunningOpenCivOne=true

	if ($LASTEXITCODE -ne 0) {
		exit $LASTEXITCODE
	}
}

dotnet publish $project `
	--configuration Release `
	--runtime $Runtime `
	--self-contained true `
	-p:PublishSingleFile=true `
	-p:IncludeNativeLibrariesForSelfExtract=true `
	-p:DebugType=none `
	-p:SkipStopRunningOpenCivOne=true `
	--output $publishDirectory `
	--nologo

if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
# Only package the program files. The publish folder also contains the user's
# original Civilization resources, which must not go into the archive.
Compress-Archive -Path @(
	(Join-Path $publishDirectory 'OpenCivOne.exe'),
	(Join-Path $publishDirectory 'README.md'),
	(Join-Path $publishDirectory 'LICENSE')
) -DestinationPath $archive -Force

Write-Host ''
Write-Host "Published: $publishDirectory"
Write-Host "Archive:   $archive"
