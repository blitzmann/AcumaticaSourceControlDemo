# ----------------------------------------------------------------------------------
# BEGIN SCRIPT PARAMETERS
# ----------------------------------------------------------------------------------
$acuProject = "DevConDemo"
$pxMethod = "BuildProject"
$website = Join-Path (get-item $PSScriptRoot).parent.FullName "Site"
$pxCommandFileName = "PX.CommandLine.exe"
$pxCommand = Join-Path (Join-Path $website "Bin") $pxCommandFileName
$projectDllFileName = "PX.Objects.DevConDemo.dll"
$acuProjectRoot = Join-Path (get-item $PSScriptRoot).parent.FullName ("AcumaticaProjects\{0}" -f $acuProject)
$acuProjectOutput = Join-Path (get-item $PSScriptRoot).parent.FullName ("AcumaticaProjects\Install\{0}.zip" -f $acuProject)
$taskName = ("Acumatica Command - {0} - project '{1}'" -f $pxMethod, $acuProject)
$projectDesc = "DevConDemo - 0.0.0.0 - 2019.01.01"
# ----------------------------------------------------------------------------------
# END SCRIPT PARAMETERS
# ----------------------------------------------------------------------------------

# ----------------------------------------------------------------------------------
# BEGIN FUNCTIONS
# ----------------------------------------------------------------------------------
Function Get-ProjectVersionDate($dllFile, $dateFormat)
{
    $versionInfo = (ls $dllFile -r | % versioninfo)
    $fileDate = (Get-ChildItem $dllFile).LastWriteTime
    $FormatedFileDate = $fileDate.ToString($dateFormat)

    return ("{0} - {1}" -f $versionInfo.FileVersion, $FormatedFileDate )
}

# ----------------------------------------------------------------------------------
# END FUNCTIONS
# ----------------------------------------------------------------------------------


$writeHostStartColor = 'yellow'
$writeHostFinishColor = 'Red'
Write-Host "=============================================================================" -foregroundcolor $writeHostStartColor
Write-Host ("=====    BEGIN: {0}" -f $taskName ) -foregroundcolor $writeHostStartColor
Write-Host "=============================================================================" -foregroundcolor $writeHostStartColor
Write-Host " "

try
{   
    # Build a smart project description using project name, dll version, and date (time) of dll
    $projectDll = Join-Path (Join-Path $acuProjectRoot "Bin") $dllFileName
    $projectVersionDate = Get-ProjectVersionDate $projectDll "yyyy.MM.dd (HH:mm)"
    $projectDesc = ("{0} - {1}" -f $acuProject, $projectVersionDate )

    $exp = "& '$pxCommand' /method `"$pxMethod`" /website `"$website`" /in `"$acuProjectRoot`" /out `"$acuProjectOutput`" /level `"0`" /description `"$projectDesc`""
    Write-Host $exp
    Write-Host " "

    Invoke-Expression $exp

    $writeHostFinishColor = 'Green'
    Write-Host " " 
    Write-Host " "
    Write-Host "=============================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host ("=====    END: {0}" -f $taskName ) -foregroundcolor $writeHostFinishColor
    Write-Host "=============================================================================" -foregroundcolor $writeHostFinishColor
}
catch
{
    Write-Host "Error Occured While Processing..." -foregroundcolor $writeHostFinishColor
    Write-Host $Error[0] -foregroundcolor $writeHostFinishColor
    Write-Host " " 
    Write-Host " "
    Write-Host "=============================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host ("=====    FAILED: {0}" -f $taskName ) -foregroundcolor $writeHostFinishColor
    Write-Host "=============================================================================" -foregroundcolor $writeHostFinishColor
}

Write-Host " "
Read-Host 'Press Enter to continue...' | Out-Null 