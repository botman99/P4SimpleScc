
//
// Copyright - Jeffrey "botman" Broome
//

using System;

namespace P4SimpleScc
{
	/// <summary>
	/// This class is used only to expose the list of Guids used by this package.
	/// This list of guids must match the set of Guids used inside the VSCT file.
	/// </summary>
	public static class GuidList
	{
		// Now define the list of guids as public static members.
		public const string SccProviderGuidString = "{B205A1B6-0000-4A1C-8680-97FD2219C692}";

		// Unique ID of the source control provider; this is also used as the command UI context to show/hide the pacakge UI
		public static readonly Guid guidSccProvider = new Guid(SccProviderGuidString);
		// The guid of the source control provider service (implementing IVsSccProvider interface)
		public static readonly Guid guidSccProviderService = new Guid("{B205A1B6-1000-4A1C-8680-97FD2219C692}");
		// The guid of the source control provider package (implementing IVsPackage interface)
		public static readonly Guid guidSccProviderPkg = new Guid("{B205A1B6-2000-4A1C-8680-97FD2219C692}");

		// Other guids for menus and commands
		public static readonly Guid guidSccProviderCmdSet = new Guid("{B205A1B6-9463-474A-807D-17F40BCFBB17}");

        public static readonly Guid GuidOpenFolderExtensibilityPackageCmdSet = new Guid("e37cc989-b956-4a50-9515-b0395b288e4a");

        // Guid to associate file action factories.
        public const string SourceFileContextType = "2C4D13FF-FEA9-4AEC-A48E-17FD9D70E594";
	};
}
