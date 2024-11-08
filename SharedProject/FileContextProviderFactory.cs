
//
// Copyright - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Workspace;
using Task = System.Threading.Tasks.Task;

namespace P4SimpleScc
{
    /// <summary>
    /// File context provider for TXT files.
    /// </summary>
    [ExportFileContextProvider(ProviderType, GuidList.SourceFileContextType)]
    public class FileContextProviderFactory : IWorkspaceProviderFactory<IFileContextProvider>
    {
        // Unique Guid for FileContextProvider.
        private const string ProviderType = "F0970AFD-11A4-41BF-8BEE-EF3FA288FB97";

        /// <inheritdoc/>
        public IFileContextProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new FileContextProvider(workspaceContext);
        }

        private class FileContextProvider : IFileContextProvider
        {
            private IWorkspace workspaceContext;

            internal FileContextProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            /// <inheritdoc />
            public async Task<IReadOnlyCollection<FileContext>> GetContextsForFileAsync(string filePath, CancellationToken cancellationToken)
            {
                var fileContexts = new List<FileContext>();

                fileContexts.Add(new FileContext(
                    new Guid(ProviderType),
                    new Guid(GuidList.SourceFileContextType),
                    filePath + "\n",
                    Array.Empty<string>()));

                return await Task.FromResult(fileContexts.ToArray());
            }
        }
    }
}
