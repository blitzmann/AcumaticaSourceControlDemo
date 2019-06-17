# **********************************************************************************
#
#   DevConDemo file
#
#   Updated 06/17/2019
#
# **********************************************************************************

# ----------------------------------------------------------------------------------
# Check for run as admin - if not run as admin
# ----------------------------------------------------------------------------------
$myWindowsID=[System.Security.Principal.WindowsIdentity]::GetCurrent()
$myWindowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($myWindowsID)
$adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
if ($myWindowsPrincipal.IsInRole($adminRole))
{
$Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + "(Elevated)"
clear-host
}
else {
$newProcess = new-object System.Diagnostics.ProcessStartInfo "PowerShell";
$newProcess.Arguments = $myInvocation.MyCommand.Definition;
$newProcess.Verb = 'runas';
[System.Diagnostics.Process]::Start($newProcess);
exit
}

# ... Begin functions

# Given a web.config file path, return the Acumatica version
Function Get-WebConfigVersion([string]$xmlFile)
{
    if(Test-Path $xmlFile)
    {
        $xml = [xml](Get-Content $xmlFile)
        return Get-WebAppVersion($xml)
    }
}

# Given a web.config xml content, return the Acumatica version
Function Get-WebAppVersion([xml]$xmlContent)
{
    $versionNbr = "0.0.0"
    $appSettingsNode = $xml.configuration.appSettings.Add
    foreach($addNode in $appSettingsNode)
    {
        if($addNode.key -eq "Version")
        {
            $versionNbr = $addNode.value   
            break;
        }
    }
    return $versionNbr
}

function Get-CommandToolPath()
{
    $commandTool = Join-Path $scriptPath 'Data'
    if(Test-Path $commandTool)
    {
        return $commandTool
    }

    $commandTool = Join-Path $scriptPath 'Acumatica ERP\Data'
    if(Test-Path $commandTool)
    {
        return $commandTool
    }

    $commandTool = Join-Path 'C:\Program Files\Acumatica ERP' 'Data'
    if(Test-Path $commandTool)
    {
        return $commandTool
    }

    $commandTool = Join-Path 'C:\Program Files (x86)\Acumatica ERP' 'Data'
    if(Test-Path $commandTool)
    {
        return $commandTool
    }

    return ''
}

Function Create-Directory($dir)
{
    if(!(Test-Path $dir))
    {
        Write-Host ('Creating Folder: {0}' -f $dir)
        New-Item $dir -type directory | Out-Null
    }
}

Function Delete-Item($dItem)
{
    if(Test-Path $dItem)
    {
        Write-Host ("Deleting: {0}" -f $dItem)
        Remove-Item $dItem -Force -Recurse
    }
}

Function Test-ValidPath($path)
{
    if([string]::IsNullOrWhiteSpace($path))
    {
        return $false
    }

    return Test-Path $path
}

Function Get-AcumaticaVersion($acumaticaDataDir)
{
    $acExe = Join-Path $acumaticaDataDir 'ac.exe'
    return Get-FileVersion $acExe
}

Function Get-FileVersion($file)
{
    if([System.IO.File]::Exists($file) -eq $false)
    {
        return "0.0.0.0"
    }

    $versionInfo = (ls $file -r | % versioninfo)
    return $versionInfo.FileVersion
}

Function MoveCreateSiteTempFolder($erpFolder, $installRootFolder, $tempFolderName)
{
    $tempErpPath = Join-Path $erpFolder $tempFolderName
    $sitePathParent = Join-Path $installRootFolder $tempFolderName

    if((Test-Path $sitePathParent) -and !(Test-Path $tempErpPath))
    {
        Write-Host ('Moving {0} Folder: {1}' -f $tempFolderName, $sitePathParent)

        $sitePathParentAcuERP = Join-Path $sitePathParent 'AcumaticaERP'
        if(Test-Path $sitePathParentAcuERP)
        {
            Move-Item $sitePathParentAcuERP $tempErpPath
        }
        else 
        {
            Move-Item $sitePathParent $tempErpPath
        }
    }

    if(!(Test-Path $tempErpPath))
    {
        Write-Host ('Creating {0} Folder: {1}' -f $tempFolderName, $tempErpPath)
        New-Item $tempErpPath -type directory | Out-Null
    }

    # cleanup the folders created by the installer
    Delete-Item $sitePathParent

    return $tempErpPath
}

Function Get-DatabaseName($sitePath)
{
    $webConfigPath = Join-Path $sitePath 'Web.Config'

    if([System.IO.File]::Exists($webConfigPath) -eq $false)
    {
        Write-Error ("Cannot find web.config file: {0}" -f $webConfigPath)
        return
    }

    $xml = [xml](Get-Content $webConfigPath)
     
    $connNodes = $xml.configuration.connectionStrings.add
    [string]$connString = ""
    #starting in 6.0 there are 2 connection strings in the web config...
    foreach($node in $connNodes)
    {
        if($node.name -eq "ProjectX")
        {
            $connString = $node.connectionString
        }
    }

    $connStringSplit = $connString.Split(";")
    foreach($connParam in $connStringSplit)
    {
        $kv = $connParam.Split("=")
        if($kv[0] -eq "Initial Catalog")
        {
            return $kv[1]
        }
    }
}

# Update the web config for developer settings and cleanup temp/backup folder locations
Function CleanupWebConfig($sitePath, $erpPath)
{
    $webConfigPath = Join-Path $sitePath 'Web.Config'

    if([System.IO.File]::Exists($webConfigPath) -eq $false)
    {
        Write-Error ("Cannot find web.config files; {0}" -f $webConfigPath)
        return
    }

    # use this to find and move the auto generated temp site folders
    $sitePathParent = (get-item $sitePath).parent.FullName

    $xml = [xml](Get-Content $webConfigPath)

    # ERP folder shared for all sub (sites) folders
    if(!(Test-Path $erpPath))
    {
        Write-Host ('Creating ERP Folder: {0}' -f $erpPath)
        New-Item $erpPath -type directory | Out-Null
    }

    [string]$snapshotsFolder = MoveCreateSiteTempFolder $erpPath $sitePathParent "Snapshots"
    [string]$customizationTempFolder = MoveCreateSiteTempFolder $erpPath $sitePathParent "Customization"
    [string]$backupFolder = MoveCreateSiteTempFolder $erpPath $sitePathParent "Backup"
    [string]$tempDirectory = MoveCreateSiteTempFolder $erpPath $sitePathParent "TemporaryAspFiles"

    # make sure debug set to true
    $node2 = $xml.configuration.'system.web'.compilation
    $node2.debug = "True"

    #this might be new to 19R1 or some recent version
    $node2.tempDirectory = $tempDirectory

    $nodeCustomizationPaths = $xml.selectNodes('//configuration//appSettings//add')
    foreach ($path in $nodeCustomizationPaths) {
      if($path.key -eq "CustomizationTempFilesPath"){
        $path.value = $customizationTempFolder
      }

      if($path.key -eq "SnapshotsFolder"){
        $path.value = $snapshotsFolder
      }

      if($path.key -eq "BackupFolder"){
        $path.value = $backupFolder
      }
    }

    $xml.Save($webConfigPath)
}

function Upgrade-AcumaticaSite([string]$siteVirtualDirectoryName, [string]$databaseName, [bool]$isPortal, [string]$acuSitePath, [string]$commandToolDir)
{
    Write-Host " "
    Write-Host "===========================================================================================================================" -foregroundcolor $script:writeHostColor
    Write-Host ("=====    BEGIN: Upgrading site {0} using database {1}" -f $siteVirtualDirectoryName, $databaseName ) -foregroundcolor $script:writeHostColor
    Write-Host "===========================================================================================================================" -foregroundcolor $script:writeHostColor
    Write-Host " "
    Write-Host " "

    $ac = 'ac.exe'
    $AcumaticaConfigDir = $commandToolDir
    if([string]::IsNullOrWhiteSpace($AcumaticaConfigDir))
    {
        $AcumaticaConfigDir = Get-CommandToolPath
    }
    $CMD = Join-Path $AcumaticaConfigDir $ac
    Write-Host $CMD

    $configMode = '-configmode:"UpgradeSite"'
    $dbNew = '-dbnew:"false"'
    $dbWinAuth = '-dbsrvwinauth:"True"'
    $dbServerName = '-dbsrvname:"(local)"'
    $dbName = ('-dbname:"{0}"' -f $databaseName)
    $dbConnWinAuth = '-dbwinauth:"True"'
    $localWebsite = '-swebsite:"Default Web Site"'
    $portalSite = ('-portal:{0}' -f $isPortal)
    $sitePath = ('-ipath:"{0}"' -f $acuSitePath)
    $virtualDirName = ('-svirtdir:"{0}"' -f $siteVirtualDirectoryName)
    $appPool = '-spool:"DefaultAppPool"'
    $salesDemo = ''
    $company = ('-company:"CompanyID=2;ParentID=1;Visible=True;CompanyType={0};LoginName=TestCompany;Delete=False"' -f $salesDemo)

    Write-Host ""
    Write-Host ("Running {0} using {1}" -f $CMD, $configMode) -ForegroundColor Yellow
    Write-Host ""
    Write-Host $CMD $configMode $dbNew $dbWinAuth $dbServerName $dbName $dbConnWinAuth $sitePath $company $appPool $localWebsite $portalSite $virtualDirName
    Write-Host ""

    & $CMD $configMode $dbNew $dbWinAuth $dbServerName $dbName $dbConnWinAuth $sitePath $company $appPool $localWebsite $portalSite $virtualDirName

    Write-Host " "
    Write-Host "===========================================================================================================================" -foregroundcolor $script:writeHostColor

    # first part above updates the site files only. Here we need to also update the database
    $configMode = '-configmode:"DBMaint"'
    $dbShrink = '-dbshrink:"Yes"'
    $dbUpdate = '-dbupdate:"Yes"'

    Write-Host ""
    Write-Host ("Running {0} using {1}" -f $CMD, $configMode) -ForegroundColor Yellow
    Write-Host ""
    Write-Host $CMD $configMode $dbNew $dbWinAuth $dbServerName $dbName $dbConnWinAuth $dbShrink $dbUpdate
    Write-Host ""

    & $CMD $configMode $dbNew $dbWinAuth $dbServerName $dbName $dbConnWinAuth $dbShrink $dbUpdate

    Write-Host " " 
    Write-Host " "
    Write-Host "===========================================================================================================================" -foregroundcolor $script:writeHostColor
    Write-Host "=====    FINISHED: Upgrading site" -foregroundcolor $script:writeHostColor
    Write-Host "===========================================================================================================================" -foregroundcolor $script:writeHostColor
    Write-Host " "
}

# ... end functions

# ----------------------------------------------------------------------------------
# Begin code below
# ----------------------------------------------------------------------------------

Try
{
    [string]$scriptPath = $PSScriptRoot
    $sitePath = Join-Path (get-item $scriptPath ).parent.parent.FullName 'Site'
    $erpPath = Join-Path (get-item $scriptPath ).parent.parent.FullName 'ERP'
    $webConfigPath = Join-Path $sitePath 'Web.Config'
    $commandToolPath = Get-CommandToolPath

    if([string]::IsNullOrWhiteSpace($commandToolPath))
    {
        $commandToolPath = "D:\AcumaticaInstalls\19.105.0032\Acumatica ERP\Data"
        #$commandToolPath = Read-Host -Prompt "Unable to find ERP Data folder. Enter ERP Data folder location. (Ex: 'C:\Program Files\Acumatica ERP')"
    }

    if((Test-ValidPath $commandToolPath) -eq $false)
    {
        throw "Unable to find Acumatica ERP Data directory"
    }

    [string]$version = Get-AcumaticaVersion $commandToolPath
    $writeHostColor = 'yellow'

    if([System.IO.File]::Exists($webConfigPath) -eq $false)
    {
        throw ("Missing file {0}" -f $webConfigPath)
    }

    $webVersion = Get-WebConfigVersion $webConfigPath

    Write-Host ""
    Write-Host ("Using Acumatica configuration version {0}. Upgrading from {1}" -f $version, $webVersion) -foregroundcolor Yellow
    Write-Host ""

    $instanceName = "DevConDemo"
    $dbName = Get-DatabaseName $sitePath
    [bool]$isPortal = $false

    # If the site already exists the prompt will ask this question to continue:
    #   Acumatica CW:
    #       Virtual directory with this name is already exists. Do you want to remove it?
    #       Yes(Y)/No(N)

    if([System.IO.File]::Exists((Join-Path $sitePath 'Bin\PX.Objects.dll')) -eq $true)
    {
        # move current site folder to a backup location to create a new site. The AC.exe does not like files existing in the path and prompts for continue but not sure how to answer the prompt via command line
        $siteBackupDir = Join-Path $erpPath 'LocalSitesBackup'
        Create-Directory $siteBackupDir
        $now = Get-Date -Format FileDateTime
        $siteTimeStamp = ("Site_{0}" -f $now)
        if(![string]::IsNullOrWhiteSpace($commandToolPath))
        {
            $siteTimeStamp = ("Site_{0}_{1}" -f $webVersion, $now)
        }
        $destinationSitePath = Join-Path $siteBackupDir $siteTimeStamp
        Write-Host ("COPYING {0} TO {1}" -f $sitePath, $destinationSitePath)
        Copy-Item $sitePath -Destination $destinationSitePath -Recurse  | Out-Null
    }

    Upgrade-AcumaticaSite $instanceName $dbName $isPortal $sitePath $commandToolPath

    CleanupWebConfig $sitePath $erpPath
}
Catch
{
    Write-Warning '*************************************'
    Write-Warning 'script failed'
    Write-Error $_.Exception.Message
    Write-Warning '*************************************'
}
Finally
{
    Write-Host " "
    Read-Host 'Press Enter to continue...' | Out-Null 
}