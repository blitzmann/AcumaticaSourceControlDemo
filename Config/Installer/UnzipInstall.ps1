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

Function Copy-File($sourceFile, $destinationFile)
{
    if(!(Test-Path $sourceFile))
    {
      Write-Host ('Unable to copy file: Missing source file: {0}' -f $sourceFile)
      return
    }

    $destinationPath = Split-Path $destinationFile
    if(!(Test-Path $destinationPath))
    {
      New-Item -ItemType Directory -Force -Path $destinationPath
      #Write-Host ('Unable to copy file: Missing destination directory: {0}' -f $destinationPath)
      #return
    }

    Copy-Item -Force $sourceFile $destinationFile
}

# -----------------------------------------------------------------------------
# Script: Get-FileMetaDataReturnObject.ps1
# Author: ed wilson, msft
# Date: 01/24/2014 12:30:18
# Keywords: Metadata, Storage, Files
# comments: Uses the Shell.APplication object to get file metadata
# Gets all the metadata and returns a custom PSObject
# it is a bit slow right now, because I need to check all 266 fields
# for each file, and then create a custom object and emit it.
# If used, use a variable to store the returned objects before attempting
# to do any sorting, filtering, and formatting of the output.
# To do a recursive lookup of all metadata on all files, use this type
# of syntax to call the function:
# Get-FileMetaData -folder (gci e:\music -Recurse -Directory).FullName
# note: this MUST point to a folder, and not to a file.
# -----------------------------------------------------------------------------
Function Get-FileMetaData
{
  <#
   .Synopsis
    This function gets file metadata and returns it as a custom PS Object 
   .Description
    This function gets file metadata using the Shell.Application object and
    returns a custom PSObject object that can be sorted, filtered or otherwise
    manipulated.
   .Example
    Get-FileMetaData -folder "e:\music"
    Gets file metadata for all files in the e:\music directory
   .Example
    Get-FileMetaData -folder (gci e:\music -Recurse -Directory).FullName
    This example uses the Get-ChildItem cmdlet to do a recursive lookup of 
    all directories in the e:\music folder and then it goes through and gets
    all of the file metada for all the files in the directories and in the 
    subdirectories.  
   .Example
    Get-FileMetaData -folder "c:\fso","E:\music\Big Boi"
    Gets file metadata from files in both the c:\fso directory and the
    e:\music\big boi directory.
   .Example
    $meta = Get-FileMetaData -folder "E:\music"
    This example gets file metadata from all files in the root of the
    e:\music directory and stores the returned custom objects in a $meta 
    variable for later processing and manipulation.
   .Parameter Folder
    The folder that is parsed for files 
   .Notes
    NAME:  Get-FileMetaData
    AUTHOR: ed wilson, msft
    LASTEDIT: 01/24/2014 14:08:24
    KEYWORDS: Storage, Files, Metadata
    HSG: HSG-2-5-14
   .Link
     Http://www.ScriptingGuys.com
 #Requires -Version 2.0
 #>
 Param([string[]]$folder)
 foreach($sFolder in $folder)
  {
   $a = 0
   $objShell = New-Object -ComObject Shell.Application
   $objFolder = $objShell.namespace($sFolder)

   foreach ($File in $objFolder.items())
    { 
     $FileMetaData = New-Object PSOBJECT
      for ($a ; $a  -le 266; $a++)
       { 
         if($objFolder.getDetailsOf($File, $a))
           {
             $hash += @{$($objFolder.getDetailsOf($objFolder.items, $a))  =
                   $($objFolder.getDetailsOf($File, $a)) }
            $FileMetaData | Add-Member $hash
            $hash.clear() 
           } #end if
       } #end for 
     $a=0
     $FileMetaData
    } #end foreach $file
  } #end foreach $sfolder
} #end Get-FileMetaData


# ----------------------------------------------------------------------------------
# Begin code below
# ----------------------------------------------------------------------------------
[string]$rootDir = $PSScriptRoot

$installer = 'AcumaticaERPInstall.msi'
$rootTargetDir = "D:\AcumaticaInstalls"
$oldInstallDir = Join-Path $rootDir "OldVersions"

$msiFile = Join-Path $rootDir 'AcumaticaERPInstall.msi'

$fileMetaData = Get-FileMetaData $rootDir

[string]$tags = ''
[string]$comment = ''
foreach($fileData in $fileMetaData)
{
  if($fileData.Name -eq $installer)
  {
      $tags = $fileData.Tags
      $comment = $fileData.Comments
      break
  }

  #if not showing file extensions...
  if($fileData.Name -eq $installer.split(".")[0])
  {
      $tags = $fileData.Tags
      $comment = $fileData.Comments
      break
  }
}

#Some versions store version in Tags and some versions in Comments

[string]$version = $tags.Replace('Version ','') #2017R2 or earlier
$version = $version.Replace('Installer','') #2018R1 or later - moved the version to the comment - but strip the comment tag of "Installer" to return empty

if([System.String]::IsNullOrWhiteSpace($version) -eq $true)
{
  $version = $comment.Replace('Version:','') #2018R1 or later 
}

if([System.String]::IsNullOrWhiteSpace($version) -eq $true)
{
  throw "Missing version information"
}

if($version.Split('.')[0].Length -eq 1)
{
  $version = ('0{0}' -f $version)
}

Write-Host ('Version {0} of {1} found' -f $version, $installer)

$writeHostColor = 'yellow'
Write-Host "==================================================================" -foregroundcolor $writeHostColor
Write-Host ('=====    RUNNING {0} {1}' -f $installer, $tags) -foregroundcolor $writeHostColor
Write-Host "==================================================================" -foregroundcolor $writeHostColor
Write-Host " "
Write-Host " "

#Save current file as an old version for reuse later (and version the file name)
$oldFileName = ("{0}.{1}.msi" -f ($installer.split(".")[0]), $version)
$oldFileDir = Join-Path $oldInstallDir $oldFileName
Copy-File $msiFile $oldFileDir

$targetDir = Join-Path $rootTargetDir $version

if(Test-Path $targetDir)
{
  Remove-Item -LiteralPath $targetDir -Recurse
}

if(Test-Path $targetDir)
{
  throw ('Path {0} already exists' -f {$targetDir})
}

$MSIArguments = @(
  "/a"
  ('"{0}"' -f $msiFile)
  "/qb"
  ('targetdir="{0}"' -f $targetDir)
)

Start-Process "msiexec.exe" -ArgumentList $MSIArguments -Wait -NoNewWindow 

Write-Host " " 
Write-Host " "
Write-Host "==================================================================" -foregroundcolor $writeHostColor
Write-Host "=====    INSTALL PROCESS COMPLETE" -foregroundcolor $writeHostColor
Write-Host "==================================================================" -foregroundcolor $writeHostColor
Read-Host 'Press Enter to continue...' | Out-Null 

