
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.AsyncPackageHelpers;
using PackageAutoLoadFlags = Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags;

using MsVsShell = Microsoft.VisualStudio.Shell;

using ClassLibrary;

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
	[@ProvideSourceControlProvider("P4SimpleScc", "#100")]

	// Pre-load the package when the command UI context is asserted (the provider will be automatically loaded after restarting the shell if it was active last time the shell was shutdown)
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad("B205A1B6-0000-4A1C-8680-97FD2219C692",PackageAutoLoadFlags.BackgroundLoad)]
	[Microsoft.VisualStudio.AsyncPackageHelpers.AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]

	// Register the key used for persisting solution properties, so the IDE will know to load the source control package when opening a controlled solution containing properties written by this package
	[ProvideSolutionProps(_strSolutionPersistanceKey)]
	// Declare the package guid
	[Guid("B205A1B6-2000-4A1C-8680-97FD2219C692")]  // this is GuidList.guidSccProviderPkg
	public sealed class SccProvider : MsVsShell.Package,
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

		public static Guid OutputPaneGuid = new Guid();

		public string solutionDirectory = "";
		public string solutionFile = "";
		public string solutionUserOptions = "";

		public bool bSolutionLoadedOutputDone = false;

		// P4SimpleScc solution configuration settings...
		public int SolutionConfigType = 0;  // 0 = disabled, 1 = automatic, 2 = manual settings
		public bool bCheckOutOnEdit = true;
		public bool bPromptForCheckout = false;
		public string P4Port = "";
		public string P4User = "";
		public string P4Client = "";

		public static bool P4SimpleSccConfigDirty = false;  // has the solution configuration for this solution been modified (and needs to be saved)?

		private CommandID menuCommandId;

		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

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

			base.Initialize();

			// Proffer the source control service implemented by the provider
			sccService = new SccProviderService(this);
			((IServiceContainer)this).AddService(typeof(SccProviderService), sccService, true);

			// Add our command handlers for menu (commands must exist in the .vsct file)
			MsVsShell.OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as MsVsShell.OleMenuCommandService;
			if (mcs != null)
			{
				// ToolWindow Command
				menuCommandId = new CommandID(GuidList.guidSccProviderCmdSet, CommandId);
				MenuCommand menuCmd = new MenuCommand(new EventHandler(Exec_menuCommand), menuCommandId);
				mcs.AddCommand(menuCmd);

				// menu command is hidden and disabled by default (will be enabled when the source control provider is enabled)
				menuCmd.Enabled = false;
				menuCmd.Visible = false;
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

			if (GetSolutionFileName() != null)
			{
				if (solutionDirectory != null && solutionDirectory.Length > 0)  // was solution already opened by the time we were loaded?
				{
					if (!bSolutionLoadedOutputDone)
					{
						string message = String.Format("Loaded solution: {0}\n", solutionFile);
						SccProvider.P4SimpleSccOutput(message);

						bSolutionLoadedOutputDone = true;
					}

					IVsSolutionPersistence solPersistence = (IVsSolutionPersistence)GetService(typeof(IVsSolutionPersistence));
					if (solPersistence != null)
					{
						// see if there are User Opts for this solution (this will load the Config settings and enable this provider if this solution is controlled by this provider
						solPersistence.LoadPackageUserOpts(this, _strSolutionUserOptionsKey);
					}
				}
			}
		}

		/// <summary>
		/// Unregister from receiving Solution Events and Project Documents, then release the resources used by the Package object
		/// </summary>
		/// <param name="disposing">true if the object is being disposed, false if it is being finalized</param>
		protected override void Dispose(bool disposing)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			sccService.Dispose();

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

			IVsOutputWindow output = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

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

				// Create a new output pane.
				bool visible = true;
				bool clearWithSolution = false;
				output.CreatePane(ref OutputPaneGuid, "P4SimpleScc", Convert.ToInt32(visible), Convert.ToInt32(clearWithSolution));

				P4SimpleSccOutput("P4SimpleScc enabled\n");
			}
			else
			{
				// Remove the existing output pane
				output.DeletePane(ref OutputPaneGuid);
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
				if (SolutionConfigType != 0)
				{
					Config.Set(Config.KEY.SolutionConfigType, SolutionConfigType);

					Config.Save(out string config_string);

					byte[] buffer = new byte[config_string.Length];
					uint BytesWritten = 0;

					System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
					encoding.GetBytes(config_string, 0, config_string.Length, buffer, 0);

					pOptionsStream.Write(buffer, (uint)config_string.Length, out BytesWritten);
				}

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
				}

				if (SolutionConfigType != 0)
				{
					IVsRegisterScciProvider rscp = (IVsRegisterScciProvider)GetService(typeof(IVsRegisterScciProvider));
					rscp.RegisterSourceControlProvider(GuidList.guidSccProvider);

					GetSolutionFileName();

					if (!bSolutionLoadedOutputDone && (solutionDirectory != null) && (solutionDirectory.Length > 0))
					{
						string message = String.Format("Loaded solution: {0}\n", solutionFile);
						SccProvider.P4SimpleSccOutput(message);

						bSolutionLoadedOutputDone = true;
					}

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


		#region Source Control Commands Execution

		private void Exec_menuCommand(object sender, EventArgs e)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

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

			SolutionConfigForm Dialog = new SolutionConfigForm(pos_x, pos_y, solutionDirectory, SolutionConfigType, bCheckOutOnEdit, bPromptForCheckout, P4Port, P4User, P4Client);

			System.Windows.Forms.DialogResult result = Dialog.ShowDialog();

			if (Dialog.PosX != pos_x || Dialog.PosY != pos_y)
			{
				Config.Set(Config.KEY.SolutionConfigDialogPosX, Dialog.PosX);
				Config.Set(Config.KEY.SolutionConfigDialogPosY, Dialog.PosY);

				P4SimpleSccConfigDirty = true;  // we need to write these to the solution .suo file
			}

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// set the global configuration settings
				SolutionConfigType = Dialog.SolutionConfigType;
				bCheckOutOnEdit = Dialog.bCheckOutOnEdit;
				bPromptForCheckout = Dialog.bPromptForCheckout;

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

				P4SimpleSccConfigDirty = true;  // we need to write these to the solution .suo file

				SetP4SettingsForSolution(solutionDirectory);  // update the settings based on the new SolutionConfigType

				ServerConnect();  // attempt to connect to the server using the new settings
			}

		}

		#endregion  // Source Control Commands Execution


		public void SetP4SettingsForSolution(string solutionDirectory)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (solutionDirectory != null && solutionDirectory.Length > 0)  // only set the P4 settings if we have a valid solution directory
			{
				// clear previous settings and load everything from the config settings for this solution
				P4Port = "";
				P4User = "";
				P4Client = "";

				Config.Get(Config.KEY.SolutionConfigCheckOutOnEdit, ref bCheckOutOnEdit);
				Config.Get(Config.KEY.SolutionConfigPromptForCheckout, ref bPromptForCheckout);

				if (SolutionConfigType == 1)  // if automatic settings
				{
					P4Command p4 = new P4Command();
					p4.RunP4Set(solutionDirectory, out P4Port, out P4User, out P4Client);
				}
				if (SolutionConfigType == 2)  // if manual settings
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
					P4SimpleSccOutput(message);
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

				p4.ServerConnect(out string stdout, out string stderr);
				if (stderr != null && stderr.Length > 0)
				{
					P4SimpleSccOutput("Connection to server failed!\n");
					P4SimpleSccOutput(stderr);

					string message = "Connection to server failed.  Use \"Show output from:\" set to 'P4SimpleScc' in Output window for more details.";
					MsVsShell.VsShellUtilities.ShowMessageBox(package, message, "P4SimpleScc Error", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				}
				else
				{
					P4SimpleSccOutput("Connection to server successful.\n");
				}
			}
		}

		public bool IsCheckedOut(string Filename)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			bool status = false;

			if (SolutionConfigType != 0)  // not disabled?
			{
				P4Command p4 = new P4Command();

				status = p4.IsCheckedOut(Filename, out string stdout, out string stderr);  // ignore stderr here since failure will be treated as if file is not checked out
			}

			return status;
		}

		public bool CheckOutFile(string Filename)
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			if (SolutionConfigType != 0)  // not disabled?
			{
				if (File.Exists(Filename))  // if the file doesn't exist, then it can't be in source control (i.e. brand new file)
				{
					P4Command p4 = new P4Command();

					P4Command.CheckOutStatus status = p4.CheckOutFile(Filename, out string stdout, out string stderr);

					// if file already checked out, or file not in client's root (outside workspace), or file is not in source control (brand new file, not yet added to source control)
					if (status == P4Command.CheckOutStatus.FileAlreadyCheckedOut || status == P4Command.CheckOutStatus.FileNotInSourceControl)
					{
						return true;
					}

					if (stderr != null && stderr.Length > 0)
					{
						string message = String.Format("edit of '{0}' failed.\n", Filename);
						P4SimpleSccOutput(message);
						P4SimpleSccOutput(stderr);

						return false;
					}
					else
					{
						string message = String.Format("edit '{0}' successful.\n", Filename);
						P4SimpleSccOutput(message);

						return true;
					}
				}
			}

			return true;  // return good status if P4SimpleScc is disabled for this solution
		}

		/// <summary>
		/// This function will output a line of text to the P4SimpleScc Output window (for logging purposes for P4 commands)
		/// </summary>
		/// <param name="text">text string to be output to the P4SimpleScc Output window (user must include \n character for newline at the end of the text string)
		public static void P4SimpleSccOutput(string text)  // user must include \n newline characters
		{
			MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

			IVsOutputWindow output = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

			if (output != null)
			{
				// Retrieve the output pane.
				output.GetPane(ref OutputPaneGuid, out IVsOutputWindowPane OutputPane);

				if (OutputPane != null)
				{
					OutputPane.OutputString(DateTime.Now.ToString("MM/dd/yyyy HH:mm::ss - "));
					OutputPane.OutputString(text);
				}
			}
		}

	}
}
