
//
// Copyright - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Timers;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

using MsVsShell = Microsoft.VisualStudio.Shell;

namespace P4SimpleScc
{
	[Export(typeof(INodeExtender))]
	public class NodeExtender : INodeExtender
	{
		public static Guid Microsoft_VisualStudio_ImageCatalog_Guid = new Guid("{ae27a6b0-e345-4288-96df-5eaf394ee369}");

		public static int Moniker_Blank_Id = 278;
		public static int Moniker_CheckedOutForEdit_Id = 447;

		private IWorkspaceCommandHandler _handler = new NodeExtenderCommandHandler();

		private static Timer LoadTimer = null;
		private static Timer RefreshTimer = null;

		public static List<WorkspaceVisualNodeBase> WorkspaceVisualNodeBaseList = new List<WorkspaceVisualNodeBase>();

		public IChildrenSource ProvideChildren(WorkspaceVisualNodeBase parentNode) => null;

		public IWorkspaceCommandHandler ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
		{
			// This is a dirty hack.  There doesn't seem to be an event when the Workspace is fully loaded, so we
			// create a Timer that waits 3 seconds and retrigger the timer each time a new handle is created.
			// When that timer expires, we assume the workspace is done loading.

			if (LoadTimer == null)  // doesn't exist yet?  create it
			{
				RefreshTimer = null;

				LoadTimer = new Timer(3000);
				LoadTimer.Elapsed += OnLoadTimerEvent;
				LoadTimer.AutoReset = false;
				LoadTimer.Enabled = true;
				LoadTimer.Start();
			}
			else
			{
				// reset the timer
				LoadTimer.Stop();
				LoadTimer.Start();
			}

			if (parentNode is IFileNode)
			{
				WorkspaceVisualNodeBaseList.Add(parentNode);

				return _handler;
			}

			return null;
		}

		private static void OnLoadTimerEvent(Object source, ElapsedEventArgs e)
		{
			if (SccProvider.SolutionConfigType != 0)
			{
				foreach(WorkspaceVisualNodeBase NodeBase in WorkspaceVisualNodeBaseList)
				{
					IFileNode FileNode = NodeBase as IFileNode;
					if (FileNode != null)
					{
						if (SccProvider.FilesCheckedOut.Contains(FileNode.FullPath))
						{
							NodeBase.SetStateIcon(Microsoft_VisualStudio_ImageCatalog_Guid, Moniker_CheckedOutForEdit_Id);
						}
					}
				}
			}

			LoadTimer = null;

			RefreshTimer = new Timer(3000);
			RefreshTimer.Elapsed += OnRefreshTimerEvent;
			RefreshTimer.AutoReset = true;
			RefreshTimer.Enabled = true;
			RefreshTimer.Start();
		}

		private static void OnRefreshTimerEvent(Object source, ElapsedEventArgs e)
		{
			if (SccProvider.SolutionConfigType != 0)
			{
				foreach(WorkspaceVisualNodeBase NodeBase in WorkspaceVisualNodeBaseList)
				{
					IFileNode FileNode = NodeBase as IFileNode;
					if (FileNode != null)
					{
						if (SccProvider.FilesCheckedOut.Contains(FileNode.FullPath))
						{
							NodeBase.SetStateIcon(Microsoft_VisualStudio_ImageCatalog_Guid, Moniker_CheckedOutForEdit_Id);
						}
					}
				}
			}
		}

		public class NodeExtenderCommandHandler : IWorkspaceCommandHandler
		{
			public int Priority => 100;

			public bool IgnoreOnMultiselect => true;

			public int Exec(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
			{
				MsVsShell.ThreadHelper.ThrowIfNotOnUIThread();

				if (pguidCmdGroup == GuidList.GuidOpenFolderExtensibilityPackageCmdSet)
				{
					if (nCmdID == SccProvider.icmdWorkspaceCheckOutFile)
					{
						IFileNode FileNode = selection[0] as IFileNode;
						if (FileNode != null)
						{
							if (SccProvider.CheckOutFile(FileNode.FullPath))
							{
								selection[0].SetStateIcon(Microsoft_VisualStudio_ImageCatalog_Guid, Moniker_CheckedOutForEdit_Id);
							}
						}
					}
					else if (nCmdID == SccProvider.icmdWorkspaceRevertFile)
					{
						IFileNode FileNode = selection[0] as IFileNode;
						if (FileNode != null)
						{
							if (SccProvider.RevertFile(FileNode.FullPath))
							{
								selection[0].SetStateIcon(Microsoft_VisualStudio_ImageCatalog_Guid, Moniker_Blank_Id);
							}
						}
					}

					return (int)OLECMDF.OLECMDF_SUPPORTED;
				}

				return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
			}

			public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, ref uint cmdf, ref string customTitle)
			{
				if (selection.Count != 1 || !(selection[0] is IFileNode))
				{
					return false;
				}

				if (pguidCmdGroup == GuidList.GuidOpenFolderExtensibilityPackageCmdSet)
				{
					// All source control commands needs to be hidden and disabled when the provider is not active
					if (!SccProviderService.Active)
					{
						cmdf = (int)OLECMDF.OLECMDF_INVISIBLE & (int)~(OLECMDF.OLECMDF_ENABLED);
						return true;
					}

					string Filename = "";

					IFileNode FileNode = selection[0] as IFileNode;
					if (FileNode != null)
					{
						Filename = FileNode.FullPath;
					}

					bool bIsCheckedOut = SccProvider.IsCheckedOut(Filename, out string stderr);

					if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
					{
						cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
						return true;
					}

					if (nCmdID == SccProvider.icmdWorkspaceCheckOutFile)
					{
						if (SccProvider.SolutionConfigType == 0)
						{
							cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
							return true;
						}

						cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

						if (!bIsCheckedOut)
						{
							cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
						}

						return true;
					}
					else if (nCmdID == SccProvider.icmdWorkspaceRevertFile)
					{
						if (SccProvider.SolutionConfigType == 0)
						{
							cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
							return true;
						}

						cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

						if (bIsCheckedOut)
						{
							cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
						}

						return true;
					}

					cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
					return true;
				}

				return selection[0].Parent.QueryStatus(pguidCmdGroup, nCmdID, ref cmdf, ref customTitle);
			}
		}
	}
}
