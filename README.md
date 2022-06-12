# P4SimpleScc

**P4SimpleScc** is a Visual Studio Extension Source Control Provider for Perforce (Helix Core).

The "simple" part of this extension is that it only does **one** thing.  It checks files out from a Perforce server when they are modified (or when they are saved).  It doesn't do anything else.  It doesn't show status of checked out files.  It doesn't submit changelists of files.  It doesn't diff files.  It doesn't revert files.  It doesn't shelve files.  It doesn't show history or time-lapse view of files.  It doesn't show a revision graph of files.  All of those functions can be performed in P4V instead of implementing that same interface in Visual Studio.

If all you want is to check out files when modified, and you want something small, fast and efficient, then P4SimpleScc does that task well.

To use P4SimpleScc, just install the extension (there's one for Visual Studio 2019 and one for Visual Studio 2022), then start up Visual Studio and click on "Tools -> Options" from the main menu.  Expand the 'Source Control' group and select 'P4SimpleScc' from the 'Current source control plug-in:' drop down and click "OK".  That will enable the P4SimpleScc provider.

![SourceControl](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/Tools_Options_SourceControl.png)

While the P4SimpleScc provider is active, you can click on 'Extensions' in the main menu and you should see a 'P4SimpleScc' menu (this menu is hidden if P4SimpleScc is not the active source control provider).  If a solution is loaded, you can then click on the 'Solution Configuration' menu item to enable source control on the currently loaded solution.

![SolutionConfigurationMenu](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/SolutionConfigurationMenu.png)

The solution configuration dialog looks like this:

![SolutionConfigurationDialog](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/SolutionConfigurationDialog.png)

There are three choices at the top, 'Disabled, 'Automatic' and 'Manual'.  'Disable' will disable P4SimpleScc provider control for this solution.  'Automatic' will use the console command "p4 set" to automatically gather the settings for P4PORT, P4USER and P4CLIENT which can come from the Windows environment, from a previous 'p4 set' command, or from a .p4config file (as long as the P4CONFIG setting has previously been set using 'p4 set P4CONFIG=' followed by the .p4config file name).  You can also use 'Manual' to set the 'P4PORT', 'P4USER' and 'P4CLIENT' settings manually.  Each of these settings must be non-blank and will be validated with the Perforce (Helix Core) server to make sure they are correct.  P4PORT will be the server name and port number (separated by a ':').  P4USER will be your username on the Perforce server.  P4CLIENT will be your workspace name on the Perforce server for where you have the solution and files (although the solution .sln file doesn't have to be under Perforce control).

You can also select whether you want to check out files when they are modified or wait and check them out when the changes to the file have been saved.  Waiting until the file is saved means that you can "undo" changes to a file if you modified it by mistake and it won't be checked out from Perforce until you save it, so you won't have to revert that file later.  If you had selected 'Check out files on modify' it would have been checked out as soon as you accidentally modified it.

There is an option to prompt you with a dialog to verify that it is okay to check out each file before doing so (otherwise, the file will automatically be checked out upon modify/save).

There is also an option to enable or disable the 'P4SimpleScc' Output pane in the Output Window.  By default, this output is disabled.  If you enable output, you can also enable 'Verbose Output' which outputs the response of all P4 commands that are issued to the server by the P4SimpleScc extension.  The 'Verbose Output' is normally not needed unless you are trying to debug why some operation in P4SimpleScc is failing.

If you have 'Output Enabled' checked in the Solution Configuration, when the P4SimpleScc provider is enabled, you can see status messages from it by opening the Output window ("View -> Output" from the main menu, or Ctrl-Alt-O).  Then select 'P4SimpleScc' in the 'Show output from:' drop down.

![OutputWindow](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/OutputWindow.png)

You can manually check out file by selecting one or more files in the Solution Explorer view, right clicking and selecting "Check Out File".

![CheckOutFile](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/CheckOutFile.png)

You can also open a file in the document editor and right click on the filename tab and select "Check Out File" from there.

![CheckOutDocument](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/CheckOutDocument.png)

If you right click on a file and the file is already checked out, it will show "Check Out File" as being disabled.

![FileCheckedOut](https://raw.githubusercontent.com/botman99/P4SimpleScc/master/img/FileCheckedOut.png)

**NOTE:** If you have the P4VS extension installed, you may need to disable it (you don't need to uninstall, just disable) since the P4VS extension seems to want to make itself the active source control provider even if another provider was active or is controlling that solution.

See the [Releases](https://github.com/botman99/P4SimpleScc/releases) page to download the latest release.

* Author: Jeffrey "botman" Broome
* License: [MIT](http://opensource.org/licenses/mit-license.php)
