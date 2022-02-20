using System;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using System.Drawing;

namespace ClassLibrary
{
	public static class Config
	{
		public enum KEY  // everything is 'int' unless otherwise noted
		{
			SolutionConfigType,
			SolutionConfigCheckOutOnEdit,  // bool
			SolutionConfigPromptForCheckout,  // bool
			SolutionConfigDialogPosX,
			SolutionConfigDialogPosY,
			SolutionConfigDialogP4Port,  // string
			SolutionConfigDialogP4User,  // string
			SolutionConfigDialogP4Client,  // string
		};

		private static Dictionary<string, string> ConfigDictionary = new Dictionary<string,string>();

		public static void Load(string InputConfig)
		{
			ConfigDictionary = new Dictionary<string,string>();

			string line;

			try
			{
				byte[] buffer = new byte[InputConfig.Length];
				System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
				encoding.GetBytes(InputConfig, 0, InputConfig.Length, buffer, 0);

				MemoryStream memory_stream = new MemoryStream(buffer);

				using (StreamReader sr = new StreamReader(memory_stream))
				{
					while (sr.Peek() >= 0)
					{
						line = sr.ReadLine().Trim();

						if (line.Length > 0)  // not a blank line?
						{
							int pos = line.IndexOf("=");

							if ((pos > 0) && (pos < (line.Length-1)))  // '=' must not be the first or last character of the line
							{
								string key = line.Substring(0, pos);
								string value = line.Substring(pos+1, (line.Length-pos)-1);

								ConfigDictionary.Add(key, value);
							}
						}
					}
				}
			}
			catch(Exception)
			{
			}
		}

		public static void Save(out string OutputString)
		{
			OutputString = "";

			try
			{
				foreach (KEY key in Enum.GetValues(typeof(KEY)))
				{
					string key_string = key.ToString();
					if (ConfigDictionary.ContainsKey(key_string))
					{
						OutputString += string.Format("{0}={1}", key_string, ConfigDictionary[key_string]) + "\n";
					}
				}
			}
			catch(Exception)
			{
			}
		}

		public static bool Get(KEY key, ref string value)
		{
			string key_string = key.ToString();
			if (ConfigDictionary.ContainsKey(key_string))
			{
				value = ConfigDictionary[key_string];
				return true;
			}
			return false;
		}

		public static void Set(KEY key, string value)
		{
			string key_string = key.ToString();

			if (ConfigDictionary.ContainsKey(key_string))  // if the key/value pair exists...
			{
				ConfigDictionary.Remove(key_string);  // ...remove the old key/value pair
			}

			ConfigDictionary.Add(key_string, value);  // add the key/value pair to the dictionary
		}


		public static bool Get(KEY key, ref int value)
		{
			string key_string = key.ToString();
			if (ConfigDictionary.ContainsKey(key_string))
			{
				value = 0;
				Int32.TryParse(ConfigDictionary[key_string], out value);
				return true;
			}
			return false;
		}

		public static void Set(KEY key, int value)
		{
			Set(key, value.ToString());
		}


		public static bool Get(KEY key, ref bool value)
		{
			string key_string = key.ToString();
			if (ConfigDictionary.ContainsKey(key_string))
			{
				value = ConfigDictionary[key_string] == "True";
				return true;
			}
			return false;
		}

		public static void Set(KEY key, bool value)
		{
			Set(key, value.ToString());  // set to 'False' or 'True'
		}


		public static bool Get(KEY key, ref List<string> value)
		{
			value = new List<string>();

			int count = 1;
			bool found = false;
			do
			{
				string key_string = string.Format("{0}{1}", key.ToString(), count);
				found = ConfigDictionary.ContainsKey(key_string);
				if (found)
				{
					value.Add(ConfigDictionary[key_string]);
				}
				count++;
			}  while(found);

			return (value.Count() > 0);
		}

		public static void Set(KEY key, List<string> StringList)
		{
			int count = 1;
			bool found = false;
			// remove ALL old key/value pairs from the dictionary (since the list parameter passed in can be different size than what's currently in dictionary)
			do
			{
				string key_string = string.Format("{0}{1}", key.ToString(), count);
				found = ConfigDictionary.ContainsKey(key_string);
				if (found)
				{
					ConfigDictionary.Remove(key_string);
				}
				count++;
			}  while(found);

			// add the new key/value pairs to the dictionary
			for (int index = 0; index < StringList.Count; index++)
			{
				string key_string = string.Format("{0}{1}", key.ToString(), index+1);
				ConfigDictionary.Add(key_string, StringList[index]);
			}
		}


		public static bool Get(KEY key, ref List<int> value)
		{
			value = new List<int>();

			List<string> StringList = new List<string>();

			Get(key, ref StringList);

			for(int index = 0; index < StringList.Count; index++)
			{
				value.Add(Convert.ToInt32(StringList[index]));
			}

			return (value.Count() > 0);
		}

		public static void Set(KEY key, List<int> IntList)
		{
			List<string> StringList = new List<string>();

			for(int index = 0; index < IntList.Count; index++)
			{
				StringList.Add(IntList[index].ToString());
			}

			Set(key, StringList);
		}


		public static bool Get(KEY key, ref Font value)
		{
			string FontString = "";
			if (Get(key, ref FontString))
			{
				string[] fields = FontString.Split(',');

				if (fields.Length == 4)
				{
					if ((fields[0].Substring(0,1) == "\"") && (fields[0].Substring(fields[0].Length - 1,1) == "\""))
					{
						fields[0] = fields[0].Substring(1, fields[0].Length - 2);  // chop off the leading and trailing double quote
					}

					FontFamily font_family = new FontFamily(fields[0]);

					float size = float.Parse(fields[1]);
					int int_style = int.Parse(fields[2]);

					FontStyle style = FontStyle.Regular;
					if ((int_style & 1) != 0)
					{
						style = style | FontStyle.Bold;
					}
					if ((int_style & 2) != 0)
					{
						style = style | FontStyle.Italic;
					}

					GraphicsUnit graphics_unit = (GraphicsUnit) Enum.Parse(typeof(GraphicsUnit), fields[3]);

					value = new Font(font_family, size, style, graphics_unit);
					return true;
				}
			}

			return false;
		}

		public static void Set(KEY key, Font value)
		{
			int Style = 0;  // Regular
			if (value.Style.HasFlag(FontStyle.Bold))
			{
				Style |= 1;
			}
			if (value.Style.HasFlag(FontStyle.Italic))
			{
				Style |= 2;
			}

			int GraphicsUnit = (int)value.Unit;

			Set(key, String.Format("\"{0}\",{1},{2},{3}", value.FontFamily.Name, value.SizeInPoints, Style, GraphicsUnit));
		}
	}
}
