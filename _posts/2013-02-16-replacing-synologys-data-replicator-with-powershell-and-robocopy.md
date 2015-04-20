---
title: Replacing Synology’s Data Replicator with Powershell and Robocopy
assets: /assets/2013-02-16-replacing-synologys-data-replicator-with-powershell-and-robocopy/
tags: [ "Miscellaneous", "Powershell" ]
---

I recently upgraded (for some definition of "upgraded") from Windows 7 to Windows 8. I had been using [Data Replicator 3](https://www.synology.com/en-us/knowledgebase/tutorials/454) to keep my machine backed up to my Synology server. However, I was keen to be rid of it because it is awfully slow and cumbersome. So I set out to do exactly that by leveraging Powershell and Robocopy, both of which are installed by default on Windows 8.

It's important to note that the solution I provide here isn't a like-for-like replacement of Data Replicator. It doesn't provide any help restoring data – you would have to either do that manually or write another script to reverse the robocopy from server to client. But on the plus side, there's nothing Synology-specific here either, so you can use this regardless of your server technology.

So here's the Powershell script I concocted (obviously you'll need to tweak the `$Server` and `$PathsToBackup` properties for your setup):

```PowerShell
# Incrementally backs up files on a client to a server
 
# the name of your server
$Server = "SERVER_NAME"
 
# the local paths to back up to your server. All paths must be fully-qualified
$PathsToBackup = @("C:\Users", "C:\SOME_OTHER_PATH")
 
$ScriptPath = $MyInvocation.MyCommand.Path
$ScriptDir = Split-Path $ScriptPath
$Client = [System.Net.Dns]::GetHostName()
$LogPath = "$ScriptDir\BackupLog.txt"
 
if (Test-Path -path $LogPath)
{
    Remove-Item $LogPath
}
 
New-Item $LogPath -type file
 
foreach ($PathToBackup in $PathsToBackup)
{
    $PathQualifier = Split-Path -Qualifier $PathToBackup
    $PathQualifier = $PathQualifier.Replace(":", "")
    $PathToBackupWithoutQualifier = Split-Path -NoQualifier $PathToBackup
    $Command = "robocopy $PathToBackup \\$Server\Backups\$Client\$PathQualifier\$PathToBackupWithoutQualifier /FFT /MIR /NP /W:0.5 /R:0 >> $LogPath"
    Invoke-Expression $Command
}
```

I placed this script in my home directory with a name of *Backup.ps1*.

Next, I set up a scheduled task to run this script every day. Open the **Task Scheduler** and select the **Task Scheduler Library**. Right-click in the list of tasks and select **Create New Task...** Use the following screenshots to guide you configuring the task. I've also listed the changes textually in case the images stop working (or are blocked for you):

### General Tab

![General Tab]({{ page.assets }}general.png "General Tab")

* Name: Backup (or whatever you want to call it)
* Security options: Run whether user is logged on or not and run with highest privileges
* Hidden: true

Configure for: I set mine to Windows 8, but not sure whether that's necessary

### Triggers Tab

![Triggers Tab]({{ page.assets }}triggers.png "Triggers Tab")

Set whatever trigger you like, but I set mine to run daily.

### Actions Tab

![Actions Tab]({{ page.assets }}actions.png "Actions Tab")

* Action: Start a program
* Program/script: powershell
* Arguments: enter the full path to the *Backup.ps1* script

Click **OK** to create the task. You will be prompted for a password, so enter that. Now you've got a scheduled task that will incrementally back up whatever local files you like to your server. Finally, note that the script produces a log (truncated on each run) called *BackupLog.txt* in the same directory as *Backup.ps1*, so this may come in handy if something isn't backing up as expected.

