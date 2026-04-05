using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClassLibrary
{
	/// <summary>
	/// Interaction logic for SolutionConfigWPF.xaml
	/// </summary>
	public partial class SolutionConfigWPF : Window
	{
		private bool bWindowInitComplete = false;  // set when window is done initializing

		public int PosX = -1;
		public int PosY = -1;

		private string solutionDirectory = "";

		public bool _DialogResult = false;

		public int SolutionConfigType = 0;
		public bool bUseNoAllWriteOptimization = false;
		public bool bCheckOutOnEdit = true;
		public bool bPromptForCheckout = false;
		public bool bDisplayCheckedOutIcon = false;
		public bool bVerboseOutput = false;
		public bool bOutputEnabled = false;

		public string P4Port = "";
		public string P4User = "";
		public string P4Client = "";

		// save the manual settings that are passed in so we can display them (if needed) when switching solution configurations
		private string ManualP4Port = "";
		private string ManualP4User = "";
		private string ManualP4Client = "";

		public delegate void VerboseOutputDelegate(string message);
		private VerboseOutputDelegate VerboseOutput = null;

		public SolutionConfigWPF(System.Windows.Window OwnerWindow, int InPosX, int InPosY, string InSolutionDirectory, int InSolutionConfigType, bool bInUseNoAllWriteOptimization, bool bInCheckOutOnEdit, bool bInPromptForCheckout, bool bInDisplayCheckedOutIcon, bool bInVerboseOutput, bool bInOutputEnabled, string InP4Port, string InP4User, string InP4Client, VerboseOutputDelegate InVerboseOutput)
		{
			InitializeComponent();

			VerboseOutput = new VerboseOutputDelegate(InVerboseOutput);

			solutionDirectory = InSolutionDirectory;
			SolutionConfigType = InSolutionConfigType;
			bUseNoAllWriteOptimization = bInUseNoAllWriteOptimization;
			bCheckOutOnEdit = bInCheckOutOnEdit;
			bPromptForCheckout = bInPromptForCheckout;
			bDisplayCheckedOutIcon = bInDisplayCheckedOutIcon;
			bVerboseOutput = bInVerboseOutput;
			bOutputEnabled = bInOutputEnabled;

			P4Port = InP4Port;
			P4User = InP4User;
			P4Client = InP4Client;

			if (SolutionConfigType == 0)  // disabled
			{
				EnableDisabledButton.IsChecked = true;
			}
			else if (SolutionConfigType == 1)  // automatic
			{
				EnableAutomaticButton.IsChecked = true;
			}
			else if (SolutionConfigType == 2)  // manual
			{
				ManualP4Port = P4Port;
				ManualP4User = P4User;
				ManualP4Client = P4Client;

				EnableManualButton.IsChecked = true;
			}

			if (bCheckOutOnEdit)
			{
				CheckOutModifyButton.IsChecked = true;
			}
			else
			{
				CheckOutSaveButton.IsChecked = true;
			}

			AllwriteCheckbox.IsChecked = bUseNoAllWriteOptimization;

			PromptPermissionCheckbox.IsChecked = bPromptForCheckout;
			DisplayIconCheckbox.IsChecked = bDisplayCheckedOutIcon;
			VerboseOutputCheckbox.IsChecked = bVerboseOutput;
			OutputEnabledCheckbox.IsChecked = bOutputEnabled;

			VerboseOutputCheckbox.IsEnabled = bOutputEnabled;

			PosX = InPosX;
			PosY = InPosY;

			WindowStartupLocation = WindowStartupLocation.Manual;

			if ((PosX != -1) && (PosY != -1))
			{
				this.Left = PosX;
				this.Top = PosY;
			}
			else if (OwnerWindow != null)
			{
				// center this window on the parent window
				this.Left = Math.Max(0, OwnerWindow.Left + (OwnerWindow.Width / 2) - (this.Width / 2));
				this.Top = Math.Max(0, OwnerWindow.Top + (OwnerWindow.Height / 2) - (this.Height / 2));
			}
			else
			{
				this.Left = 0;
				this.Top = 0;
			}
		}

		private void ContentRendered_Event(object sender, EventArgs e)
		{
			PosX = (int)this.Left;
			PosY = (int)this.Top;

			bWindowInitComplete = true;  // window initialization is complete
		}

		private void WindowLocation_Changed(object sender, EventArgs e)
		{
			if (bWindowInitComplete)
			{
				PosX = (int)this.Left;
				PosY = (int)this.Top;
			}
		}

		private void TextBoxEnable(bool bEnable)
		{
			P4PORT.IsReadOnly = !bEnable;
			P4USER.IsReadOnly = !bEnable;
			P4CLIENT.IsReadOnly = !bEnable;
		}

		private void SetTextBoxText()
		{
			P4PORT.Text = P4Port;
			P4USER.Text = P4User;
			P4CLIENT.Text = P4Client;
		}

		private void EnableDisable_Checked(object sender, RoutedEventArgs e)
		{
			SolutionConfigType = 0;  // disabled

			P4Port = "";
			P4User = "";
			P4Client = "";

			// save the previous manual settings
			ManualP4Port = P4PORT.Text;
			ManualP4User = P4USER.Text;
			ManualP4Client = P4CLIENT.Text;

			TextBoxEnable(false);
			SetTextBoxText();
		}

		private void EnableAutomatic_Checked(object sender, RoutedEventArgs e)
		{
			SolutionConfigType = 1;  // automatic

			// save the previous manual settings
			ManualP4Port = P4PORT.Text;
			ManualP4User = P4USER.Text;
			ManualP4Client = P4CLIENT.Text;

			P4Command p4 = new P4Command();
			p4.RunP4Set(solutionDirectory, out P4Port, out P4User, out P4Client, out string verbose);

			// if we have enabled the 'bVerboseOutput' setting locally in this dialog box, we need to output the results of the "p4 set" command here...
			if ((VerboseOutput != null) && bOutputEnabled && bVerboseOutput)
			{
				VerboseOutput(verbose);
			}

			TextBoxEnable(false);
			SetTextBoxText();
		}

		private void EnableManual_Checked(object sender, RoutedEventArgs e)
		{
			SolutionConfigType = 2;  // manual settings

			// default to the previously set manual settings
			P4Port = ManualP4Port;
			P4User = ManualP4User;
			P4Client = ManualP4Client;

			TextBoxEnable(true);
			SetTextBoxText();
		}

		private void OutputEnabledCheckbox_Changed(object sender, RoutedEventArgs e)
		{
			VerboseOutputCheckbox.IsEnabled = (OutputEnabledCheckbox.IsChecked == true);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (SolutionConfigType != 0)
			{
				if (P4PORT.Text == "" || P4USER.Text == "" || P4CLIENT.Text == "")
				{
					string message = "P4PORT, P4USER or P4CLIENT is blank.  These must not be blank for things to work properly.  If you are using Windows environment variables, you will need to restart Visual Studio after changing them.  Changes via 'p4 set' or from the .p4config file do not require restarting Visual Studio.";
					string caption = "Invalid Settings";
					System.Windows.Forms.MessageBox.Show(message, caption, MessageBoxButtons.OK);

					return;
				}
			}

			bUseNoAllWriteOptimization = (AllwriteCheckbox.IsChecked == true);
			bCheckOutOnEdit = (CheckOutModifyButton.IsChecked == true);
			bPromptForCheckout = (PromptPermissionCheckbox.IsChecked == true);
			bDisplayCheckedOutIcon = (DisplayIconCheckbox.IsChecked == true);
			bOutputEnabled = (OutputEnabledCheckbox.IsChecked == true);
			bVerboseOutput = (VerboseOutputCheckbox.IsChecked == true);

			if (SolutionConfigType == 2)  // manual settings?
			{
				P4Port = P4PORT.Text;
				P4User = P4USER.Text;
				P4Client = P4CLIENT.Text;
			}

			_DialogResult = true;

			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			_DialogResult = false;

			Close();
		}
	}
}
