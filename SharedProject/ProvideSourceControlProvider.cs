/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Globalization;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace P4SimpleScc
{
	/// <summary>
	/// This attribute registers the source control provider.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ProvideSourceControlProvider : MsVsShell.RegistrationAttribute
	{
		private readonly string _regName = null;
		private readonly Guid _regGuid;
		private readonly Type _packageType;
		private readonly Type _provider;

		/// <summary>
		/// Constructor
		/// </summary>
		public ProvideSourceControlProvider(string regName, string sourceControlGuid, Type sccPackage, Type sccProvider)
		{
			_regName = regName;
			_regGuid = new Guid(sourceControlGuid);
			_packageType = sccPackage;
			_provider = sccProvider;
		}

		/// <summary>
		///	 Called to register this attribute with the given context.  The context
		///	 contains the location where the registration inforomation should be placed.
		///	 It also contains other information such as the type being registered and path information.
		/// </summary>
		public override void Register(RegistrationContext context)
		{
			// http://msdn.microsoft.com/en-us/library/bb165948.aspx

			// Declare the source control provider, its name, the provider's service
			// and aditionally the packages implementing this provider
			using (Key sccProviders = context.CreateKey("SourceControlProviders"))
			{
				using (Key sccProviderKey = sccProviders.CreateSubkey(_regGuid.ToString("B")))
				{
					sccProviderKey.SetValue("", _regName);
					sccProviderKey.SetValue("Service", _provider.GUID.ToString("B"));

					using (Key sccProviderNameKey = sccProviderKey.CreateSubkey("Name"))
					{
						sccProviderNameKey.SetValue("", _regName);
						sccProviderNameKey.SetValue("Package", _packageType.GUID.ToString("B"));

						sccProviderNameKey.Close();
					}

					// Additionally, you can create a "Packages" subkey where you can enumerate the dll
					// that are used by the source control provider, something like "Package1"="P4SimpleSccProvider.dll"
					// but this is not a requirement.
					sccProviderKey.Close();
				}

				sccProviders.Close();
			}
		}

		/// <summary>
		/// Unregister the source control provider
		/// </summary>
		/// <param name="context">The Registration Context to be unregistered</param>
		public override void Unregister(RegistrationContext context)
		{
		}
	}
}
