/***************************************************************************
 
Copyright (c) Microsoft Corporation. All rights reserved.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

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
   
		// Unique ID of the source control provider; this is also used as the command UI context to show/hide the pacakge UI
		public static readonly Guid guidSccProvider = new Guid("{B205A1B6-0000-4A1C-8680-97FD2219C692}");
		// The guid of the source control provider service (implementing IVsSccProvider interface)
		public static readonly Guid guidSccProviderService = new Guid("{B205A1B6-1000-4A1C-8680-97FD2219C692}");
		// The guid of the source control provider package (implementing IVsPackage interface)
		public static readonly Guid guidSccProviderPkg = new Guid("{B205A1B6-2000-4A1C-8680-97FD2219C692}");

		// Other guids for menus and commands
		public static readonly Guid guidSccProviderCmdSet = new Guid("{B205A1B6-9463-474A-807D-17F40BCFBB17}");
	};
}
