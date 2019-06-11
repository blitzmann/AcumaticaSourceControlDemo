# ----------------------------------------------------------------------------------
# script parameters
# ----------------------------------------------------------------------------------
$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
$slnName = "DevConDemo.sln"
$slnFullName = Join-Path (get-item $PSScriptRoot).parent.FullName $slnName
$buildConfiguration = "/property:Configuration=Debug"


$writeHostStartColor = 'yellow'
$writeHostFinishColor = 'Red'
Write-Host "==================================================================" -foregroundcolor $writeHostStartColor
Write-Host ("=====    BEGIN: BUILDING SOLUTION {0}" -f $slnName ) -foregroundcolor $writeHostStartColor
Write-Host "==================================================================" -foregroundcolor $writeHostStartColor
Write-Host " "

try
{   
    $buildArgs = @($slnFullName, 
        $buildConfiguration, 
        "/target:Rebuild")
    & $msbuild $buildArgs

    $writeHostFinishColor = 'Green'
    Write-Host " " 
    Write-Host " "
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host ("=====    END: BUILDING SOLUTION {0}" -f $slnName ) -foregroundcolor $writeHostFinishColor
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
}
catch
{
    Write-Host "Error Occured While Processing..." -foregroundcolor $writeHostFinishColor
    Write-Host $Error[0] -foregroundcolor $writeHostFinishColor
    Write-Host " " 
    Write-Host " "
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host ("=====    FAILED: BUILDING SOLUTION {0}" -f $slnName ) -foregroundcolor $writeHostFinishColor
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
}

Write-Host " "
Read-Host 'Press Enter to continue...' | Out-Null 