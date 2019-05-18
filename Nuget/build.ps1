[CmdletBinding()]
param(
    [Parameter()]
    # Delete local fake-folder and lock file, use it when you change the dependencies / want to update dependencies or in order to be sure everything is done rom scratch
    [switch]$Force = $false,

    [Parameter()]
    [string]$TargetName = "Done",

    [Parameter()]
    [string]$BuildScript = "build.fsx",

    [Parameter()]
    # If set to true it can help to diagnostic fake script, but as Fake use stderr, some build system will detect it as a failure were it is in fact just verbosity.
    [switch]$FakeVerbose = $false,

    [Parameter()]
    # Skip UnitTest target, use this only when debugging the script for target after UnitTests
    [switch]$SkipUnitTests = $false,

    [Parameter()]
    # Skip IntegrationTests target, use this only when debugging the script  for target after IntegrationTests
    [switch]$SkipIntegrationTests = $false
)

$FakeFileName = "fake"
$FakeNugetName = "fake-cli"


function Get-DotNetToolPath([string]$ToolName) {
    return ".$($ToolName.ToLower())"
}

function Install-DotNetTool([string]$ToolName, [string]$ToolNugetName = $null) {
    # The tool name might be different from the pacakge name :
    # for paket : ToolName = paket / ToolNugetName = paket
    # for fake  : ToolName = fake  / ToolNugetName = fake-cli

    if ([string]::IsNullOrEmpty($ToolNugetName)) {
        # if no tool nuget name provided we assume that the nuget package has the same name as the tool (eg : paket)
        $ToolNugetName = $ToolName
    }

    # We assume that the tool-path will be the same as the tool but prefix by a '.'
    # It will allow us the clean artifacts per tool based on that assumption
    $ToolPath = Get-DotNetToolPath -ToolName $ToolName


    Write-Host "Installing '$($ToolNugetName)' dotnet tool"
    $installDotNetToolArgs = "dotnet tool install $($ToolNugetName) --tool-path $($ToolPath)"

    Write-Host "Execution expression : '$($installDotNetToolArgs)'"
    Invoke-Expression $installDotNetToolArgs
}

function Remove-DotNetToolArtifacts([string]$ToolName) {
    $ToolFolder = Get-DotNetToolPath -ToolName $ToolName

    if (Test-Path $ToolFolder -PathType Container) {
        Write-Host "Deleting '$($ToolFolder)' folder"
        Remove-Item -Force -Recurse $ToolFolder
    }
}

function Get-FakeCliRelativePath {
    $FakeToolFolder = Get-DotNetToolPath -ToolName $FakeFileName
    return "$($FakeToolFolder)/$($FakeFileName).exe"
}

function Remove-FakeArtifacts([string]$BuildScript) {
    Remove-DotNetToolArtifacts -ToolName $FakeFileName
    
    $scriptLockFileName = "$($BuildScript).lock"
    if (Test-Path $scriptLockFileName -PathType Leaf) {
        Write-Host "Deleting fake lockfile : '$($scriptLockFileName)'"
        Remove-Item -Force $scriptLockFileName
    }
}

function Install-FakeDotNetTool {
    $fakeToolPath = Get-DotNetToolPath -ToolName $FakeFileName
    $fakeToolPathExists = Test-Path $fakeToolPath -PathType Container
    $fake = Get-FakeCliRelativePath
    $fakeFileExists = Test-Path $fake -PathType Leaf

    if (-Not $fakeToolPathExists -or -Not $fakeFileExists) {
        Install-DotNetTool -ToolName $FakeFileName -ToolNugetName $FakeNugetName
    }
}

function Invoke-FakeBuild([string]$BuildScript, [string]$TargetName) {
    $fake = Get-FakeCliRelativePath
    $fakeBuildArgs = "./$($fake)"
    $runArgs = " run --nocache $($BuildScript) --parallel 8 --target $($TargetName)"

    if ($FakeVerbose) {
        $fakeBuildArgs += " -v"
    }

    $fakeBuildArgs += $runArgs

    if ($SkipUnitTests -or $SkipIntegrationTests) {
        $fakeBuildArgs += " --"

        if ($SkipUnitTests) {
            $fakeBuildArgs += " --skip-unit-tests"
        }

        if ($SkipIntegrationTests) {
            $fakeBuildArgs += " --skip-integration-tests"
        }
    }

    Write-Host "Execution expression : '$($fakeBuildArgs)'"
    Invoke-Expression $fakeBuildArgs
}

if ($Force) {
    Remove-FakeArtifacts -BuildScript $BuildScript
}

Install-FakeDotNetTool

Invoke-FakeBuild -TargetName $TargetName -BuildScript $BuildScript

exit $LASTEXITCODE;
