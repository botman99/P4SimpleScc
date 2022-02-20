using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassLibrary
{
	public partial class SolutionConfigForm : Form
	{
		private bool bWindowInitComplete;  // set when form window is done initializing

		public int PosX = -1;
		public int PosY = -1;

		private string solutionDirectory = "";

		public int SolutionConfigType = 0;
		public bool bCheckOutOnEdit = true;
		public bool bPromptForCheckout = false;

		public string P4Port = "";
		public string P4User = "";
		public string P4Client = "";

		// save the manual settings that are passed in so we can display them (if needed) when switching solution configurations
		private string ManualP4Port = "";
		private string ManualP4User = "";
		private string ManualP4Client = "";

		public SolutionConfigForm(int InPosX, int InPosY, string InSolutionDirectory, int InSolutionConfigType, bool bInCheckOutOnEdit, bool bInPromptForCheckout, string InP4Port, string InP4User, string InP4Client)
		{
			bWindowInitComplete = false;  // we aren't done initializing the window yet

			InitializeComponent();

			solutionDirectory = InSolutionDirectory;
			SolutionConfigType = InSolutionConfigType;
			bCheckOutOnEdit = bInCheckOutOnEdit;
			bPromptForCheckout = bInPromptForCheckout;

			if (SolutionConfigType == 0)  // disabled
			{
				radioButtonDisabled.Checked = true;
			}
			else if (SolutionConfigType == 1)  // automatic
			{
				radioButtonAutomatic.Checked = true;
			}
			else if (SolutionConfigType == 2)  // manual
			{
				ManualP4Port = InP4Port;
				ManualP4User = InP4User;
				ManualP4Client = InP4Client;

				radioButtonManual.Checked = true;
			}

			if (bCheckOutOnEdit)
			{
				radioButtonOnModify.Checked = true;
			}
			else
			{
				radioButtonOnSave.Checked = true;
			}

			checkBoxPromptForCheckout.Checked = bPromptForCheckout;

			PosX = InPosX;
			PosY = InPosY;

			if ((PosX != -1) && (PosY != -1))
			{
				this.StartPosition = FormStartPosition.Manual;
				this.Location = new Point(PosX, PosY);
			}
			else  // otherwise, center the window on the parent form
			{
				this.StartPosition = FormStartPosition.CenterParent;
			}
		}

		private void OnShown(object sender, EventArgs e)
		{
			bWindowInitComplete = true;  // window initialization is complete
		}

		private void OnMove(object sender, EventArgs e)
		{
			if (bWindowInitComplete)
			{
				PosX = Location.X;
				PosY = Location.Y;
			}
		}

		private void TextBoxEnable(bool bEnable)
		{
			textBoxP4Port.ReadOnly = !bEnable;
			textBoxP4User.ReadOnly = !bEnable;
			textBoxP4Client.ReadOnly = !bEnable;
		}

		private void SetTextBoxText()
		{
			textBoxP4Port.Text = P4Port;
			textBoxP4User.Text = P4User;
			textBoxP4Client.Text = P4Client;
		}

		private void radioButtonDisabled_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButtonDisabled.Checked)
			{
				SolutionConfigType = 0;  // disabled

				P4Port = "";
				P4User = "";
				P4Client = "";

				TextBoxEnable(false);
				SetTextBoxText();
			}
		}

		private void radioButtonAutomatic_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButtonAutomatic.Checked)
			{
				SolutionConfigType = 1;  // automatic

				P4Command p4 = new P4Command();
				p4.RunP4Set(solutionDirectory, out P4Port, out P4User, out P4Client);

				TextBoxEnable(false);
				SetTextBoxText();
			}
		}

		private void radioButtonManual_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButtonManual.Checked)
			{
				SolutionConfigType = 2;  // manual settings

				P4Port = ManualP4Port;
				P4User = ManualP4User;
				P4Client = ManualP4Client;

				TextBoxEnable(true);
				SetTextBoxText();
			}
			else
			{
				// save the previous manual settings
				ManualP4Port = textBoxP4Port.Text;
				ManualP4User = textBoxP4User.Text;
				ManualP4Client = textBoxP4Client.Text;
			}
		}

		private void radioButtonOnModify_CheckedChanged(object sender, EventArgs e)
		{
			if (!bWindowInitComplete)
			{
				return;
			}

			if (radioButtonOnModify.Checked)
			{
				bCheckOutOnEdit = true;
			}
		}

		private void radioButtonOnSave_CheckedChanged(object sender, EventArgs e)
		{
			if (!bWindowInitComplete)
			{
				return;
			}

			if (radioButtonOnSave.Checked)
			{
				bCheckOutOnEdit = false;
			}
		}

		private void checkBoxPromptForCheckout_CheckedChanged(object sender, EventArgs e)
		{
			bPromptForCheckout = checkBoxPromptForCheckout.Checked;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (SolutionConfigType != 0)
			{
				if (textBoxP4Port.Text == "" || textBoxP4User.Text == "" || textBoxP4Client.Text == "")
				{
					string message = "P4PORT, P4USER or P4CLIENT is blank.  These must not be blank for things to work properly.  If you are using Windows environment variables, you will need to restart Visual Studio after changing them.  Changes via 'p4 set' or from the .p4config file do not require restarting Visual Studio.";
					string caption = "Invalid Settings";
					MessageBox.Show(message, caption, MessageBoxButtons.OK);

					return;
				}
			}

			if (SolutionConfigType == 2)  // manual settings?
			{
				P4Port = textBoxP4Port.Text;
				P4User = textBoxP4User.Text;
				P4Client = textBoxP4Client.Text;
			}

			this.DialogResult = DialogResult.OK;

			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;

			Close();
		}
	}
}
