
//
// Copyright - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Extensions.VS;


namespace P4SimpleScc
{
    [ExportFileContextActionProvider((FileContextActionProviderOptions)VsCommandActionProviderOptions.SupportVsCommands, ProviderType, ProviderPriority.Normal, GuidList.SourceFileContextType)]
	public class ActionProviderFactory : IWorkspaceProviderFactory<IFileContextActionProvider>, IVsCommandActionProvider
	{
        // Unique Guid for WordCountActionProvider.
        private const string ProviderType = "0DD39C9C-3DE4-4B9C-BE19-7D011341A65B";

        private static readonly Guid ProviderCommandGroup = GuidList.GuidOpenFolderExtensibilityPackageCmdSet;

        public IFileContextActionProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new ActionProvider(workspaceContext);
        }

        public IReadOnlyCollection<CommandID> GetSupportedVsCommands()
        {
            return new List<CommandID>
            {
                new CommandID(GuidList.GuidOpenFolderExtensibilityPackageCmdSet, SccProvider.icmdWorkspaceCheckOutFile)
            };
        }

        internal class ActionProvider : IFileContextActionProvider
        {
            private IWorkspace workspaceContext;

            internal ActionProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            public Task<IReadOnlyList<IFileContextAction>> GetActionsAsync(string filePath, FileContext fileContext, CancellationToken cancellationToken)
            {
				// if P4SimpleScc is active but disabled, show the "Check Out File" menu item as disabled
				if (SccProvider.SolutionConfigType == 0)
				{
                    return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                    {
                    });
				}

				bool bIsCheckedOut = SccProvider.IsCheckedOut(filePath, out string stderr);

                if (!bIsCheckedOut)  // if the file is not already checked out, then add the menu item to check out the file
                {
                    return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                    {
                        new MyContextAction(
                            fileContext,
                            new Tuple<Guid, uint>(ProviderCommandGroup, SccProvider.icmdWorkspaceCheckOutFile),
                            "My Action",
                            async (fCtxt, progress, ct) =>
                            {
                                await DoWorkAsync(workspaceContext, fileContext, filePath);
                            }
                        )
                    });
                }
                else  // if the file is already checked out, we return an empty list, SccProvider.QueryStatus will intercept the menu command and disable it (grey it out)
                {
                    return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                    {
                    });
                }
            }

            internal static async Task DoWorkAsync(IWorkspace workspaceContext, FileContext fileContext, string filePath)
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                SccProvider.CheckOutFile(filePath);

                // invoke the FileContextChanged event to force the menu to be recreated (since the action above will change the state of the file)
                await fileContext.OnFileContextChanged.InvokeAsync(null, new EventArgs());
            }

            internal class MyContextAction : IFileContextAction, IVsCommandItem
            {
                private Func<FileContext, IProgress<IFileContextActionProgressUpdate>, CancellationToken, Task> executeAction;

                internal MyContextAction(
                    FileContext fileContext,
                    Tuple<Guid, uint> command,
                    string displayName,
                    Func<FileContext, IProgress<IFileContextActionProgressUpdate>, CancellationToken, Task> executeAction)
                {
                    this.executeAction = executeAction;
                    this.Source = fileContext;
                    this.CommandGroup = command.Item1;
                    this.CommandId = command.Item2;
                    this.DisplayName = displayName;
                }

                public Guid CommandGroup { get; }
                public uint CommandId { get; }
                public string DisplayName { get; }
                public FileContext Source { get; }

                public async Task<IFileContextActionResult> ExecuteAsync(IProgress<IFileContextActionProgressUpdate> progress, CancellationToken cancellationToken)
                {
                    await this.executeAction(this.Source, progress, cancellationToken);
                    return new FileContextActionResult(true);
                }

            }
        }
	}
}
