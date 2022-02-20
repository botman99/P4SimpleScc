
This Visual Studio extension was created using shared projects as described here:

https://docs.microsoft.com/en-us/visualstudio/extensibility/migration/update-visual-studio-extension?view=vs-2022#use-shared-projects-for-multi-targeting

To build (and debug) the VS2019 extension, open the .sln file using Visual Studio 2019.
To build (and debug) the VS2022 extension, open the .sln file using Visual Studio 2022.


The ClassLibrary was originally created as a Windows Forms App (.NET Framework) Project and then
the 'Output type:' in the Properties was changed to "Class Library".  This was necessary because
both of the VISX projects need access to the Windows Forms Dialog(s) and Visual Studio won't let
you add Windows Forms Dialogs (using .NET Framework) to the SharedProject.

After being switched from "Windows Forms App" to "Class Library", things like 'Program.cs' and
'App.config' were removed from the new project (since these aren't needed for a class library).

Any additional classes that are needed by both the VISX project's classes and by the Windows
Forms classes (such as P4Command.cs) need to also be added to the ClassLibrary so that these are
accessible by both projects.


The origin of the Source Control Provider code came from the VSSDK sample here:
https://github.com/microsoft/VSSDK-Extensibility-Samples/tree/master/ArchivedSamples/Source_Code_Control_Provider

The AsyncPackageHelpers code came from the 'Backwards Compatible AsyncPackage 2013 code here:
https://github.com/microsoft/VSSDK-Extensibility-Samples/tree/master/Backwards_Compatible_AsyncPackage_2013/BackwardsCompatibleAsyncPackage/AsyncPackageHelpers

This was needed to get the extension to auto-load and auto-execute on load so that this
extension could detect when a solution was loaded that was using P4SimpleScc as the source
control provider.

See this page for some migration issues from VS2019 and VS2022:

https://docs.microsoft.com/en-us/visualstudio/extensibility/migration/breaking-api-list?view=vs-2022

