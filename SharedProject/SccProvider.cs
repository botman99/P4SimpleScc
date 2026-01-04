
//
// Copyright - Jeffrey "botman" Broome
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

using PackageAutoLoadFlags = Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags;
using EnvDTE;

using MsVsShell = Microsoft.VisualStudio.Shell;

using ClassLibrary;
using Microsoft.VisualStudio.Shell;

namespace P4SimpleScc
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	// Declare that resources for the package are to be found in the managed assembly resources, and not in a satellite dll
	[MsVsShell.PackageRegistration(UseManagedResourcesOnly = true)]
	// Register the resource ID of the CTMENU section (generated from compiling the VSCT file), so the IDE will know how to merge this package's menus with the rest of the IDE when "devenv /setup" is run
	// The menu resource ID needs to match the ResourceName number defined in the csproj project file in the VSCTCompile section
	// Everytime the version number changes VS will automatically update the menus on startup; if the version doesn't change, you will need to run manually "devenv /setup /rootsuffix:Exp" to see VSCT changes reflected in IDE
	[MsVsShell.ProvideMenuResource("Menus.ctmenu", 1)]
	// Register the source control provider's service (implementing IVsScciProvider interface)
	[MsVsShell.ProvideService(typeof(SccProviderService), ServiceName = "P4SimpleScc")]
	// Register the source control provider to be visible in Tools/Options/SourceControl/Plugin dropdown selector
	[@ProvideSourceControlProvider("P4SimpleScc", GuidList.SccProviderGuidString, typeof(SccProvider), typeof(SccProviderService))]

	// Pre-load the package when the command UI context is asserted (the provider will be automatically loaded after restarting the shell if it was active last time the shell was shutdown)
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad("B205A1B6-0000-4A1C-8680-97FD2219C692",PackageAutoLoadFlags.BackgroundLoad)]
	[Microsoft.VisualStudio.AsyncPackageHelpers.AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
#if P4SIMPLESCC_VS2019
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
#endif

	// Register the key used for persisting solution properties, so the IDE will know to load the source control package when opening a controlled solution containing properties written by this package
	[ProvideSolutionProps(_strSolutionPersistanceKey)]
	// Declare the package guid
	[Guid("B205A1B6-2000-4A1C-8680-97FD2219C692")]  // this is GuidList.guidSccProviderPkg
	public sealed class SccProvider : MsVsShell.Package,
		IOleCommandTarget,
		IVsPersistSolutionProps	// We'll write properties in the solution file to track when solution is controlled; the interface needs to be implemented by the package object
	{
		// The service provider implemented by the package
		private SccProviderService sccService = null;

		// The name of this provider (to be written in solution and project files)
		// As a best practice, to be sure the provider has an unique name, a guid like the provider guid can be used as a part of the name
		private const string _strProviderName = "P4SimpleScc:{B205A1B6-2000-4A1C-8680-97FD2219C692}";  // this is GuidList.guidSccProviderPkg (same as this class's Guid)

		// The name of the solution section used to persist provider options (should be unique)
		private const string _strSolutionPersistanceKey = "P4SimpleSccProviderSolutionProperties";

		// The name of the section in the solution user options file used to persist user-specific options (should be unique, shorter than 31 characters and without dots)
		private const string _strSolutionUserOptionsKey = "P4SimpleSccProvider";

		// The names of the properties stored by the provider in the solution file
		private const string _strSolutionControlledProperty = "SolutionIsControlled";
		private const string _strSolutionBindingsProperty = "SolutionBindings";

		private static MsVsShell.Package package;
		public static System.IServiceProvider serviceProvider = null;
		private IOleCommandTarget pkgCommandTarget = null;

		public static Guid OutputPaneGuid = Guid.Empty;

		public string solutionDirectory = "";
		public string solutionFile = "";
		public string solutionUserOptions = "";

		public bool bSolutionLoadedOutputDone = false;

		public bool bIsWorkspace = false;  // 'true' if "Open Folder" was used to open this project, 'false' if project was opened using a Solution file

		public bool bReadUserOptionsCalled = false;  // this allows us to determine if user settings were attempted to be loaded
		public bool bUserSettingsWasEmpty = false;  // previously, if P4SimpleScc was disabled, settings were removed from the .sou file in the .vs folder, this bool will be 'true' if the settings are missing

		// P4SimpleScc solution configuration settings...
		public static int SolutionConfigType = 0;  // 0 = disabled, 1 = automatic, 2 = manual settings
		public static bool bUseNoAllWriteOptimization = false;
		public bool bCheckOutOnEdit = true;
		public bool bPromptForCheckout = false;
		public string P4Port = "";
		public string P4User = "";
		public string P4Client = "";
		public static bool bVerboseOutput = false;
		public static bool bOutputEnabled = false;

		public static bool P4SimpleSccConfigDirty = false;  // has the solution configuration for this solution been modified (and needs to be saved)?

		// 'noallwrite' will be set in Workspace options if 'allwrite' is unchecked in P4V (this causes 'bIsNoAllWrite' to be set to true)
		public static bool bIsNoAllWrite = true;

		private static object OpututQueueLock = new object();
		private static List<string> OutputQueueList = new List<string>();
		private System.Windows.Threading.DispatcherTimer OutputQueueTimer = null;

		private static char[] InvalidChars;

		// these are used by NodeExtender to keep track of which files are checked out so we can set the StateIcon properly
		public static HashSet<string> FilesCheckedOut = new HashSet<string>();

		// from C:\Program Files\Microsoft Visual Studio\2022\Professional\VSSDK\VisualStudioIntegration\Common\Inc\dteinternal.h
		private static Guid vsProjectItemKindPhysicalFile = new Guid("6bb5f8ee-4483-11d3-8bcf-00c04f8ec28c");

		// GetSccGlyph will attempt to check the status of every single file in the solution at startup, this is extremely
		// slow on solutions with thousands of files.  We skip this check at startup and use the FilesCheckedOutAtStart list
		// instead and then set 'bShouldSkipGetSccGlyphCheckoutStatus' to false when someone checks out or reverts a file.
		public static bool bShouldSkipGetSccGlyphCheckoutStatus = true;

		private CommandID menuCommandId;
		private CommandID menuCheckOutFileCommandId;
		private CommandID menuCheckOutDocumentFileCommandId;
		private CommandID menuRevertFileCommandId;
		private CommandID menuRevertDocumentFileCommandId;

		/// <summary>
		/// Command ID.
		/// </summary>
		public const int icmdSolutionConfiguration = 0x0100;
		public const int icmdCheckOutFile = 0x101;
		public const int icmdCheckOutDocumentFile = 0x102;
		public const int icmdWorkspaceCheckOutFile = 0x0501;

		public const int icmdRevertFile = 0x103;
		public const int icmdRevertDocumentFile = 0x104;
		public const int icmdWorkspaceRevertFile = 0x0502;


		/// <summary>
		/// Constructor
		/// </summary>
		public SccProvider()
		{
			// The provider implements the IVsPersistSolutionProps interface which is derived from IVsPersistSolutionOpts,
			// The base class MsVsShell.Package also implements IVsPersistSolutionOpts, so we're overriding its functionality
			// Therefore, to persist user options in the suo file we will not use the set of AddOptionKey/OnLoadOptions/OnSaveOptions
			// set of functions, but instead we'll use the IVsPersistSolutionProps functions directly.
		}

		#region Package Members

		/// <summary>
		/// Get an instance of the desired service type. (Calls Package.GetService)
		/// </summary>
		/// <param name="serviceType">The type of service to retrieve</param>
		/// <returns>An instance of the requested service, or null if the service could not be found</returns>
		public new object GetService(Type serviceType)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			return base.GetService(serviceType);
		}

		/// <summary>
		/// Initialize the Provider
		/// </summary>
		protected override void Initialize()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			package = this;
			serviceProvider = package as System.IServiceProvider;
			pkgCommandTarget = GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;

			base.Initialize();

			InvalidChars = Path.GetInvalidPathChars();  // get characters not allowed in file paths

			// Proffer the source control service implemented by the provider
			sccService = new SccProviderService(this);
			((IServiceContainer)this).AddService(typeof(SccProviderService), sccService, true);

			// Add our command handlers for menu (commands must exist in the .vsct file)
			MsVsShell.OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as MsVsShell.OleMenuCommandService;
			if (mcs != null)
			{
				// ToolWindow Command
				menuCommandId = new CommandID(GuidList.guidSccProviderCmdSet, icmdSolutionConfiguration);
				MenuCommand menuCmd = new MenuCommand(new EventHandler(Exec_menuCommand), menuCommandId);
				mcs.AddCommand(menuCmd);

				menuCheckOutFileCommandId = new CommandID(GuidList.guidSccProviderCmdSet, icmdCheckOutFile);
				MenuCommand menuCheckOutFileCmd = new MenuCommand(new EventHandler(Exec_menuCheckOutFileCommand), menuCheckOutFileCommandId);
				mcs.AddCommand(menuCheckOutFileCmd);

				menuRevertFileCommandId = new CommandID(GuidList.guidSccProviderCmdSet, icmdRevertFile);
				MenuCommand menuRevertFileCmd = new MenuCommand(new EventHandler(Exec_menuRevertFileCommand), menuRevertFileCommandId);
				mcs.AddCommand(menuRevertFileCmd);

				menuCheckOutDocumentFileCommandId = new CommandID(GuidList.guidSccProviderCmdSet, icmdCheckOutDocumentFile);
				MenuCommand menuCheckOutDocumentFileCmd = new MenuCommand(new EventHandler(Exec_menuCheckOutDocumentFileCommand), menuCheckOutDocumentFileCommandId);
				mcs.AddCommand(menuCheckOutDocumentFileCmd);

				menuRevertDocumentFileCommandId = new CommandID(GuidList.guidSccProviderCmdSet, icmdRevertDocumentFile);
				MenuCommand menuRevertDocumentFileCmd = new MenuCommand(new EventHandler(Exec_menuRevertDocumentFileCommand), menuRevertDocumentFileCommandId);
				mcs.AddCommand(menuRevertDocumentFileCmd);
			}

			bool bP4SimpleSccEnabled = false;

			SettingsManager settingsManager = new ShellSettingsManager(this);
			if (settingsManager != null)
			{
				WritableSettingsStore store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
				if (store.CollectionExists(@"P4SimpleScc"))
				{
					bP4SimpleSccEnabled = store.GetBoolean("P4SimpleScc", "bP4SimpleSccEnabled");
				}
			}

			if (bP4SimpleSccEnabled)  // only enable this source control provider by default if it was enabled the last time Visual Studio was shut down
			{
				// Register the provider with the source control manager
				// If the package is to become active, this will also callback on OnActiveStateChange and the menu commands will be enabled
				IVsRegisterScciProvider rscp = (IVsRegisterScciProvider)GetService(typeof(IVsRegisterScciProvider));
				if (rscp != null)
				{
					rscp.RegisterSourceControlProvider(GuidList.guidSccProvider);
				}
			}

			string solutionFile = GetSolutionFileName();
			if ( solutionFile != null)  // this is the path that will be taken when the solution is opened by double clicking or being specified as a commandline argument when launching Visual Studio
			{
				// this code can be hit after the solution/workspace is open due to the slowness of loading this provider (especially in Debug builds)
				// double check if the "solution" that was loaded is a solution file or a folder

				FileAttributes attribute = File.GetAttributes(solutionFile);
				if ((attribute & FileAttributes.Directory) == FileAttributes.Directory)
				{
					bIsWorkspace = true;
					NodeExtender.WorkspaceVisualNodeBaseList = new List<WorkspaceVisualNodeBase>();
				}
				else
				{
					bIsWorkspace = false;
				}

				if (solutionDirectory != null && solutionDirectory.Length > 0)  // was solution already opened by the time we were loaded?
				{
					IVsSolutionPersistence solPersistence = (IVsSolutionPersistence)GetService(typeof(IVsSolutionPersistence));
					if (solPersistence != null)
					{
						// see if there are User Opts for this solution (this will load the Config settings and enable this provider if this solution is controlled by this provider
						solPersistence.LoadPackageUserOpts(this, _strSolutionUserOptionsKey);
					}

					if (!bSolutionLoadedOutputDone && (solutionDirectory != null) && (solutionDirectory.Length > 0))
					{
						string message = String.Format("Loaded solution: {0}\n", solutionFile);
						SccProvider.P4SimpleSccQueueOutput(message);

						bSolutionLoadedOutputDone = true;
					}

					AfterOpenSolutionOrFolder();
				}
			}

			OutputQueueTimer = new System.Windows.Threading.DispatcherTimer();
			OutputQueueTimer.Interval = TimeSpan.FromMilliseconds(100);
			OutputQueueTimer.Tick += OnTimerEvent;
			OutputQueueTimer.Start();
		}

		/// <summary>
		/// Unregister from receiving Solution Events and Project Documents, then release the resources used by the Package object
		/// </summary>
		/// <param name="disposing">true if the object is being disposed, false if it is being finalized</param>
		protected override void Dispose(bool disposing)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (sccService != null)
			{
				sccService.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Checks whether the provider is invoked in command line mode
		/// </summary>
		public bool InCommandLineMode()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			IVsShell shell = (IVsShell)GetService(typeof(SVsShell));
			if (shell != null)
			{
				object pvar;
				if (shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out pvar) == VSConstants.S_OK &&
					(bool)pvar)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// This function is called by the IVsSccProvider service implementation when the active state of the provider changes
		/// If the package needs to refresh UI or perform other tasks, this is a good place to add the code
		/// </summary>
		public void OnActiveStateChange(bool bIsEnabled)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			MsVsShell.OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as MsVsShell.OleMenuCommandService;
			if (mcs != null)
			{
				var menuCommand = mcs.FindCommand(menuCommandId);
				menuCommand.Enabled = bIsEnabled;
				menuCommand.Visible = bIsEnabled;
			}

			// store user settings to indicate if this source control provider was last enabled or disabled when Visual Studio shut down (so we can read it when it starts up again)
			SettingsManager settingsManager = new ShellSettingsManager(this);
			if (settingsManager != null)
			{
				WritableSettingsStore store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
				if (!store.CollectionExists(@"P4SimpleScc"))
				{
					store.CreateCollection(@"P4SimpleScc");
				}

				store.SetBoolean("P4SimpleScc", "bP4SimpleSccEnabled", bIsEnabled);
			}

			if (bIsEnabled)
			{
				IVsRegisterScciProvider rscp = (IVsRegisterScciProvider)GetService(typeof(IVsRegisterScciProvider));
				if (rscp != null)
				{
					rscp.RegisterSourceControlProvider(GuidList.guidSccProvider);
				}
			}
		}

		/// <summary>
		/// Returns the name of the source control provider
		/// </summary>
		public string ProviderName
		{
			get { return _strProviderName; }
		}

		#endregion  // Package Members


		#region Source Control Utility Functions

		/// <summary>
		/// Returns the filename of the solution
		/// </summary>
		public string GetSolutionFileName()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			IVsSolution sol = (IVsSolution)GetService(typeof(SVsSolution));
			if (sol != null)
			{
				if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) == VSConstants.S_OK)
				{
					return solutionFile;
				}
			}

			return null;
		}

		#endregion  // Source Control Utility Functions


		//--------------------------------------------------------------------------------
		// IVsPersistSolutionProps specific functions
		//--------------------------------------------------------------------------------
		#region IVsPersistSolutionProps interface functions

		public int SaveUserOptions(IVsSolutionPersistence pPersistence)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// The shell will create a stream for the section of interest, and will call back the provider on
			// IVsPersistSolutionProps.WriteUserOptions() to save specific options under the specified key.
			if (P4SimpleSccConfigDirty)
			{
				pPersistence.SavePackageUserOpts(this, _strSolutionUserOptionsKey);
			}

			return VSConstants.S_OK;
		}

		public int LoadUserOptions(IVsSolutionPersistence pPersistence, uint grfLoadOpts)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// Note this can be during opening a new solution, or may be during merging of 2 solutions.
			// The provider calls the shell back to let it know which options keys from the suo file were written by this provider.
			// If the shell will find in the suo file a section that belong to this package, it will create a stream,
			// and will call back the provider on IVsPersistSolutionProps.ReadUserOptions() to read specific options
			// under that option key.
			pPersistence.LoadPackageUserOpts(this, _strSolutionUserOptionsKey);

			return VSConstants.S_OK;
		}

		public int WriteUserOptions(IStream pOptionsStream, string pszKey)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// This function gets called by the shell to let the package write user options under the specified key.
			// The key was declared in SaveUserOptions(), when the shell started saving the suo file.
			Debug.Assert(pszKey.CompareTo(_strSolutionUserOptionsKey) == 0, "The shell called to read an key that doesn't belong to this package");

			if (pszKey == _strSolutionUserOptionsKey)
			{
				Config.Set(Config.KEY.SolutionConfigType, SolutionConfigType);

				Config.Save(out string config_string);

				byte[] buffer = new byte[config_string.Length];
				uint BytesWritten = 0;

				System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
				encoding.GetBytes(config_string, 0, config_string.Length, buffer, 0);

				pOptionsStream.Write(buffer, (uint)config_string.Length, out BytesWritten);

				P4SimpleSccConfigDirty = false;
			}

			return VSConstants.S_OK;
		}

		public int ReadUserOptions(IStream pOptionsStream, string pszKey)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// This function is called by the shell if the _strSolutionUserOptionsKey section declared
			// in LoadUserOptions() as being written by this package has been found in the suo file.
			// Note this can be during opening a new solution, or may be during merging of 2 solutions.
			// A good source control provider may need to persist this data until OnAfterOpenSolution or OnAfterMergeSolution is called

			bReadUserOptionsCalled = true;

			string config_string = "";

			if (pszKey == _strSolutionUserOptionsKey)
			{
				uint buffer_size = 4096;  // allocate enough space for longest possible configuration string
				byte[] buffer = new byte[buffer_size];
				uint BytesRead = 0;

				pOptionsStream.Read(buffer, buffer_size, out BytesRead);

				config_string = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');  // trim nulls from end of string

				if (config_string != "")  // process the config settings
				{
					Config.Load(config_string);

					Config.Get(Config.KEY.SolutionConfigType, ref SolutionConfigType);

					Config.Get(Config.KEY.SolutionConfigAllWriteOptimization, ref bUseNoAllWriteOptimization);

					Config.Get(Config.KEY.SolutionConfigCheckOutOnEdit, ref bCheckOutOnEdit);
					Config.Get(Config.KEY.SolutionConfigPromptForCheckout, ref bPromptForCheckout);

					// NOTE: We don't initialize P4Port, P4User or P4Client here as these are handled in SetP4SettingsForSolution based on SolutionConfigType setting

					Config.Get(Config.KEY.SolutionConfigVerboseOutput, ref bVerboseOutput);
					Config.Get(Config.KEY.SolutionConfigOutputEnabled, ref bOutputEnabled);
				}
				else
				{
					bUserSettingsWasEmpty = true;  // no settings in the .suo file means P4SimpleScc was previously disabled (prior to verson 2.1)
				}

				if (bOutputEnabled)
				{
					CreateOutputPane();
				}

				GetSolutionFileName();					

				if (!bSolutionLoadedOutputDone && (solutionDirectory != null) && (solutionDirectory.Length > 0))
				{
					string message = String.Format("Loaded solution: {0}\n", solutionFile);
					SccProvider.P4SimpleSccQueueOutput(message);

					bSolutionLoadedOutputDone = true;
				}

				if (SolutionConfigType != 0)
				{
					IVsRegisterScciProvider rscp = (IVsRegisterScciProvider)GetService(typeof(IVsRegisterScciProvider));
					rscp.RegisterSourceControlProvider(GuidList.guidSccProvider);

					SetP4SettingsForSolution(solutionDirectory);

					ServerConnect();
				}
			}

			return VSConstants.S_OK;
		}

		public int QuerySaveSolutionProps(IVsHierarchy pHierarchy, VSQUERYSAVESLNPROPS[] pqsspSave)
		{
			// we don't use the solution props as they modify the .sln file (which may or may not be under source control)
			return VSConstants.S_OK;
		}

		public int SaveSolutionProps(IVsHierarchy pHierarchy, IVsSolutionPersistence pPersistence)
		{
			// we don't use the solution props as they modify the .sln file (which may or may not be under source control)
			return VSConstants.S_OK;
		}

		public int WriteSolutionProps(IVsHierarchy pHierarchy, string pszKey, IPropertyBag pPropBag)
		{
			// we don't use the solution props as they modify the .sln file (which may or may not be under source control)
			return VSConstants.S_OK;
		}

		public int ReadSolutionProps(IVsHierarchy pHierarchy, string pszProjectName, string pszProjectMk, string pszKey, int fPreLoad, IPropertyBag pPropBag)
		{
			// we don't use the solution props as they modify the .sln file (which may or may not be under source control)
			return VSConstants.S_OK;
		}

		public int OnProjectLoadFailure(IVsHierarchy pStubHierarchy, string pszProjectName, string pszProjectMk, string pszKey)
		{
			return VSConstants.S_OK;
		}

		#endregion  // IVsPersistSolutionProps

		public void AfterOpenSolutionOrFolder()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (bReadUserOptionsCalled && bUserSettingsWasEmpty)  // dirty hack to handle P4SimpleScc being disabled for a solution prior to version 2.1
			{
				P4SimpleSccConfigDirty = true;  // we need to save these settings (so that we save the 'disabled' setting)
			}

			ClassLibrary.P4Command p4 = new ClassLibrary.P4Command();

			if (GetSolutionFileName() != null && !String.IsNullOrEmpty(solutionDirectory))
			{
				if (!bReadUserOptionsCalled)  // don't automatically change anything if previous solution settings exists
				{
					// We may have a P4CONFIG file (https://www.perforce.com/manuals/v23.1/cmdref/Content/CmdRef/P4CONFIG.html),
					// in which case we want to default the solution config to automatic.
					// Start with checking whether P4 is even configured to look for one.
					p4.RunP4Set(solutionDirectory, out string P4Port, out string P4User, out string P4Client, out string P4Config, out string verbose);
					if (!string.IsNullOrEmpty(P4Config))
					{
						// The P4CONFIG environment variable is set, now go look for the file.
						string currentPath = solutionDirectory;

						// Loop until we reach the root directory.
						while (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
						{
							string filePath = Path.Combine(currentPath, P4Config);

							if (File.Exists(filePath))
							{
								// P4CONFIG exists! Start out defaulting to automatic and set up P4 settings.
								SolutionConfigType = 1;
								SetP4SettingsForSolution(solutionDirectory);

								p4.ServerConnect(out string stdout, out string stderr, out verbose, out bool bIsNotAllWrite);

								if (stderr != null && stderr.Length > 0) // if the P4CONFIG settings were not valid...
								{
									SolutionConfigType = 0;  // revert back to P4SimpleScc being disabled
								}
								else
								{
									P4SimpleSccConfigDirty = true;  // we need to save these settings
								}

								break;
							}

							var parentDir = Directory.GetParent(currentPath);
							if (parentDir == null)
							{
								break;
							}
							currentPath = parentDir.FullName;
						}
					}
				}
			}

			if (SolutionConfigType != 0)
			{
				bShouldSkipGetSccGlyphCheckoutStatus = true;
				FilesCheckedOut = new HashSet<string>();

				p4.GetCheckedOutFiles(out FilesCheckedOut);

				if (FilesCheckedOut.Count > 0)
				{
					// This code is needed if you launch Visual Studio by double clicking on a solution file.
					// This SccProvider is slow to load and the GetSccGlyph in SccProviderServices will not be loaded
					// by the time the solution explorer displays files.  The FilesCheckedOut will be populated
					// several seconds after the solution is fully loaded and so we need to update the glyph of
					// each file that is already checked out.  If you start Visual Studio and the load a project,
					// the SccProvider will be loaded and the 'FilesCheckedOut' list will be populated before
					// GetSccGlyph in SccProviderServices gets a chance to run (and that code will automatically
					// update the glyph on each of the checked out files.

					IVsSolution sol = (IVsSolution)GetService(typeof(SVsSolution));
					if (sol != null)
					{
						List<string> ProjectFilenames = new List<string>();
						List<IVsHierarchy> ProjectHierarchy = new List<IVsHierarchy>();

						// gather the list of projects in this solution up front so we don't have to iterate it for each project file below
						Guid nullGuid = Guid.Empty;
						int hr = sol.GetProjectEnum((uint)(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION), ref nullGuid, out IEnumHierarchies enumHierarchies);
						if (hr == VSConstants.S_OK)
						{
							IVsHierarchy[] hierarchy = new IVsHierarchy[1];
							while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
							{
								if (hierarchy[0] != null)
								{
									hierarchy[0].GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
									Project project = obj as Project;

									if ((project != null) && (project.FullName != null)&& (project.FullName != ""))
									{
										ProjectFilenames.Add(project.FullName);
										ProjectHierarchy.Add(hierarchy[0]);
									}
								}
							}
						}

						EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
						if (dte2 != null)
						{
							foreach (string Filename in FilesCheckedOut)
							{
								ProjectItem item = dte2.Solution.FindProjectItem(Filename);
								if (item == null)  // if there is no project item, then this Filename is a project filename
								{
									for (int index = 0; index < ProjectFilenames.Count; ++index)
									{
										if (ProjectFilenames[index] == Filename)
										{
											ProjectHierarchy[index].ParseCanonicalName(Filename, out uint itemId);

											VSITEMSELECTION itemSelection = new VSITEMSELECTION();
											itemSelection.pHier = ProjectHierarchy[index];
											itemSelection.itemid = itemId;

											UpdateGlyphOnSelectedFile(itemSelection, Filename);

											break;			
										}
									}
								}
								else
								{
									Project project = item.ContainingProject;

									sol.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy);
									if (projectHierarchy != null)
									{
										projectHierarchy.ParseCanonicalName(Filename, out uint itemId);

										VSITEMSELECTION itemSelection = new VSITEMSELECTION();
										itemSelection.pHier = projectHierarchy;
										itemSelection.itemid = itemId;

										UpdateGlyphOnSelectedFile(itemSelection, Filename);
									}
								}
							}
						}
					}
				}
			}
		}


		#region Source Control Command Enabling

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, System.IntPtr pCmdText)
		{
			// This code handles the right-click menu of files in the Solution Explorer (for solutions) and right-clicking open Documents for solutions and workspaces
			// The code that handles right-click menu for Solution Explorer for Workspaces is handled in GetActionsAsync of ActionProviderFactory

			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if ((pguidCmdGroup == GuidList.guidSccProviderCmdSet) || (pguidCmdGroup != GuidList.GuidOpenFolderExtensibilityPackageCmdSet))
			{
				// All source control commands needs to be hidden and disabled when the provider is not active
				if (!SccProviderService.Active)
				{
					prgCmds[0].cmdf = (int)OLECMDF.OLECMDF_INVISIBLE & (int)~(OLECMDF.OLECMDF_ENABLED);
					return VSConstants.S_OK;
				}

				if (prgCmds[0].cmdID == icmdSolutionConfiguration)
				{
					prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
					return VSConstants.S_OK;
				}
				else if (prgCmds[0].cmdID == icmdCheckOutFile)
				{
					if (SolutionConfigType == 0)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
						return VSConstants.S_OK;
					}

					GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> vsItemSelections, out List<string> Filenames);

					Debug.Assert(vsItemSelections.Count == Filenames.Count, "selectedNodes and selectedFiles not the same count");

					bool bAllFilesAreCheckedOut = true;  // assume all files are checked out

					foreach (string Filename in Filenames)
					{
						bool bIsCheckedOut = IsCheckedOut(Filename, out string stderr);

						bool bShouldIgnoreStatus = false;
						if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
						{
							bShouldIgnoreStatus = true;  // don't prevent file from being modified (since not under workspace or not under source control)
						}

						if (!bIsCheckedOut && !bShouldIgnoreStatus)
						{
							bAllFilesAreCheckedOut = false;
						}
					}

					if (bAllFilesAreCheckedOut)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
					}
					else
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
					}

					return VSConstants.S_OK;
				}
				else if (prgCmds[0].cmdID == icmdRevertFile)
				{
					if (SolutionConfigType == 0)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
						return VSConstants.S_OK;
					}

					GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> vsItemSelections, out List<string> Filenames);

					Debug.Assert(vsItemSelections.Count == Filenames.Count, "selectedNodes and selectedFiles not the same count");

					bool bNoFilesAreCheckedOut = true;  // assume no files are checked out

					foreach (string Filename in Filenames)
					{
						bool bIsCheckedOut = IsCheckedOut(Filename, out string stderr);

						bool bShouldIgnoreStatus = false;
						if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
						{
							bShouldIgnoreStatus = true;  // don't prevent file from being modified (since not under workspace or not under source control)
						}

						if (bIsCheckedOut && !bShouldIgnoreStatus)
						{
							bNoFilesAreCheckedOut = false;
						}
					}

					if (bNoFilesAreCheckedOut)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
					}
					else
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
					}

					return VSConstants.S_OK;
				}
				else if (prgCmds[0].cmdID == icmdCheckOutDocumentFile)
				{
					if (SolutionConfigType == 0)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
						return VSConstants.S_OK;
					}

					string Filename = "";

					EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
					if (dte2 != null)
					{
						Filename = dte2.ActiveDocument.FullName;
					}

					bool bIsCheckedOut = IsCheckedOut(Filename, out string stderr);

					bool bShouldIgnoreStatus = false;
					if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
					{
						bShouldIgnoreStatus = true;  // not under workspace or not under source control
					}

					if (bIsCheckedOut || bShouldIgnoreStatus)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
					}
					else
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
					}

					return VSConstants.S_OK;
				}
				else if (prgCmds[0].cmdID == icmdRevertDocumentFile)
				{
					if (SolutionConfigType == 0)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
						return VSConstants.S_OK;
					}

					string Filename = "";

					EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
					if (dte2 != null)
					{
						Filename = dte2.ActiveDocument.FullName;
					}

					bool bIsCheckedOut = IsCheckedOut(Filename, out string stderr);

					bool bShouldIgnoreStatus = false;
					if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
					{
						bShouldIgnoreStatus = true;  // don't prevent file from being modified (since not under workspace or not under source control)
					}

					if (!bIsCheckedOut || bShouldIgnoreStatus)
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
					}
					else
					{
						prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
					}

					return VSConstants.S_OK;
				}
			}

			return this.pkgCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		#endregion // Source Control Command Enabling


		#region Source Control Commands Execution

		private void Exec_menuCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			GetSolutionFileName();

			// see if we have a solution loaded or not
			if (solutionDirectory == null || solutionDirectory == "")
			{
				string message = "There is no solution loaded.  You need to load a solution before applying settings.";
				MsVsShell.VsShellUtilities.ShowMessageBox(package, message, "P4SimpleScc Warning", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

				return;
			}

			int pos_x = -1;
			Config.Get(Config.KEY.SolutionConfigDialogPosX, ref pos_x);
			int pos_y = -1;
			Config.Get(Config.KEY.SolutionConfigDialogPosY, ref pos_y);

			SolutionConfigForm Dialog = new SolutionConfigForm(pos_x, pos_y, solutionDirectory, SolutionConfigType, bUseNoAllWriteOptimization, bCheckOutOnEdit, bPromptForCheckout, bVerboseOutput, bOutputEnabled, P4Port, P4User, P4Client, VerboseOutput);

			System.Windows.Forms.DialogResult result = Dialog.ShowDialog();

			if (Dialog.PosX != pos_x || Dialog.PosY != pos_y)
			{
				Config.Set(Config.KEY.SolutionConfigDialogPosX, Dialog.PosX);
				Config.Set(Config.KEY.SolutionConfigDialogPosY, Dialog.PosY);

				P4SimpleSccConfigDirty = true;  // we need to save these settings
			}

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// set the global configuration settings
				SolutionConfigType = Dialog.SolutionConfigType;
				bUseNoAllWriteOptimization = Dialog.bUseNoAllWriteOptimization;
				bCheckOutOnEdit = Dialog.bCheckOutOnEdit;
				bPromptForCheckout = Dialog.bPromptForCheckout;
				bVerboseOutput = Dialog.bVerboseOutput;
				bOutputEnabled = Dialog.bOutputEnabled;

				if (bOutputEnabled && (OutputPaneGuid == Guid.Empty))  // if output is enabled and there's no output pane, then create one
				{
					CreateOutputPane();
				}
				else if (!bOutputEnabled && (OutputPaneGuid != Guid.Empty))  // else if output is not enabled and there is an output pane, then remove it
				{
					RemoveOutputPane();
				}

				if (Dialog.SolutionConfigType == 2)  // manual settings
				{
					P4Port = Dialog.P4Port;
					P4User = Dialog.P4User;
					P4Client = Dialog.P4Client;
				}
				else
				{
					P4Port = "";
					P4User = "";
					P4Client = "";
				}

				// save all the config settings
				Config.Set(Config.KEY.SolutionConfigType, SolutionConfigType);
				Config.Set(Config.KEY.SolutionConfigCheckOutOnEdit, bCheckOutOnEdit);
				Config.Set(Config.KEY.SolutionConfigPromptForCheckout, bPromptForCheckout);

				// these will be blank for all configurations except 'manual settings' (SetP4SettingsForSolution will re-initialize them at runtime)
				Config.Set(Config.KEY.SolutionConfigDialogP4Port, P4Port);
				Config.Set(Config.KEY.SolutionConfigDialogP4User, P4User);
				Config.Set(Config.KEY.SolutionConfigDialogP4Client, P4Client);

				Config.Set(Config.KEY.SolutionConfigAllWriteOptimization, bUseNoAllWriteOptimization);

				Config.Set(Config.KEY.SolutionConfigVerboseOutput, bVerboseOutput);
				Config.Set(Config.KEY.SolutionConfigOutputEnabled, bOutputEnabled);

				P4SimpleSccConfigDirty = true;  // we need to write these to the solution .suo file

				SetP4SettingsForSolution(solutionDirectory);  // update the settings based on the new SolutionConfigType

				ServerConnect();  // attempt to connect to the server using the new settings
			}

		}

		private void Exec_menuCheckOutFileCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			bShouldSkipGetSccGlyphCheckoutStatus = false;

			if (bIsWorkspace)
			{
				List<string> Filenames = GetSelectedWorkspaceFiles();

				foreach (string Filename in Filenames)
				{
					if (CheckOutFile(Filename))
					{
						foreach(WorkspaceVisualNodeBase NodeBase in NodeExtender.WorkspaceVisualNodeBaseList)
						{
							IFileNode FileNode = NodeBase as IFileNode;
							if ((FileNode != null) && (FileNode.FullPath == Filename))
							{
								NodeBase.SetStateIcon(NodeExtender.Microsoft_VisualStudio_ImageCatalog_Guid, NodeExtender.Moniker_CheckedOutForEdit_Id);
							}
						}
					}
				}
			}
			else  // solution
			{
				GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> selectedNodes, out List<string> selectedFiles);

				Debug.Assert(selectedNodes.Count == selectedFiles.Count, "selectedNodes and selectedFiles not the same count");

				for (int index = 0; index < selectedNodes.Count; index++)
				{
					if (CheckOutFile(selectedFiles[index]))
					{
						UpdateGlyphOnSelectedFile(selectedNodes[index], selectedFiles[index]);
					}
				}
			}
		}

		private void Exec_menuCheckOutDocumentFileCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			bShouldSkipGetSccGlyphCheckoutStatus = false;

			if (bIsWorkspace)
			{
				string Filename = "";

				EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
				if (dte2 != null)
				{
					Filename = dte2.ActiveDocument.FullName;
				}

				if (CheckOutFile(Filename))
				{
					foreach(WorkspaceVisualNodeBase NodeBase in NodeExtender.WorkspaceVisualNodeBaseList)
					{
						IFileNode FileNode = NodeBase as IFileNode;
						if ((FileNode != null) && (FileNode.FullPath == Filename))
						{
							NodeBase.SetStateIcon(NodeExtender.Microsoft_VisualStudio_ImageCatalog_Guid, NodeExtender.Moniker_CheckedOutForEdit_Id);
						}
					}
				}
			}
			else  // solution
			{
				GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> selectedNodes, out List<string> selectedFiles);

				Debug.Assert(selectedNodes.Count == selectedFiles.Count, "selectedNodes and selectedFiles not the same count");

				for (int index = 0; index < selectedNodes.Count; index++)
				{
					if (CheckOutFile(selectedFiles[index]))
					{
						UpdateGlyphOnSelectedFile(selectedNodes[index], selectedFiles[index]);
					}
				}
			}
		}

		private void Exec_menuRevertFileCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			bShouldSkipGetSccGlyphCheckoutStatus = false;

			if (bIsWorkspace)
			{
				List<string> Filenames = GetSelectedWorkspaceFiles();

				foreach (string Filename in Filenames)
				{
					if (RevertFile(Filename))
					{
						foreach(WorkspaceVisualNodeBase NodeBase in NodeExtender.WorkspaceVisualNodeBaseList)
						{
							IFileNode FileNode = NodeBase as IFileNode;
							if ((FileNode != null) && (FileNode.FullPath == Filename))
							{
								NodeBase.SetStateIcon(NodeExtender.Microsoft_VisualStudio_ImageCatalog_Guid, NodeExtender.Moniker_Blank_Id);
							}
						}
					}
				}
			}
			else  // solution
			{
				GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> selectedNodes, out List<string> selectedFiles);

				Debug.Assert(selectedNodes.Count == selectedFiles.Count, "selectedNodes and selectedFiles not the same count");

				for (int index = 0; index < selectedNodes.Count; index++)
				{
					if (RevertFile(selectedFiles[index]))
					{
						UpdateGlyphOnSelectedFile(selectedNodes[index], selectedFiles[index]);
					}
				}
			}
		}

		private void Exec_menuRevertDocumentFileCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			bShouldSkipGetSccGlyphCheckoutStatus = false;

			if (bIsWorkspace)
			{
				string Filename = "";

				EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
				if (dte2 != null)
				{
					Filename = dte2.ActiveDocument.FullName;
				}

				if (RevertFile(Filename))
				{
					foreach(WorkspaceVisualNodeBase NodeBase in NodeExtender.WorkspaceVisualNodeBaseList)
					{
						IFileNode FileNode = NodeBase as IFileNode;
						if ((FileNode != null) && (FileNode.FullPath == Filename))
						{
							NodeBase.SetStateIcon(NodeExtender.Microsoft_VisualStudio_ImageCatalog_Guid, NodeExtender.Moniker_Blank_Id);
						}
					}
				}
			}
			else  // solution
			{
				GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> selectedNodes, out List<string> selectedFiles);

				Debug.Assert(selectedNodes.Count == selectedFiles.Count, "selectedNodes and selectedFiles not the same count");

				for (int index = 0; index < selectedNodes.Count; index++)
				{
					if (RevertFile(selectedFiles[index]))
					{
						UpdateGlyphOnSelectedFile(selectedNodes[index], selectedFiles[index]);
					}
				}
			}
		}

		public void UpdateGlyphOnSelectedFile(VSITEMSELECTION itemSelection, string Filename)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (itemSelection.pHier == null)  // we can't display a moniker glyph on the solution file (since it doesn't have a project)
			{
				return;
			}

			IVsSolution sol = (IVsSolution)GetService(typeof(SVsSolution));
			if (sol != null)
			{
				IVsSccProject2 sccProject2 = itemSelection.pHier as IVsSccProject2;
				if ((sccProject2 != null) && (itemSelection.itemid != VSConstants.VSITEMID_NIL))
				{
					string[] rgpszFullPaths = new string[1];
					rgpszFullPaths[0] = Filename;
					VsStateIcon[] rgsiGlyphs = new VsStateIcon[1];
					uint[] rgdwSccStatus = new uint[1];
					sccService.GetSccGlyph(1, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

					uint[] rguiAffectedNodes = new uint[1];
					rguiAffectedNodes[0] = itemSelection.itemid;
					sccProject2.SccGlyphChanged(1, rguiAffectedNodes, rgsiGlyphs, rgdwSccStatus);
				}
			}
		}

		#endregion  // Source Control Commands Execution


		List<string> GetSelectedWorkspaceFiles()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// this returns a list of selected filenames from the Workspace Explorer

			List<string> selectedFiles = new List<string>();

			EnvDTE80.DTE2 dte2 = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
			if (dte2 != null)
			{
				EnvDTE.UIHierarchy uih = dte2.ToolWindows.SolutionExplorer;
				System.Array selectedItems = (System.Array)uih.SelectedItems;

				foreach (UIHierarchyItem selItem in selectedItems)
				{
					ProjectItem projItem = selItem.Object as ProjectItem;
					if (projItem != null)
					{
						string Filename = projItem.Properties.Item("FullPath").Value.ToString();
						selectedFiles.Add(Filename);
					}
				}
			}

			return selectedFiles;
		}

		void GetSelectedSolutionNodesAndFiles(out List<VSITEMSELECTION> selectedNodes, out List<string> selectedFiles)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			// this returns a list of selected nodes and their filenames from the Solution Explorer

			selectedNodes = new List<VSITEMSELECTION>();
			selectedFiles = new List<string>();

			IntPtr hierarchyPointer = IntPtr.Zero;
			IntPtr selectionContainerPointer = IntPtr.Zero;

			try
			{
				var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

				monitorSelection.GetCurrentSelection(out hierarchyPointer, out uint itemId, out IVsMultiItemSelect multiItemSelect, out selectionContainerPointer);

				if (itemId != VSConstants.VSITEMID_SELECTION)  // if a single node is selected...
				{
					IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPointer, typeof(IVsHierarchy)) as IVsHierarchy;

					if (selectedHierarchy == null)  // if the solution...
					{
						VSITEMSELECTION selectedItem;
						selectedItem.pHier = selectedHierarchy;
						selectedItem.itemid = itemId;
						selectedNodes.Add(selectedItem);
					}
					else  // otherwise check if project or file...
					{
						selectedHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObject);
						Project project = extObject as Project;

						if (project != null)  // if this item is a project...
						{
							VSITEMSELECTION selectedItem;
							selectedItem.pHier = selectedHierarchy;
							selectedItem.itemid = itemId;
							selectedNodes.Add(selectedItem);
						}
						else  // otherwise, check if this is a physical file
						{
							selectedHierarchy.GetGuidProperty(itemId, (int)__VSHPROPID.VSHPROPID_TypeGuid, out Guid typeGuid);
							if (typeGuid == vsProjectItemKindPhysicalFile)
							{
								VSITEMSELECTION selectedItem;
								selectedItem.pHier = selectedHierarchy;
								selectedItem.itemid = itemId;
								selectedNodes.Add(selectedItem);
							}
						}
					}
				}
				else if (multiItemSelect != null)
				{
					multiItemSelect.GetSelectionInfo(out uint numItems, out int isSingleHierarchyInt);

					bool bIsSingleHierarchy = isSingleHierarchyInt == 0 ? false : true;

					if (numItems > 0)
					{
						VSITEMSELECTION[] vsItemSelections = new VSITEMSELECTION[numItems];
						multiItemSelect.GetSelectedItems(0, numItems, vsItemSelections);

						try
						{
							foreach (VSITEMSELECTION vsItemSelection in vsItemSelections)
							{
								if (vsItemSelection.pHier == null)  // if the solution...
								{
									VSITEMSELECTION selectedItem;
									selectedItem.pHier = vsItemSelection.pHier;
									selectedItem.itemid = vsItemSelection.itemid;
									selectedNodes.Add(selectedItem);
								}
								else  // otherwise check if project or file...
								{
									vsItemSelection.pHier.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObject);
									Project project = extObject as Project;

									if (project != null)  // if this item is a project...
									{
										VSITEMSELECTION selectedItem;
										selectedItem.pHier = vsItemSelection.pHier;
										selectedItem.itemid = vsItemSelection.itemid;
										selectedNodes.Add(selectedItem);
									}
									else  // otherwise, check if this is a physical file
									{
										vsItemSelection.pHier.GetGuidProperty(vsItemSelection.itemid, (int)__VSHPROPID.VSHPROPID_TypeGuid, out Guid typeGuid);
										if (typeGuid == vsProjectItemKindPhysicalFile)
										{
											selectedNodes.Add(vsItemSelection);
										}
									}
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			finally
			{
				if (hierarchyPointer != IntPtr.Zero)
				{
					Marshal.Release(hierarchyPointer);
				}

				if (selectionContainerPointer != IntPtr.Zero)
				{
					Marshal.Release(selectionContainerPointer);	
				}
			}

			try
			{
				foreach (VSITEMSELECTION selectedNode in selectedNodes)
				{
					if (selectedNode.pHier == null)  // if this is the solution...
					{
						selectedFiles.Add(GetSolutionFileName());
					}
					else  // otherwise, get the filename from the project
					{
						IVsProject project = selectedNode.pHier as IVsProject;
						if (project != null)
						{
							project.GetMkDocument(selectedNode.itemid, out string Filename);

							if ((Filename != null) && (Filename != ""))
							{
								selectedFiles.Add(Filename);
							}
						}
					}
				}
			}
			catch
			{
			}
		}


		public void SetP4SettingsForSolution(string solutionDirectory)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (solutionDirectory != null && solutionDirectory.Length > 0)  // only set the P4 settings if we have a valid solution directory
			{
				// clear previous settings and load everything from the config settings for this solution
				P4Port = "";
				P4User = "";
				P4Client = "";

				if (SolutionConfigType == 1)  // if automatic settings
				{
					P4Command p4 = new P4Command();
					p4.RunP4Set(solutionDirectory, out P4Port, out P4User, out P4Client, out string verbose);

					// we don't need to worry about stderr output here since "p4 set" should never fail to run (as long as P4V is installed)
					if (bVerboseOutput && (verbose != null) && (verbose != ""))
					{
						if (!verbose.EndsWith("\n"))
						{
							verbose += "\n";
						}
						P4SimpleSccQueueOutput(verbose);
					}
				}
				else if (SolutionConfigType == 2)  // if manual settings
				{
					// always override all settings with the manual settings (from the Config class)
					Config.Get(Config.KEY.SolutionConfigDialogP4Port, ref P4Port);
					Config.Get(Config.KEY.SolutionConfigDialogP4User, ref P4User);
					Config.Get(Config.KEY.SolutionConfigDialogP4Client, ref P4Client);
				}

				string message = "";

				if (SolutionConfigType == 0)
				{
					message = String.Format("P4SimpleScc is disabled for this solution.\n");
				}
				else if (SolutionConfigType == 1)
				{
					message = String.Format("Using automatic settings: P4PORT={0}, P4USER={1}, P4CLIENT={2}\n", P4Port, P4User, P4Client);
				}
				else if (SolutionConfigType == 2)
				{
					message = String.Format("Using manual settings: P4PORT={0}, P4USER={1}, P4CLIENT={2}\n", P4Port, P4User, P4Client);
				}

				if (message != "")
				{
					P4SimpleSccQueueOutput(message);
				}

				// set the P4Command environment variables for future "p4" commands (like "p4 edit", etc.)
				P4Command.SetEnv(P4Port, P4User, P4Client);
			}
		}

		public void ServerConnect()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (SolutionConfigType != 0)  // not disabled?
			{
				P4Command p4 = new P4Command();

				p4.ServerConnect(out string stdout, out string stderr, out string verbose, out bIsNoAllWrite);

				if (bVerboseOutput && (verbose != null) && (verbose != ""))
				{
					if (!verbose.EndsWith("\n"))
					{
						verbose += "\n";
					}
					P4SimpleSccQueueOutput(verbose);
				}

				if (stderr != null && stderr.Length > 0)
				{
					P4SimpleSccQueueOutput("Connection to server failed!\n");
					P4SimpleSccQueueOutput(stderr);

					string message = "Connection to server failed.\n" + stderr;
					MsVsShell.VsShellUtilities.ShowMessageBox(package, message, "P4SimpleScc Error", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				}
				else
				{
					P4SimpleSccQueueOutput("Connection to server successful.\n");
				}
			}
		}

		public static bool IsCheckedOut(string Filename, out string stderr)
		{
			stderr = "";
			bool status = false;

			if (SolutionConfigType != 0)  // not disabled?
			{
				if (bUseNoAllWriteOptimization && !bIsNoAllWrite)
				{
					FileInfo info = new FileInfo(Filename);

					if (!info.IsReadOnly)
					{
						return true;
					}
				}

				P4Command p4 = new P4Command();

				status = p4.IsCheckedOut(Filename, out string stdout, out stderr, out string verbose);  // ignore stderr here since failure will be treated as if file is not checked out

				if (bVerboseOutput && (verbose != null) && (verbose != ""))
				{
					if (!verbose.EndsWith("\n"))
					{
						verbose += "\n";
					}
					P4SimpleSccQueueOutput(verbose);
				}
			}

			return status;
		}

		public static bool CheckOutFile(string Filename)
		{
			if (SolutionConfigType != 0)  // not disabled?
			{
				if (File.Exists(Filename))  // if the file doesn't exist, then it can't be in source control (i.e. brand new file not yet saved)
				{
					P4Command p4 = new P4Command();

					P4Command.CheckOutStatus status = p4.CheckOutFile(Filename, out string stdout, out string stderr, out string verbose);

					if (bVerboseOutput && (verbose != null) && (verbose != ""))
					{
						if (!verbose.EndsWith("\n"))
						{
							verbose += "\n";
						}
						P4SimpleSccQueueOutput(verbose);
					}

					// if the file is already checked out, or file not in client's root (outside workspace), or file is not in source control (brand new file, not yet added to source control)
					if (status == P4Command.CheckOutStatus.FileAlreadyCheckedOut || status == P4Command.CheckOutStatus.FileNotInSourceControl)
					{
						return true;
					}

					if (stderr != null && stderr.Length > 0)
					{
						string message = String.Format("Check out of '{0}' failed.\n", Filename);
						P4SimpleSccQueueOutput(message);
						P4SimpleSccQueueOutput(stderr);

						string dialog_message = message + stderr;
						MsVsShell.VsShellUtilities.ShowMessageBox(package, dialog_message, "P4SimpleScc Error", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

						return false;
					}
					else
					{
						string message = String.Format("Check out of '{0}' successful.\n", Filename);
						P4SimpleSccQueueOutput(message);

						FilesCheckedOut.Add(Filename);

						return true;
					}
				}
			}

			return true;  // return good status if P4SimpleScc is disabled for this solution
		}

		public static bool RevertFile(string Filename)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (SolutionConfigType != 0)  // not disabled?
			{
				if (File.Exists(Filename))  // if the file doesn't exist, then it can't be in source control (i.e. brand new file not yet saved)
				{
					IVsUIShell uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

					string revert_message = String.Format("Do you want to revert: '{0}'?", Filename);
					if (!VsShellUtilities.PromptYesNo(revert_message, "Warning", OLEMSGICON.OLEMSGICON_WARNING, uiShell))
					{
						return true;
					}

					P4Command p4 = new P4Command();

					P4Command.CheckOutStatus status = p4.RevertFile(Filename, out string stdout, out string stderr, out string verbose);

					if (bVerboseOutput && (verbose != null) && (verbose != ""))
					{
						if (!verbose.EndsWith("\n"))
						{
							verbose += "\n";
						}
						P4SimpleSccQueueOutput(verbose);
					}

					// if the file is not checked out, or file not in client's root (outside workspace), or file is not in source control (brand new file, not yet added to source control)
					if (status == P4Command.CheckOutStatus.FileNotCheckedOut || status == P4Command.CheckOutStatus.FileNotInSourceControl)
					{
						return true;
					}

					if (stderr != null && stderr.Length > 0)
					{
						string message = String.Format("Revert of '{0}' failed.\n", Filename);
						P4SimpleSccQueueOutput(message);
						P4SimpleSccQueueOutput(stderr);

						string dialog_message = message + stderr;
						MsVsShell.VsShellUtilities.ShowMessageBox(package, dialog_message, "P4SimpleScc Error", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

						return false;
					}
					else
					{
						string message = String.Format("Revert of '{0}' successful.\n", Filename);
						P4SimpleSccQueueOutput(message);

						FilesCheckedOut.Remove(Filename);

						return true;
					}
				}
			}

			return true;  // return good status if P4SimpleScc is disabled for this solution
		}

		public void VerboseOutput(string message)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (OutputPaneGuid == Guid.Empty)
			{
				CreateOutputPane();
			}

			string output = message;

			if (!message.EndsWith("\n"))  // add newline character at end if there isn't one
			{
				output += "\n";
			}

			P4SimpleSccQueueOutput(output, true);
		}

		/// <summary>
		/// This function will create the P4SimpleScc Output window (for logging purposes for P4 commands)
		/// </summary>
		private void CreateOutputPane()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (OutputPaneGuid != Guid.Empty)
			{
				return;
			}

			// Create a new output pane.
			IVsOutputWindow output = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

			OutputPaneGuid = Guid.NewGuid();

			bool visible = true;
			bool clearWithSolution = false;
			output.CreatePane(ref OutputPaneGuid, "P4SimpleScc", Convert.ToInt32(visible), Convert.ToInt32(clearWithSolution));
		}

		/// <summary>
		/// This function will remove the P4SimpleScc Output window (for logging purposes for P4 commands)
		/// </summary>
		private void RemoveOutputPane()
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			IVsOutputWindow output = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

			// Remove the existing output pane
			output.DeletePane(ref OutputPaneGuid);

			OutputPaneGuid = Guid.Empty;
		}

		/// <summary>
		/// This function will output a line of text to the P4SimpleScc Output window (for logging purposes for P4 commands)
		/// </summary>
		/// <param name="text">text string to be output to the P4SimpleScc Output window (user must include \n character for newline at the end of the text string)
		public static void P4SimpleSccQueueOutput(string text, bool bForceOutput = false)  // user must include \n newline characters
		{
			if (bOutputEnabled || bForceOutput)
			{
				string OutputMessage = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss - ") + text;

				lock (OpututQueueLock)
				{
					OutputQueueList.Add(OutputMessage);
				}
			}
		}

		private void OnTimerEvent(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			lock (OpututQueueLock)
			{
				IVsOutputWindow output = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

				if (output != null)
				{
					// Retrieve the output pane.
					output.GetPane(ref OutputPaneGuid, out IVsOutputWindowPane OutputPane);

					if (OutputPane != null)
					{
						foreach( string message in OutputQueueList)
						{
							OutputPane.OutputStringThreadSafe(message);
						}
					}
				}

				OutputQueueList.Clear();
			}
		}
	}
}
