# ----------------------------------------------------------------------------------
# script parameters
# ----------------------------------------------------------------------------------
$scriptBranchRoot = (get-item $PSScriptRoot ).parent.FullName
$scriptCommon = Join-Path (get-item $scriptBranchRoot ).parent.FullName 'Common'
$webConfigBase = "web.config.BASE"
$isDebug = $false
$updateTfs = $true

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
$newProcess.Verb = "runas";
[System.Diagnostics.Process]::Start($newProcess);
exit
}

# ----------------------------------------------------------------------------------
# Begin code below
# ----------------------------------------------------------------------------------

# function Add-TfsFile($file)
# {
#     if(Test-Path $file)
#     {
#         Write-Host ('* Add file {0} to TFS.' -f $file) -ForegroundColor Cyan
#         if($AddNewFilesToTfs -eq $true)
#         {
#             #$x = $ws.PendAdd($file)
#             tf add /noignore $file
#         }
#     }
# }

# function Delete-TfsFile($file)
# {
#     if(Test-Path $file)
#     {
#         Write-Host ('* Delete file {0} from TFS.' -f $file) -ForegroundColor DarkCyan
#         if($AddNewFilesToTfs -eq $true)
#         {
#             #$x = $ws.PendDelete($file)
#             tf delete $file
#         }
#     }
# }

function GetEnvironmentVariable($variableName)
{
    if ([System.Environment]::GetEnvironmentVariable($variableName) -ne $null)
    {
        ([System.Environment]::GetEnvironmentVariable($variableName).Trim())
    }
}

function Get-Parameters ()
{
    $ParmFile = (Join-Path $PSScriptRoot "CopySiteParameters.txt")
    
    if ((Test-Path $ParmFile) -eq $false)
    {
        $private:buffer = @()
        $buffer += "SourceSiteDir=C:\Program Files (x86)\Acumatica ERP\SomeAcumaticaSiteDirectory"
        $buffer += "SourcePortalDir="
        $buffer += "UpdateBin=False"
        
        Write-ImportantInfo ('Creating File: {0}' -f $ParmFile)
        $buffer | Out-File $ParmFile -Encoding Default
    }

    if ((Test-Path $ParmFile) -ne $false)
    {
        $fileContent = Get-Content $ParmFile
        Write-Host "*************************************************************"
        Write-Host "Input parameters:"
	    foreach ($line in $fileContent)
        {
            Write-Host $line
            $line = $line.split("=")
            if($line.Count -eq 2)
            {
                [System.Environment]::SetEnvironmentVariable($line[0],$line[1])     
            }
            else
            {
                Write-Host "Bad BuildParameters file content: {0}" -f $line
            }
        }
        Write-Host "*************************************************************"
    }   
    
    $script:DestinationSiteDir = Join-Path $scriptBranchRoot 'Site'
    $script:DestinationPortalDir = Join-Path $scriptBranchRoot 'Portal'
    $script:SourceSiteDir = GetEnvironmentVariable("SourceSiteDir")
    $script:SourcePortalDir = GetEnvironmentVariable("SourcePortalDir")
    $script:ProcessPortal = [System.String]::IsNullOrWhiteSpace($SourcePortalDir) -eq $false
    $script:UpdateDestinationSiteBin = GetEnvironmentVariable("UpdateBin")
    # $script:UpdateDevWebConfig = GetEnvironmentVariable("UpdateDeveloperWebConfig")
    # $script:AddNewFilesToTfs = GetEnvironmentVariable("AddNewFilesToTfs")
}

function Write-ImportantInfo($msg)
{
    Write-Host ("")
    Write-Host ("** {0}" -f $msg) -foregroundcolor Black -BackgroundColor Yellow
    Write-Host ("")
}

function Check-Paramters()
{
    $errorFound = $false
    if ((Test-Path $SourceSiteDir) -eq $false)
    {
        $errorFound = $true
        Write-ImportantInfo ("Source Site Directory is not valid: {0}" -f $SourceSiteDir)
    }

    if ((Test-Path $DestinationSiteDir) -eq $false)
    {
        $errorFound = $true
        Write-ImportantInfo ("Destination Site Directory is not valid: {0}" -f $DestinationSiteDir)
    }

    if($errorFound -eq $false)
    {
        Write-WebAppVersions $SourceSiteDir $DestinationSiteDir "Site"
    }

    if($ProcessPortal -eq $true)
    {
        if ((Test-Path $SourcePortalDir) -eq $false)
        {
            $errorFound = $true
            Write-ImportantInfo ("Source Portal Directory is not valid: {0}" -f $SourcePortalDir)
        }

        if ((Test-Path $DestinationPortalDir) -eq $false)
        {
            $errorFound = $true
            Write-ImportantInfo ("Destination Portal Directory is not valid: {0}" -f $DestinationPortalDir)
        }

        if($errorFound -eq $false)
        {
            Write-WebAppVersions $SourcePortalDir $DestinationPortalDir "Portal"
        }
    }
    else 
    {
        Write-Host "Skipping portal for upgrade"
    }

    if($errorFound -eq $true)
    {
        throw "Errors in parameters."
    }
}

Function UpdateWebConfigBase($webConfigDir)
{
    $xmlConfigBase = Join-Path $webConfigDir $webConfigBase

    if(Test-Path $xmlConfigBase)
    {
        Write-Host ('Update {0}' -f $xmlConfigBase)

        $xml = [xml](Get-Content $xmlConfigBase)
     
        # update connection string
        $connNodes = $xml.configuration.connectionStrings.add

        foreach($node in $connNodes)
        {
            if($node.name -eq "ProjectX")
            {
                $node.connectionString = 'data source=[YourServer];Initial Catalog=[YourDB];Integrated Security=False;User ID=[user];Password=[pass]'
            }
            # new starting in 6.0 beta version 6.00.0732 released 7/26/2016
            if($node.name -eq "ProjectX_MySql")
            {
                $node.connectionString = 'Server=localhost;Database=debug50_3;Uid=root;Pwd=Aw34esz;found rows=true;Unicode=true;'
            }
        }

        # make sure debug set to true
        $node2 = $xml.configuration.'system.web'.compilation
        $node2.debug = "True"
        $node2.optimizeCompilations = "True"

        # machinekey values need set when copied from the install files location. If values copied from another site web.config they are already set.
        #  The site will not work if left as the default 10 character zero value.
        #   site error if left unchanged:  Machine validation key is invalid. It is '10' chars long. It should be either "AutoGenerate" or between 40 and 128 Hex chars long, and may be followed by ",IsolateApps".
        $machineKeyNode =  $xml.SelectSingleNode('//configuration//location//system.web//machineKey')
        $validationKey = "CBE631863EB64022706978D560C25BAEB4E3C30EB845D2DB7982052141005F75E23DE4E25B73FFBCB7EC609079AF9B49E33B77BF10056A606095DF40B56FB8B5" #random key copied from a 6.0 site
        $decryptionKey = "7CE570355C7C9D1D88A3DD1F189010487D7CC05CDD9375EE" #random key copied from a 6.0 site (same site as validationkey)
        foreach($mkAtt in $machineKeyNode.Attributes)
        {
            # default value from files site copy is "0000000000"
            if($mkAtt.name -eq "validationKey" -and $mkAtt.value -eq "0000000000")
            {
                $mkAtt.value = $validationKey
            }
            if($mkAtt.name -eq "decryptionKey" -and $mkAtt.value -eq "0000000000")
            {
                $mkAtt.value = $decryptionKey
            }
        }

        $CustomizationTempFilesPathExists = $false
        $CustomizationTempFilesPathValue = "C:\Program Files (x86)\Acumatica ERP\Customization\"
        $SnapshotsFolderExists = $false
        $SnapshotsFolderValue = "C:\Program Files (x86)\Acumatica ERP\Snapshots\MySite"
        $BackupFolderExists = $false
        $BackupFolderValue = "C:\Program Files (x86)\Acumatica ERP\BackUp\Sites\"
        $AutomationDebugExists = $false
        $PageValidationExists = $false
        $InstantiateAllCachesExists = $false
        $CompilePagesExists = $false
        $DisableScheduleProcessorExists = $false

        $nodeCustomizationPaths = $xml.selectNodes('//configuration//appSettings//add')
        foreach ($path in $nodeCustomizationPaths) {
            if($path.key -eq "CustomizationTempFilesPath"){
                $path.value = $CustomizationTempFilesPathValue
                $CustomizationTempFilesPathExists = $true
            }

            if($path.key -eq "SnapshotsFolder"){
                $path.value = $SnapshotsFolderValue
                $SnapshotsFolderExists = $true
            }

            if($path.key -eq "BackupFolder"){
                $path.value = $BackupFolderValue
                $BackupFolderExists = $true
            }

            if($path.key -eq "AutomationDebug"){
                $path.value = "False"
                $AutomationDebugExists = $true
            }

            if($path.key -eq "PageValidation"){
                $path.value = "True"
                $PageValidationExists = $true
            }

            if($path.key -eq "InstantiateAllCaches"){
                $path.value = "False"
                $InstantiateAllCachesExists = $true
            }

            if($path.key -eq "CompilePages"){
                $path.value = "False"
                $CompilePagesExists = $true
            }

            if($path.key -eq "DisableScheduleProcessor"){
                $path.value = "True"
                $DisableScheduleProcessorExists = $true
            }
        }

        if($CustomizationTempFilesPathExists -eq $false){
            Add-AppSettingsKeyValue $xml "CustomizationTempFilesPath" $CustomizationTempFilesPathValue
        }

        if($SnapshotsFolderExists -eq $false){
            Add-AppSettingsKeyValue $xml "SnapshotsFolder" $SnapshotsFolderValue
        }

        if($BackupFolderExists -eq $false){
            Add-AppSettingsKeyValue $xml "BackupFolder" $BackupFolderValue
        }

        if($AutomationDebugExists -eq $false){
            Add-AppSettingsKeyValue $xml "AutomationDebug" "False"
        }

        if($PageValidationExists -eq $false){
            Add-AppSettingsKeyValue $xml "PageValidation" "True"
        }

        if($InstantiateAllCachesExists -eq $false){
            Add-AppSettingsKeyValue $xml "InstantiateAllCaches" "False"
        }

        if($CompilePagesExists -eq $false){
            Add-AppSettingsKeyValue $xml "CompilePages" "False"
        }

        if($DisableScheduleProcessorExists -eq $false){
            Add-AppSettingsKeyValue $xml "DisableScheduleProcessor" "True"
        }

        $xml.Save($xmlConfigBase)
    }
}

Function Add-AppSettingsKeyValue([xml]$webConfig, [string]$key, [string]$value)
{
    #process calling this should first check for key existing in XML

    $addLocation = $webConfig.SelectSingleNode('//configuration//appSettings')
    #Add to xml...
    $child = $xml.CreateElement("add")
    $keyAtt = $xml.CreateAttribute("key")
    $keyAtt.Value = $key
    $child.Attributes.Append($keyAtt)
    $valueAtt = $xml.CreateAttribute("value")
    $valueAtt.Value = $value
    $child.Attributes.Append($valueAtt)
    $addLocation.AppendChild($child)
}

Function Write-WebAppVersion([string]$xmlFile, [string]$writeNote = "")
{
    if(Test-Path $xmlFile)
    {
        $xml = [xml](Get-Content $xmlFile)
        $versionNumber = Get-WebAppVersion($xml)
        Write-Host ('{0} Version {1}' -f $writeNote, $versionNumber) -ForegroundColor Cyan
    }
}

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

Function Write-WebAppVersions($SourceDir, $DestinationDir, $description)
{
    $SourceCfg = Join-Path $SourceDir 'web.config'
    Write-WebAppVersion $SourceCfg ('New {0}' -f $description)

    $DestinationCfg = Join-Path $DestinationDir 'web.config'
    Write-WebAppVersion $DestinationCfg ('Current {0}' -f $description)
}

Function Get-FileContentArray($file, $excludedFileNames)
{
    [string[]] $returnArray = @();

    #variable to hold the objects of the list file for copying 
    $fileNameList = Get-Content $file 

    #copy file from source to destination 
    foreach($fileName in $fileNameList)
    { 
        if([System.String]::IsNullOrWhiteSpace($fileName) -eq $true)
        {
            continue;
        }

        if($excludedFileNames.Contains($fileName))
        {
            continue;
        }

        $returnArray += $fileName

        # used to include debug files...
        if($fileName.Contains('.dll'))
        {
            $returnArray += $fileName.Replace('.dll','.pdb')
            $returnArray += $fileName.Replace('.dll','.xml')
        }
        if($fileName.Contains('.exe'))
        {
            $returnArray += $fileName.Replace('.exe','.pdb')
        }
    }

    return $returnArray
}

Function CopyFilesListFiles($sourceDir, $destinationDir, $fileNameList)
{
    $sourceFileListPath = Join-Path $sourceDir 'files.list'
    $destinationFileListPath = Join-Path $destinationDir 'files.list'
    
    if(Test-Path $sourceFileListPath)
    {
        if(Test-Path $destinationFileListPath)
        {
            Remove-DeletedSiteFiles $sourceFileListPath $destinationFileListPath
        }

        Copy-Item -Force $sourceFileListPath $destinationFileListPath
    }

    Write-Host '----------------------------------------------------------------------------------'
    Write-Host ('Copy Source {0}' -f $sourceFileListPath )
    Write-Host ('Copy Destination {0}' -f $destinationFileListPath )
    Write-Host '----------------------------------------------------------------------------------'

    #copy file from source to destination 
    foreach($fileName in $fileNameList)
    { 
        $destinationFileName = $fileName

        #in some versions the text caps is different...
        if($destinationFileName.ToUpper().Equals('WEB.CONFIG'))
        {
            $webConfigBase = ('{0}.BASE' -f $destinationFileName)
            Write-Host ('**  Rename {0} with {1}  **' -f $destinationFileName, $webConfigBase )                        
            $destinationFileName = $webConfigBase
        }

        $sourceFilePath = Join-Path $sourceDir $fileName
        $destinationFilePath = Join-Path $destinationDir $destinationFileName

        Copy-File $sourceFilePath $destinationFilePath
    } 
}

#Detect the files deleted between the 2 sites and delete if "removed"
Function Remove-DeletedSiteFiles([string]$fileListFrom, [string]$fileListTo)
{
    Write-Host "Checking for deleted files..."

    #variable to hold the objects of the list file for copying 
    $fileNameFromList = Get-Content $fileListFrom 
    $fileNameToList = Get-Content $fileListTo 

    #copy file from source to destination 
    foreach($fileName in $fileNameToList)
    { 
        if( (FileNameInContent $fileName $fileNameFromList) -eq $false)
        {
            #delete me!
            Delete-TfsFile (Join-Path (Split-Path $fileListTo) $fileName)

            #if dll try to delete related pdb as it is not in the standard file list
            if($fileName.Contains('.dll'))
            {
                $pdbFile = $fileName.Replace('.dll','.pdb')
                Delete-TfsFile (Join-Path (Split-Path $fileListTo) $pdbFile)
            }
        }
    } 
}

Function FileNameInContent($compareFileName, $compareFileContents)
{
    foreach($fileName in $compareFileContents)
    { 
        if($fileName -eq $compareFileName)
        {
            return $true
        }
    } 
    return $false
}

#copy file first checking source file path as containg requested file
Function Copy-File($sourceFile, $destinationFile)
{
    if(Test-Path $sourceFile)
    {
        Create-FileDirectory $destinationFile
        
        $existingFile = (Test-Path $destinationFile)

        Copy-Item -Force $sourceFile $destinationFile

        if($isDebug)
        {
            Write-Host ('Copy {0} to {1}' -f $sourceFile, $destinationFile )
        }

        if($existingFile -eq $false)
        {
            Add-TfsFile $destinationFile
        }
    }
}

# make sure the file contains a directory. Use this before a file copy on the destination file
Function Create-FileDirectory($fullFilePath)
{
    $splitPath = split-path $fullFilePath
    if(!(Test-Path $splitPath))
    {
        Write-Host ('Creating Directory: {0}' -f $splitPath) -ForegroundColor Yellow
        New-Item $splitPath -type directory | Out-Null
    }
}


#Load common library
if($updateTfs -eq $true)
{
    $tfsCommon = (join-path $scriptCommon "TFSCommon.ps1")
    Write-Host ('Loading Common file: {0}' -f $tfsCommon)
    $c = . $tfsCommon
}


$writeHostStartColor = 'yellow'
$writeHostFinishColor = 'Red'
Write-Host "==================================================================" -foregroundcolor $writeHostStartColor
Write-Host "=====    UPGRADING ACUMATICA TFS SITE" -foregroundcolor $writeHostStartColor
Write-Host "==================================================================" -foregroundcolor $writeHostStartColor
Write-Host " "
Write-Host " "


#make sure version is greater than current version

try
{   
    if($isDebug)
    {
        Write-Host " RUNNING DEBUG MODE " -ForegroundColor Yellow        
    }

    Get-Parameters
    Check-Paramters

    if([System.String]::IsNullOrWhiteSpace($SourceSiteDir) -eq $false)
    {
        Write-Host " "
        Write-Host "~~~~~~~~~~~~~~~~~~~~~~~~~~"
        Write-Host "~~~ Begin SITE Upgrade ~~~"
        Write-Host "~~~~~~~~~~~~~~~~~~~~~~~~~~"

        [string[]] $excludedFiles = "App_Data\CustomizationStatus.xml","App_Data\Assistant\AcumaticaAssistant.exe","Icons\login_logo.png"

        $sourceFileListPath = Join-Path $SourceSiteDir 'files.list'
        $includefilesList = Get-FileContentArray $sourceFileListPath $excludedFiles

        CopyFilesListFiles $SourceSiteDir $DestinationSiteDir $includefilesList
        UpdateWebConfigBase $DestinationSiteDir
        Write-Host " "
    }
    
    if([System.String]::IsNullOrWhiteSpace($SourcePortalDir) -eq $false)
    {
        Write-Host " "
        Write-Host "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        Write-Host "~~~ Begin PORTAL Upgrade ~~~"
        Write-Host "~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        [string[]] $baseListExclude = "Web.Config" #these will then be included in the portal copy...
        $sourceMainSiteFileListPath = Join-Path $SourceSiteDir 'files.list'
        $mainSiteFileList = Get-FileContentArray $sourceMainSiteFileListPath $baseListExclude

        $sourcePortalSiteFileListPath = Join-Path $SourcePortalDir 'files.list'
        $mainSiteFileList += "files.list"
        #we only want the files related to Portal and not found in the main site.
        $includefilesList = Get-FileContentArray $sourcePortalSiteFileListPath $mainSiteFileList

        CopyFilesListFiles $SourcePortalDir $DestinationPortalDir $includefilesList
        UpdateWebConfigBase $DestinationPortalDir
        Write-Host " "
    }

    $writeHostFinishColor = 'Green'
    Write-Host " " 
    Write-Host " "
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host "=====    UPGRADE PROCESS COMPLETE" -foregroundcolor $writeHostFinishColor
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
}
catch
{
    Write-Host "Error Occured While Processing..." -foregroundcolor $writeHostFinishColor
    Write-Host $Error[0] -foregroundcolor $writeHostFinishColor
    Write-Host " " 
    Write-Host " "
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
    Write-Host "=====    UPGRADE PROCESS FAILED" -foregroundcolor $writeHostFinishColor
    Write-Host "==================================================================" -foregroundcolor $writeHostFinishColor
}

Write-Host " "
Read-Host 'Press Enter to continue...' | Out-Null 

