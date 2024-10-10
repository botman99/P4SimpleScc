/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

// SccProviderService.cs : Implementation of Sample Source Control Provider Service

using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace P4SimpleScc
{
	[Guid("B205A1B6-1000-4A1C-8680-97FD2219C692")]
	public class SccProviderService :
		IVsSccProvider,				// Required for provider registration with source control manager
		IVsSccManager2,				// Base source control functionality interface
		IVsSolutionEvents,			// We'll register for solution events, these are useful for source control
		IVsSolutionEvents7,			// We'll register for folder events, these are useful for source control
		IVsQueryEditQuerySave2,		// Required to allow editing of controlled files
		IDisposable 
	{
		// Whether the provider is active or not
		private bool _active = false;
		// The service and source control provider
		private SccProvider _sccProvider = null;
		// The cookie for solution events 
		private uint _vsSolutionEventsCookie;


		#region SccProvider Service initialization/unitialization

		public SccProviderService(SccProvider sccProvider)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Debug.Assert(null != sccProvider);
			_sccProvider = sccProvider;

			// Subscribe to solution events
			IVsSolution sol = (IVsSolution)_sccProvider.GetService(typeof(SVsSolution));
			sol.AdviseSolutionEvents(this, out _vsSolutionEventsCookie);
			Debug.Assert(VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie);
		}

		/// <summary>
		/// Unregister from receiving Solution Events and Project Documents
		/// </summary>
		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Unregister from receiving solution events
			if (VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie)
			{
				IVsSolution sol = (IVsSolution)_sccProvider.GetService(typeof(SVsSolution));
				sol.UnadviseSolutionEvents(_vsSolutionEventsCookie);
				_vsSolutionEventsCookie = VSConstants.VSCOOKIE_NIL;
			}
		}

		#endregion  // SccProvider


		//----------------------------------------------------------------------------
		// IVsSccProvider specific functions
		//--------------------------------------------------------------------------------
		#region IVsSccProvider interface functions

		public int SetActive()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Debug.WriteLine(string.Format(CultureInfo.CurrentUICulture, "The source control provider is now active"));

			_active = true;
			_sccProvider.OnActiveStateChange(true);

			return VSConstants.S_OK;
		}

		public int SetInactive()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Debug.WriteLine(string.Format(CultureInfo.CurrentUICulture, "The source control provider is now inactive"));

			_active = false;
			_sccProvider.OnActiveStateChange(false);

			return VSConstants.S_OK;
		}

		public int AnyItemsUnderSourceControl(out int pfResult)
		{
			if (!_active)
			{
				pfResult = 0;
			}
			else
			{
				// Although the parameter is an int, it's in reality a BOOL value, so let's return 0/1 values
				pfResult = (_sccProvider.SolutionConfigType != 0) ? 1 : 0;
			}
	
			return VSConstants.S_OK;
		}

		#endregion  // IVsSccProvider


		//----------------------------------------------------------------------------
		// IVsSccManager2 specific functions
		//--------------------------------------------------------------------------------
		#region IVsSccManager2 interface functions

		public int RegisterSccProject(IVsSccProject2 pscp2Project, string pszSccProjectName, string pszSccAuxPath, string pszSccLocalPath, string pszProvider)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (pszProvider.CompareTo(_sccProvider.ProviderName)!=0)
			{
				// If the provider name controlling this project is not our provider, the user may be adding to a 
				// solution controlled by this provider an existing project controlled by some other provider.
				// We'll deny the registration with scc in such case.
				return VSConstants.E_FAIL;
			}

			if (pscp2Project == null)
			{
				Debug.WriteLine(string.Format(CultureInfo.CurrentUICulture, "Solution {0} is registering with source control", _sccProvider.GetSolutionFileName()));
			}

			return VSConstants.S_OK;
		}

		public int UnregisterSccProject(IVsSccProject2 pscp2Project)
		{
			return VSConstants.S_OK;
		}

		public int GetSccGlyph(int cFiles, string[] rgpszFullPaths, VsStateIcon[] rgsiGlyphs, uint[] rgdwSccStatus)
		{
			return VSConstants.S_OK;
		}

		public int GetSccGlyphFromStatus(uint dwSccStatus, VsStateIcon[] psiGlyph)
		{
			psiGlyph[0] = VsStateIcon.STATEICON_BLANK; 
			return VSConstants.S_OK;
		}

		public int IsInstalled(out int pbInstalled)
		{
			// All source control packages should always return S_OK and set pbInstalled to nonzero
			pbInstalled = 1;
			return VSConstants.S_OK;
		}

		public int BrowseForProject(out string pbstrDirectory, out int pfOK)
		{
			// Obsolete method
			pbstrDirectory = null;
			pfOK = 0;
			return VSConstants.E_NOTIMPL;
		}

		public int CancelAfterBrowseForProject()
		{
			// Obsolete method
			return VSConstants.E_NOTIMPL;
		}

		#endregion  // IVsSccManager2


		//----------------------------------------------------------------------------
		// IVsSolutionEvents specific functions
		//--------------------------------------------------------------------------------
		#region IVsSolutionEvents interface functions

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return OnAfterOpenSolutionOrFolder();
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
		{
			return OnQueryCloseSolutionOrFolder();
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return OnBeforeCloseSolutionOrFolder();
		}

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			return OnAfterCloseSolutionOrFolder();
		}

		#endregion  // IVsSolutionEvents


		//----------------------------------------------------------------------------
		// IVsSolutionEvents7 specific functions
		//--------------------------------------------------------------------------------
		#region IVsSolutionEvents7 interface functions
		public void OnAfterOpenFolder(string folderPath)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var returnCode = OnAfterOpenSolutionOrFolder();
			if (returnCode != VSConstants.S_OK)
			{
				throw new NotImplementedException("Inner call returned unexpected code " + returnCode);
			}
		}

		public void OnQueryCloseFolder(string folderPath, ref int pfCancel)
		{
			var returnCode = OnQueryCloseSolutionOrFolder();
			if (returnCode != VSConstants.S_OK)
			{
				throw new NotImplementedException("Inner call returned unexpected code " + returnCode);
			}
		}

		public void OnBeforeCloseFolder(string folderPath)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var returnCode = OnBeforeCloseSolutionOrFolder();
			if (returnCode != VSConstants.S_OK)
			{
				throw new NotImplementedException("Inner call returned unexpected code " + returnCode);
			}
		}

		public void OnAfterCloseFolder(string folderPath)
		{
			var returnCode = OnAfterCloseSolutionOrFolder();
			if (returnCode != VSConstants.S_OK)
			{
				throw new NotImplementedException("Inner call returned unexpected code " + returnCode);
			}
		}

		[Obsolete("This API is no longer supported by Visual Studio.")]
		public void OnAfterLoadAllDeferredProjects()
		{ }
		#endregion	// IVsSolutionEvents7

		//----------------------------------------------------------------------------
		// IVsQueryEditQuerySave2 specific functions
		//--------------------------------------------------------------------------------
		#region IVsQueryEditQuerySave2 interface functions

		public int QueryEditFiles(uint rgfQueryEdit, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pfEditVerdict, out uint prgfMoreInfo)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Initialize output variables
			pfEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
			prgfMoreInfo = 0;

			if ((rgfQueryEdit & (uint)tagVSQueryEditFlags.QEF_ReportOnly) != 0)  // ignore any report query
			{
				return VSConstants.S_OK;
			}

			// In non-UI mode just allow the edit, because the user cannot be asked what to do with the file
			if (_sccProvider.InCommandLineMode())
			{
				return VSConstants.S_OK;
			}

			if (_sccProvider.SolutionConfigType != 0)  // if not disabled...
			{
				if (_sccProvider.bCheckOutOnEdit)
				{
					try 
					{
						//Iterate through all the files
						for (int iFile = 0; iFile < cFiles; iFile++)
						{
							uint fEditVerdict = (uint)tagVSQueryEditResult.QER_EditNotOK;  // assume not okay until known otherwise
							uint fMoreInfo = 0;

							bool fileExists = File.Exists(rgpszMkDocuments[iFile]);

							if (!fileExists)  // if the file doesn't exist then it's okay to edit as this is a brand new file (not saved yet)
							{
								fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
							}
							else
							{
								bool bIsCheckoutOkay = true;
								bool bIsCheckedOut = _sccProvider.IsCheckedOut(rgpszMkDocuments[iFile], out string stderr);

								bool bShouldIgnoreStatus = false;
								if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
								{
									bShouldIgnoreStatus = true;  // don't prevent file from being modified (since not under workspace or not under source control)
								}

								if (_sccProvider.bPromptForCheckout && !bIsCheckedOut && !bShouldIgnoreStatus)  // if prompt for permission to check out and file is not checked out...
								{
									IVsUIShell uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

									string message = String.Format("Do you want to check out: '{0}'?", rgpszMkDocuments[iFile]);
									if (!VsShellUtilities.PromptYesNo(message, "Warning", OLEMSGICON.OLEMSGICON_WARNING, uiShell))
									{
										bIsCheckoutOkay = false;

										pfEditVerdict = (uint)tagVSQueryEditResult.QER_NoEdit_UserCanceled;
										prgfMoreInfo = (uint)tagVSQueryEditResultFlags.QER_CheckoutCanceledOrFailed;
									}
								}

								if (bIsCheckoutOkay)
								{
									if (bIsCheckedOut || bShouldIgnoreStatus || _sccProvider.CheckOutFile(rgpszMkDocuments[iFile]))  // if file is already checked out or if we were able to check out the file, then edit is okay
									{
										fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
										fMoreInfo = (uint)tagVSQueryEditResultFlags.QER_MaybeCheckedout;
									}
									else
									{
										// if we couldn't check out the file, it's okay to edit it in memory, when the file is saved, it will try to check it out and dislay a "Save As" dialog if it couldn't be checked out
										fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
										fMoreInfo = (uint)tagVSQueryEditResultFlags.QER_InMemoryEdit;
									}
								}
							}

							// It's a bit unfortunate that we have to return only one set of flags for all the files involved in the operation
							// The edit can continue if all the files were approved for edit
							prgfMoreInfo |= fMoreInfo;
							pfEditVerdict |= fEditVerdict;
						}
					}
					catch(Exception)
					{
						// If an exception was caught, do not allow the edit
						pfEditVerdict = (uint)tagVSQueryEditResult.QER_EditNotOK;
						prgfMoreInfo = (uint)tagVSQueryEditResultFlags.QER_EditNotPossible;
					}
				}
			}

			return VSConstants.S_OK;
		}

		public int QuerySaveFiles(uint rgfQuerySave, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Initialize output variables
			// It's a bit unfortunate that we have to return only one set of flags for all the files involved in the operation
			// The last file will win setting this flag
			pdwQSResult = (uint)tagVSQuerySaveResult.QSR_SaveOK;

			// In non-UI mode attempt to silently flip the attributes of files or check them out 
			// and allow the save, because the user cannot be asked what to do with the file
			if (_sccProvider.InCommandLineMode())
			{
				rgfQuerySave = rgfQuerySave | (uint)tagVSQuerySaveFlags.QSF_SilentMode;
			}

			if (_sccProvider.SolutionConfigType != 0)  // if not disabled...
			{
				if (!_sccProvider.bCheckOutOnEdit)
				{
					try 
					{
						//Iterate through all the files
						for (int iFile = 0; iFile < cFiles; iFile++)
						{
							bool fileExists = File.Exists(rgpszMkDocuments[iFile]);  // if the file doesn't exist, then we don't need to check it out (it's a brand new file)

							if (fileExists)
							{
								bool bIsCheckoutOkay = true;
								bool bIsCheckedOut = _sccProvider.IsCheckedOut(rgpszMkDocuments[iFile], out string stderr);

								bool bShouldIgnoreStatus = false;
								if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
								{
									bShouldIgnoreStatus = true;  // don't prevent file from being modified (since not under workspace or not under source control)
								}

								if (_sccProvider.bPromptForCheckout && !bIsCheckedOut && !bShouldIgnoreStatus)  // if prompt for permission to check out and file is not checked out...
								{
									IVsUIShell uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

									string message = String.Format("Do you want to check out: '{0}'?", rgpszMkDocuments[iFile]);
									if (!VsShellUtilities.PromptYesNo(message, "Warning", OLEMSGICON.OLEMSGICON_WARNING, uiShell))
									{
										bIsCheckoutOkay = false;
										pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_UserCanceled;
									}
								}

								if (bIsCheckoutOkay && !bIsCheckedOut && !bShouldIgnoreStatus && !_sccProvider.CheckOutFile(rgpszMkDocuments[iFile]))  // if file exists and we couldn't check it out, then it's not okay to save the file
								{
									pdwQSResult = (uint)tagVSQuerySaveResult.QSR_ForceSaveAs;  // force a "Save As" dialog to save the file
								}
							}
						}
					}
					catch (Exception)
					{
						// If an exception was caught, do not allow the save
						pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_Cancel;
					}
				}
			}
	 
			return VSConstants.S_OK;
		}

		public int QuerySaveFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo, out uint pdwQSResult)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Delegate to the other QuerySave function
			string[] rgszDocuements = new string[1];
			uint[] rgrgf = new uint[1];
			rgszDocuements[0] = pszMkDocument;
			rgrgf[0] = rgf;

			return QuerySaveFiles(((uint)tagVSQuerySaveFlags.QSF_DefaultOperation), 1, rgszDocuements, rgrgf, pFileInfo, out pdwQSResult);
		}

		public int DeclareReloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
		{
			return VSConstants.S_OK;
		}

		public int DeclareUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterSaveUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
		{
			return VSConstants.S_OK;
		}

		public int IsReloadable(string pszMkDocument, out int pbResult)
		{
			// Since we're not tracking which files are reloadable and which not, consider everything reloadable
			pbResult = 1;

			return VSConstants.S_OK;
		}

		public int BeginQuerySaveBatch()
		{
			return VSConstants.S_OK;
		}

		public int EndQuerySaveBatch()
		{
			return VSConstants.S_OK;
		}

		#endregion  // IVsQueryEditQuerySave2

		private int OnAfterOpenSolutionOrFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_sccProvider.AfterOpenSolutionOrFolder();

			return VSConstants.S_OK;
		}

		private int OnBeforeCloseSolutionOrFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_sccProvider.GetSolutionFileName() != null)
			{
				if (_sccProvider.solutionDirectory != null && _sccProvider.solutionDirectory.Length > 0)
				{
					string message = String.Format("Unloading solution: {0}\n", _sccProvider.solutionFile);
					SccProvider.P4SimpleSccOutput(message);
				}
			}

			// set all configuration settings back to uninitialized
			_sccProvider.SolutionConfigType = 0;
			_sccProvider.bCheckOutOnEdit = true;
			_sccProvider.bPromptForCheckout = false;

			_sccProvider.P4Port = "";
			_sccProvider.P4User = "";
			_sccProvider.P4Client = "";

			_sccProvider.solutionDirectory = "";
			_sccProvider.solutionFile = "";

			_sccProvider.bSolutionLoadedOutputDone = false;

			_sccProvider.bReadUserOptionsCalled = false;
			_sccProvider.bUserSettingsWasEmpty = false;

			return VSConstants.S_OK;
		}

		private int OnAfterCloseSolutionOrFolder()
		{
			return VSConstants.S_OK;
		}

		private int OnQueryCloseSolutionOrFolder()
		{
			return VSConstants.S_OK;
		}

        /// <summary>
        /// Returns whether this source control provider is the active scc provider.
        /// </summary>
        public bool Active
        {
            get { return _active; }
        }

	}
}
