# DevConDemo example file

function Write-ReadHostText ($msg) {
    Write-Host ""
    Write-Host $msg -ForegroundColor Yellow
    $result = Read-Host
    Write-Host ""
    return $result
}


$cmdTool = Join-Path $PSScriptRoot 'PublishCustomizations.exe'
$installPath = Join-Path (get-item $PSScriptRoot).parent.FullName 'AcumaticaProjects\Install'


$instancePrompt = Write-ReadHostText "Enter local host website instance name (Ex: 'DevConDemo')"
$instance = ("http://localhost/{0}" -f $instancePrompt)

$user = Write-ReadHostText  "Enter username"

$pass = Write-ReadHostText  "Enter password"

$cmdArgs = @($installPath, 
        $instance, 
        $user,
        $pass)

& $cmdTool $cmdArgs

Write-Host " "
Read-Host 'Press Enter to continue...' | Out-Null 