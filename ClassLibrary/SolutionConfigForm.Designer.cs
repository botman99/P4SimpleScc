
namespace ClassLibrary
{
	partial class SolutionConfigForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.panel1 = new System.Windows.Forms.Panel();
			this.textBoxP4Client = new System.Windows.Forms.TextBox();
			this.textBoxP4User = new System.Windows.Forms.TextBox();
			this.textBoxP4Port = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.radioButtonManual = new System.Windows.Forms.RadioButton();
			this.radioButtonAutomatic = new System.Windows.Forms.RadioButton();
			this.radioButtonDisabled = new System.Windows.Forms.RadioButton();
			this.panel2 = new System.Windows.Forms.Panel();
			this.radioButtonOnSave = new System.Windows.Forms.RadioButton();
			this.radioButtonOnModify = new System.Windows.Forms.RadioButton();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.checkBoxPromptForCheckout = new System.Windows.Forms.CheckBox();
			this.checkBoxVerboseOutput = new System.Windows.Forms.CheckBox();
			this.checkBoxOutputEnabled = new System.Windows.Forms.CheckBox();
			this.checkBoxAllWriteOptimization = new System.Windows.Forms.CheckBox();
			this.label8 = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.textBoxP4Client);
			this.panel1.Controls.Add(this.textBoxP4User);
			this.panel1.Controls.Add(this.textBoxP4Port);
			this.panel1.Controls.Add(this.label7);
			this.panel1.Controls.Add(this.label6);
			this.panel1.Controls.Add(this.label5);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.radioButtonManual);
			this.panel1.Controls.Add(this.radioButtonAutomatic);
			this.panel1.Controls.Add(this.radioButtonDisabled);
			this.panel1.Location = new System.Drawing.Point(12, 12);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(506, 261);
			this.panel1.TabIndex = 0;
			// 
			// textBoxP4Client
			// 
			this.textBoxP4Client.Location = new System.Drawing.Point(76, 223);
			this.textBoxP4Client.Name = "textBoxP4Client";
			this.textBoxP4Client.ReadOnly = true;
			this.textBoxP4Client.Size = new System.Drawing.Size(416, 20);
			this.textBoxP4Client.TabIndex = 13;
			// 
			// textBoxP4User
			// 
			this.textBoxP4User.Location = new System.Drawing.Point(76, 197);
			this.textBoxP4User.Name = "textBoxP4User";
			this.textBoxP4User.ReadOnly = true;
			this.textBoxP4User.Size = new System.Drawing.Size(416, 20);
			this.textBoxP4User.TabIndex = 11;
			// 
			// textBoxP4Port
			// 
			this.textBoxP4Port.Location = new System.Drawing.Point(76, 171);
			this.textBoxP4Port.Name = "textBoxP4Port";
			this.textBoxP4Port.ReadOnly = true;
			this.textBoxP4Port.Size = new System.Drawing.Size(416, 20);
			this.textBoxP4Port.TabIndex = 9;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(9, 226);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(61, 13);
			this.label7.TabIndex = 12;
			this.label7.Text = "P4CLIENT:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(9, 200);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(53, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "P4USER:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 174);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(53, 13);
			this.label5.TabIndex = 8;
			this.label5.Text = "P4PORT:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 140);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(306, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "Enter the P4PORT, P4USER and P4CLIENT settings manually.";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 94);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(382, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "environment, or from previous \'p4 set\' settings, or from the .p4config file setti" +
    "ngs.";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 78);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(394, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Uses \'p4 set\' to get P4PORT, P4USER and P4CLIENT settings from the Windows";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 32);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(184, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Disable P4SimpleScc for this solution.";
			// 
			// radioButtonManual
			// 
			this.radioButtonManual.AutoSize = true;
			this.radioButtonManual.Location = new System.Drawing.Point(12, 120);
			this.radioButtonManual.Name = "radioButtonManual";
			this.radioButtonManual.Size = new System.Drawing.Size(101, 17);
			this.radioButtonManual.TabIndex = 6;
			this.radioButtonManual.TabStop = true;
			this.radioButtonManual.Text = "Manual Settings";
			this.radioButtonManual.UseVisualStyleBackColor = true;
			this.radioButtonManual.CheckedChanged += new System.EventHandler(this.radioButtonManual_CheckedChanged);
			// 
			// radioButtonAutomatic
			// 
			this.radioButtonAutomatic.AutoSize = true;
			this.radioButtonAutomatic.Location = new System.Drawing.Point(12, 58);
			this.radioButtonAutomatic.Name = "radioButtonAutomatic";
			this.radioButtonAutomatic.Size = new System.Drawing.Size(72, 17);
			this.radioButtonAutomatic.TabIndex = 3;
			this.radioButtonAutomatic.TabStop = true;
			this.radioButtonAutomatic.Text = "Automatic";
			this.radioButtonAutomatic.UseVisualStyleBackColor = true;
			this.radioButtonAutomatic.CheckedChanged += new System.EventHandler(this.radioButtonAutomatic_CheckedChanged);
			// 
			// radioButtonDisabled
			// 
			this.radioButtonDisabled.AutoSize = true;
			this.radioButtonDisabled.Location = new System.Drawing.Point(12, 12);
			this.radioButtonDisabled.Name = "radioButtonDisabled";
			this.radioButtonDisabled.Size = new System.Drawing.Size(66, 17);
			this.radioButtonDisabled.TabIndex = 1;
			this.radioButtonDisabled.TabStop = true;
			this.radioButtonDisabled.Text = "Disabled";
			this.radioButtonDisabled.UseVisualStyleBackColor = true;
			this.radioButtonDisabled.CheckedChanged += new System.EventHandler(this.radioButtonDisabled_CheckedChanged);
			// 
			// panel2
			// 
			this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel2.Controls.Add(this.radioButtonOnSave);
			this.panel2.Controls.Add(this.radioButtonOnModify);
			this.panel2.Location = new System.Drawing.Point(12, 347);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(506, 66);
			this.panel2.TabIndex = 16;
			// 
			// radioButtonOnSave
			// 
			this.radioButtonOnSave.AutoSize = true;
			this.radioButtonOnSave.Location = new System.Drawing.Point(12, 35);
			this.radioButtonOnSave.Name = "radioButtonOnSave";
			this.radioButtonOnSave.Size = new System.Drawing.Size(136, 17);
			this.radioButtonOnSave.TabIndex = 18;
			this.radioButtonOnSave.TabStop = true;
			this.radioButtonOnSave.Text = "Check out files on save";
			this.radioButtonOnSave.UseVisualStyleBackColor = true;
			this.radioButtonOnSave.CheckedChanged += new System.EventHandler(this.radioButtonOnSave_CheckedChanged);
			// 
			// radioButtonOnModify
			// 
			this.radioButtonOnModify.AutoSize = true;
			this.radioButtonOnModify.Location = new System.Drawing.Point(12, 12);
			this.radioButtonOnModify.Name = "radioButtonOnModify";
			this.radioButtonOnModify.Size = new System.Drawing.Size(143, 17);
			this.radioButtonOnModify.TabIndex = 17;
			this.radioButtonOnModify.TabStop = true;
			this.radioButtonOnModify.Text = "Check out files on modify";
			this.radioButtonOnModify.UseVisualStyleBackColor = true;
			this.radioButtonOnModify.CheckedChanged += new System.EventHandler(this.radioButtonOnModify_CheckedChanged);
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(325, 481);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(79, 29);
			this.buttonOK.TabIndex = 22;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(421, 481);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(79, 29);
			this.buttonCancel.TabIndex = 23;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// checkBoxPromptForCheckout
			// 
			this.checkBoxPromptForCheckout.AutoSize = true;
			this.checkBoxPromptForCheckout.Location = new System.Drawing.Point(25, 428);
			this.checkBoxPromptForCheckout.Name = "checkBoxPromptForCheckout";
			this.checkBoxPromptForCheckout.Size = new System.Drawing.Size(248, 17);
			this.checkBoxPromptForCheckout.TabIndex = 19;
			this.checkBoxPromptForCheckout.Text = "Prompt for permission before checking out files.";
			this.checkBoxPromptForCheckout.UseVisualStyleBackColor = true;
			this.checkBoxPromptForCheckout.CheckedChanged += new System.EventHandler(this.checkBoxPromptForCheckout_CheckedChanged);
			// 
			// checkBoxVerboseOutput
			// 
			this.checkBoxVerboseOutput.AutoSize = true;
			this.checkBoxVerboseOutput.Location = new System.Drawing.Point(144, 458);
			this.checkBoxVerboseOutput.Name = "checkBoxVerboseOutput";
			this.checkBoxVerboseOutput.Size = new System.Drawing.Size(226, 17);
			this.checkBoxVerboseOutput.TabIndex = 21;
			this.checkBoxVerboseOutput.Text = "Verbose Output ( for debugging purposes )";
			this.checkBoxVerboseOutput.UseVisualStyleBackColor = true;
			this.checkBoxVerboseOutput.CheckedChanged += new System.EventHandler(this.checkBoxVerboseOutput_CheckedChanged);
			// 
			// checkBoxOutputEnabled
			// 
			this.checkBoxOutputEnabled.AutoSize = true;
			this.checkBoxOutputEnabled.Location = new System.Drawing.Point(25, 458);
			this.checkBoxOutputEnabled.Name = "checkBoxOutputEnabled";
			this.checkBoxOutputEnabled.Size = new System.Drawing.Size(100, 17);
			this.checkBoxOutputEnabled.TabIndex = 20;
			this.checkBoxOutputEnabled.Text = "Output Enabled";
			this.checkBoxOutputEnabled.UseVisualStyleBackColor = true;
			this.checkBoxOutputEnabled.CheckedChanged += new System.EventHandler(this.checkBoxOutputEnabled_CheckedChanged);
			// 
			// checkBoxAllWriteOptimization
			// 
			this.checkBoxAllWriteOptimization.Location = new System.Drawing.Point(12, 279);
			this.checkBoxAllWriteOptimization.Name = "checkBoxAllWriteOptimization";
			this.checkBoxAllWriteOptimization.Size = new System.Drawing.Size(506, 39);
			this.checkBoxAllWriteOptimization.TabIndex = 14;
			this.checkBoxAllWriteOptimization.Text = "In a P4V Workspace with \'Allwrite\' checked (in the \'Advanced\' tab), treat files t" +
    "hat are writable (not \'read-only\') as checked out without checking with the Perf" +
    "orce server first.";
			this.checkBoxAllWriteOptimization.UseVisualStyleBackColor = true;
			this.checkBoxAllWriteOptimization.CheckedChanged += new System.EventHandler(this.checkBoxAllWriteOptimization_CheckedChanged);
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(25, 321);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(493, 23);
			this.label8.TabIndex = 15;
			this.label8.Text = "(the above causes files that are writable and not under source control to mistake" +
    "nly display \'Revert File\')";
			// 
			// SolutionConfigForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(531, 523);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.checkBoxAllWriteOptimization);
			this.Controls.Add(this.checkBoxOutputEnabled);
			this.Controls.Add(this.checkBoxVerboseOutput);
			this.Controls.Add(this.checkBoxPromptForCheckout);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SolutionConfigForm";
			this.Text = "Solution Configuration";
			this.Shown += new System.EventHandler(this.OnShown);
			this.Move += new System.EventHandler(this.OnMove);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton radioButtonManual;
		private System.Windows.Forms.RadioButton radioButtonAutomatic;
		private System.Windows.Forms.RadioButton radioButtonDisabled;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.RadioButton radioButtonOnSave;
		private System.Windows.Forms.RadioButton radioButtonOnModify;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxP4Client;
		private System.Windows.Forms.TextBox textBoxP4User;
		private System.Windows.Forms.TextBox textBoxP4Port;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.CheckBox checkBoxPromptForCheckout;
		private System.Windows.Forms.CheckBox checkBoxVerboseOutput;
		private System.Windows.Forms.CheckBox checkBoxOutputEnabled;
		private System.Windows.Forms.CheckBox checkBoxAllWriteOptimization;
		private System.Windows.Forms.Label label8;
	}
}