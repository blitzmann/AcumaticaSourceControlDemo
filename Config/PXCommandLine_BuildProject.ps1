# ----------------------------------------------------------------------------------
# script parameters
# ----------------------------------------------------------------------------------
$acuProject = "DevConDemo"
$pxMethod = "BuildProject"
$website = Join-Path (get-item $PSScriptRoot).parent.FullName "Site"
$pxCommandFileName = "PX.CommandLine.exe"
$pxCommand = Join-Path (Join-Path $website "Bin") $pxCommandFileName
$acuProjectRoot = Join-Path (get-item $PSScriptRoot).parent.FullName ("AcumaticaProjects\{0}" -f $acuProject)
$acuProjectOutput = Join-Path (get-item $PSScriptRoot).parent.FullName ("AcumaticaProjects\Install\{0}.zip" -f $acuProject)
$taskName = ("Acumatica Command - {0} - project '{1}'" -f $pxMethod, $acuProject)

$writeHostStartColor = 'yellow'
$writeHostFinishColor = 'Red'
Write-Host "=============================================================================" -foregroundcolor $writeHostStartColor
Write-Host ("=====    BEGIN: {0}" -f $taskName ) -foregroundcolor $writeHostStartColor
Write-Host "=============================================================================" -foregroundcolor $writeHostStartColor
Write-Host " "

try
{   
    Invoke-Expression ("$pxCommand /method `"{0}`" /website `"{1}`" /in `"{2}`" /out `"{3}`" /level `"0`" /description `"{4}`"" -f $pxMethod, $website, $acuProjectRoot, $acuProjectOutput, "TestDesc")

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