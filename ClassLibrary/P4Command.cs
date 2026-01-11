
//
// Copyright 2022 - Jeffrey "botman" Broome
//

using System;
using System.Text;

using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace ClassLibrary
{
	public class P4Command
	{
		private static string port;
		private static string user;
		private static string workspace;

		private StringBuilder stdoutBuilder;
		private StringBuilder stderrBuilder;
		private StringBuilder verboseBuilder;

		private bool StdOutDone;  // wait until OnOutputDataReceived receives e.Data == null to know that stdout has terminated
		private bool StdErrDone;  // wait until OnErrorDataReceived receives e.Data == null to know that stderr has terminated

		public enum CheckOutStatus
		{
			FileCheckedOut,				// the file was successfully checked out of source control
			FileAlreadyCheckedOut,		// the file was already checked out of source control
			FileNotInSourceControl,		// the file does not exist in source control
			ErrorCheckingOutFile,		// the file could not be checked out of source control
			FileReverted,				// the file was successfully reverted in source control
			FileNotCheckedOut,			// the file was not checked out of source control
			ErrorRevertingFile,			// the file could not be reverted in source control
		}

		private struct IsCheckedOutHistoryStruct
		{
			public long Milliseconds;
			public string Filename;
			public string stdout;
			public string stderr;
			public bool return_result;
		}

		// we keep a local cache of the check out status of files for a short amount of time so that we don't hit the server multiple times for the same file
		private static List<IsCheckedOutHistoryStruct> IsCheckedOutHistory = new List<IsCheckedOutHistoryStruct>();

		public P4Command()
		{
		}

		public static void SetEnv(string in_port, string in_user, string in_workspace)  // set the default port, user and workspace
		{
			port = in_port;
			user = in_user;
			workspace = in_workspace;
		}

		public void Run(string command, out string stdout, out string stderr, out string verbose)  // use the current port, user and workspace for the command
		{
			Run(command, port, user, workspace, out stdout, out stderr, out verbose);
		}

		public void Run(string command, string in_port, string in_user, string in_workspace, out string stdout, out string stderr, out string verbose)  // use the specified port, user and workspace for the command
		{
			stdoutBuilder = new StringBuilder();
			stderrBuilder = new StringBuilder();
			verboseBuilder = new StringBuilder();

			bool bTimedOut = false;

			try
			{
				Process proc = new Process();

				StdOutDone = false;
				StdErrDone = false;

				ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe");

				string arguments = "p4.exe";

				if (in_port != null && in_port.Length > 0)
				{
					arguments += String.Format(" -p {0}", in_port);
				}

				if (in_user != null && in_user.Length > 0)
				{
					arguments += String.Format(" -u {0}", in_user);
				}

				if (in_workspace != null && in_workspace.Length > 0)
				{
					arguments += String.Format(" -c {0}", in_workspace);
				}

				startInfo.UseShellExecute = false;
				startInfo.CreateNoWindow = true;

				startInfo.RedirectStandardInput = false;
				startInfo.RedirectStandardOutput = true;
				startInfo.RedirectStandardError = true;

				startInfo.Arguments = "/C " + arguments + " " + command;
				verboseBuilder.Append("command: " + arguments + " " + command + "\n");

				proc.StartInfo = startInfo;
				proc.EnableRaisingEvents = true;

				proc.OutputDataReceived += OnOutputDataReceived;
				proc.ErrorDataReceived += OnErrorDataReceived;

				proc.Start();
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();

				if (!proc.WaitForExit(5 * 60 * 1000))  // wait with a 5 minute timeout (in milliseconds)
				{
					bTimedOut = true;
					proc.Kill();
				}

				int output_timeout = 100;
				while (!StdOutDone || !StdErrDone)  // wait until the output and error streams have been flushed (or a 1 second (1000 ms) timeout is reached)
				{
					Thread.Sleep(10);

					if (--output_timeout == 0)
					{
						break;
					}
				}

				proc.Close();
			}
			catch (Exception ex)
			{
				string message = String.Format("P4Command.Run exception: {0}", ex.Message);
				System.Console.WriteLine(message);
			}

			stdout = stdoutBuilder.ToString();
			stderr = stderrBuilder.ToString();

			verboseBuilder.Append("response: " + stdout);
			if (!stdout.EndsWith("\n"))
			{
				verboseBuilder.Append("\n");
			}

			verboseBuilder.Append("error: " + stderr);
			if (!stderr.EndsWith("\n"))
			{
				verboseBuilder.Append("\n");
			}

			verbose = verboseBuilder.ToString();

			if (bTimedOut)  // did the P4 command take too long and time out?
			{
				stderr = "Perforce command timed out after waiting for 5 minutes.  Check your Solution Configuration settings to make sure they are correct.  You may want to try to manually check out or add the file in P4V.\n";
			}
		}

		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				stdoutBuilder.AppendLine(e.Data);
			}
			else
			{
				StdOutDone = true;
			}
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				stderrBuilder.AppendLine(e.Data);
			}
			else
			{
				StdErrDone = true;
			}
		}

		private bool GetField(string input, string fieldname, out string fieldvalue)  // scans input looking for field and returns value of field if found
		{
			fieldvalue = "";

			if (fieldname == null || fieldname.Length == 0)
			{
				return false;
			}

			if (input == null || input.Length == 0)
			{
				return false;
			}

			string line;

			try
			{
				byte[] buffer = new byte[input.Length];
				System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
				encoding.GetBytes(input, 0, input.Length, buffer, 0);

				MemoryStream memory_stream = new MemoryStream(buffer);

				using (StreamReader sr = new StreamReader(memory_stream))
				{
					while (sr.Peek() >= 0)
					{
						line = sr.ReadLine().Trim();

						if (line.Length > 0)  // not a blank line?
						{
							if (line[0] == '#')
							{
								continue;  // skip comment lines
							}

							int pos = line.IndexOf(fieldname);

							if (pos == 0)  // found a match?
							{
								fieldvalue = line.Substring(fieldname.Length, line.Length - fieldname.Length).Trim();

								return true;
							}
						}
					}
				}
			}
			catch(Exception)
			{
			}

			return false;
		}

		public void RunP4Set(string solutionDirectory, out string P4Port, out string P4User, out string P4Client, out string verbose)
		{
			string P4Config;
			RunP4Set(solutionDirectory, out P4Port, out P4User, out P4Client, out P4Config, out verbose);
		}


		public void RunP4Set(string solutionDirectory, out string P4Port, out string P4User, out string P4Client, out string P4Config, out string verbose)
		{
			P4Port = "";
			P4User = "";
			P4Client = "";
			P4Config = "";
			verbose = "";

			if (solutionDirectory == null || solutionDirectory == "")
			{
				return;
			}

			SetEnv(P4Port, P4User, P4Client);

			// the "p4.exe set" command must be run from the solution directory (to get the proper settings from the .p4config file)
			string CurrentDirectory = Directory.GetCurrentDirectory();  // save the current directory
			Directory.SetCurrentDirectory(solutionDirectory);

			P4Command p4cmd = new ClassLibrary.P4Command();

			p4cmd.Run("set", out string stdout, out string stderr, out verbose);  // note: "p4 set" does not require a port, user, password or workspace.

			Directory.SetCurrentDirectory(CurrentDirectory);  // restore the saved current directory

			if (stdout != null && stdout.Length > 0)
			{
				StringReader reader = new StringReader(stdout);
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Contains("P4PORT="))
					{
						int pos = line.IndexOf(" (");
						if (pos >= 0)  // if the line contains " (" then strip it and the following text
						{
							line = line.Remove(pos).Trim();
						}

						P4Port = line.Substring(7);
					}
					else if (line.Contains("P4USER="))
					{
						int pos = line.IndexOf(" (");
						if (pos >= 0)  // if the line contains " (" then strip it and the following text
						{
							line = line.Remove(pos).Trim();
						}

						P4User = line.Substring(7);
					}
					else if (line.Contains("P4CLIENT="))
					{
						int pos = line.IndexOf(" (");
						if (pos >= 0)  // if the line contains " (" then strip it and the following text
						{
							line = line.Remove(pos).Trim();
						}

						P4Client = line.Substring(9);
					}
					else if (line.Contains("P4CONFIG="))
					{
						int pos = line.IndexOf(" (");
						if (pos >= 0)  // if the line contains " (" then strip it and the following text
						{
							line = line.Remove(pos).Trim();
						}

						P4Config = line.Substring(9);
					}
				}
			}
		}

		public void ServerConnect(out string stdout, out string stderr, out string verbose, out bool bIsNoAllWrite)
		{
			verbose = "";
			bIsNoAllWrite = true;  // assume true until known otherwise

			Run("info -s", port, "", "", out stdout, out stderr, out string info_verbose);  // 'info' needs to run on the specified server (to make sure the server is valid)
			verbose += info_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				return;
			}

			string command = String.Format("users {0}", user);
			Run(command, port, user, "", out stdout, out stderr, out string users_verbose);  // get the user information from the specified server (to make sure the user is valid)
			verbose += users_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				return;
			}

			command = String.Format("client -o {0}", workspace);  // get the user's workspace from the specified server and then validate it
			Run(command, port, user, "", out stdout, out stderr, out string client_verbose);
			verbose += client_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				return;
			}

			// Parse output looking for "Access:" and "Root:" to validate the workspace settings on this machine
			string Access = "";
			if (!GetField(stdout, "Access:", out Access))
			{
				stderr = String.Format("P4CLIENT workspace '{0}' is not valid on server.\n", workspace);
				return;
			}

			string Root = "";
			if (!GetField(stdout, "Root:", out Root))
			{
				stderr = String.Format("P4CLIENT workspace '{0}' is not valid on server.\n", workspace);
				return;
			}

			string Options = "";
			if (!GetField(stdout, "Options:", out Options))
			{
				stderr = String.Format("P4CLIENT workspace '{0}' is not valid on server.\n", workspace);
				return;
			}

			bIsNoAllWrite = Options.Contains("noallwrite") ? true : false;

			// check if Root directory exists on this machine
			if (!Directory.Exists(Root))
			{
				stderr = String.Format("workspace Root: folder '{0}' does not exist on this machine.\n", Root);
				return;
			}
		}

		public bool IsCheckedOut(string Filename, out string stdout, out string stderr, out string verbose)
		{
			bool return_result = false;

			// first, remove any old entries from IsCheckedOutHistory...
			long time_now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

			for (int index = IsCheckedOutHistory.Count - 1; index >= 0; --index)
			{
				if (IsCheckedOutHistory[index].Milliseconds + 1000 < time_now)  // remove any entries that are over 1 second old
				{
					IsCheckedOutHistory.RemoveAt(index);
				}
			}

			// then, check if this file is in the history list...
			foreach(IsCheckedOutHistoryStruct history in IsCheckedOutHistory)
			{
				if (history.Filename == Filename)
				{
					stdout = history.stdout;
					stderr = history.stderr;
					verbose = "";  // we don't return any verbose text when the file matches an entry in the cache (to prevent duplicate logging)

					return history.return_result;
				}
			}

			// see if the file is checked out (for edit, not for integrate)
			string command = String.Format("fstat -T \"action\" \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out verbose);

			if (stderr == null || stderr.Length == 0)  // ignore errors here, if no error, check if file is open for edit
			{
				string action = "";
				GetField(stdout, "... action", out action);

				if (action == "edit")  // if already open for edit then we don't need to do anything
				{
					return_result = true;
				}
				else if (action == "add")  // if open for add then we don't need to do anything (GitHub issue #4)
				{
					return_result = true;
				}
			}

			// add the file to the CheckedOutHistory
			IsCheckedOutHistoryStruct newIsCheckedOut;
			newIsCheckedOut.Filename = Filename;
			newIsCheckedOut.stdout = stdout;
			newIsCheckedOut.stderr = stderr;
			newIsCheckedOut.Milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			newIsCheckedOut.return_result = return_result;

			IsCheckedOutHistory.Add(newIsCheckedOut);

			return return_result;
		}

		public CheckOutStatus CheckOutFile(string Filename, out string stdout, out string stderr, out string verbose)
		{
			verbose = "";

			// see if the file exists in source control
			string command = String.Format("fstat -T \"clientFile\" \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string clientFile_verbose);
			verbose += clientFile_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
				{
					return CheckOutStatus.FileNotInSourceControl;
				}
				else
				{
					return CheckOutStatus.ErrorCheckingOutFile;
				}
			}

			// see if the file is already checked out
			command = String.Format("fstat -T \"action\" \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string action_verbose);
			verbose += action_verbose;

			if (stderr == null || stderr.Length == 0)  // ignore errors here, if no error, check if file is open for edit
			{
				string action = "";
				GetField(stdout, "... action", out action);

				if (action == "edit")  // if already open for edit then we don't need to do anything
				{
					return CheckOutStatus.FileAlreadyCheckedOut;
				}
			}

			command = String.Format("edit \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string edit_verbose);
			verbose += edit_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				return CheckOutStatus.ErrorCheckingOutFile;
			}

			return CheckOutStatus.FileCheckedOut;
		}

		public CheckOutStatus RevertFile(string Filename, out string stdout, out string stderr, out string verbose)
		{
			verbose = "";

			// see if the file exists in source control
			string command = String.Format("fstat -T \"clientFile\" \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string clientFile_verbose);
			verbose += clientFile_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				if (stderr.Contains("is not under client's root") || stderr.Contains("not in client view") || stderr.Contains("no such file"))  // if file is outside client's workspace, or file does not exist in source control...
				{
					return CheckOutStatus.FileNotInSourceControl;
				}
				else
				{
					return CheckOutStatus.ErrorRevertingFile;
				}
			}

			// see if the file is checked out
			command = String.Format("fstat -T \"action\" \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string action_verbose);
			verbose += action_verbose;

			if (stderr != null || stderr.Length > 0)  // an error will occur if file isn't opened for edit
			{
				if (stderr.Contains("Field action doesn't exist."))
				{
					return CheckOutStatus.FileNotCheckedOut;
				}
			}

			command = String.Format("revert \"{0}\"", Filename);
			Run(command, out stdout, out stderr, out string edit_verbose);
			verbose += edit_verbose;

			if (stderr != null && stderr.Length > 0)
			{
				return CheckOutStatus.ErrorRevertingFile;
			}

			return CheckOutStatus.FileReverted;
		}

		public bool GetCheckedOutFiles(out HashSet<string> InFilesCheckedOutAtStart)
		{
			InFilesCheckedOutAtStart = new HashSet<string>();

			string command = String.Format("-F \"%action% %depotFile%\" opened");
			Run(command, out string stdout, out string stderr, out string verbose);

			if (stderr != null && stderr.Length > 0)
			{
				return false;
			}

			string[] openedFiles = stdout.Split(new char[] {'\r', '\n'});

			if (openedFiles.Length / 2 > 100)  // divide by 2 because every other line will be an empty newline
			{
				return false;
			}

			List<string> depotFiles = new List<string>();

			foreach(string openedFile in openedFiles)
			{
				if (openedFile.StartsWith("edit "))
				{
					depotFiles.Add(openedFile.Substring(5).Trim(new char[] {'\r', '\n'}));
				}
			}

			foreach(string depotFile in depotFiles)
			{
				command = String.Format("fstat \"{0}\"", depotFile);
				Run(command, out stdout, out stderr, out verbose);

				if (stderr == null || stderr.Length == 0)
				{
					string clientFile = "";
					GetField(stdout, "... clientFile", out clientFile);

					if (clientFile != "")
					{
						InFilesCheckedOutAtStart.Add(clientFile);
					}
				}
			}

			return true;
		}
	}
}
