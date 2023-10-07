using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public static class Functions
	{
		static StringBuilder lineBuilder = new StringBuilder();

		public static void ExtractAllLines(List<string> lines, string filePath)
		{
			var sourceLines = File.ReadAllLines(filePath);
			foreach (var line in sourceLines)
			{
				//Throw out all whitespace
				var trimmedLine = line.Trim();

				//Find and process inner files
				if (trimmedLine.ToLower().StartsWith("include "))
				{
					var folderPath = Path.GetDirectoryName(filePath);
					var innerFilePath = line.Substring(8).Split(";")[0]; //
					ExtractAllLines(lines, folderPath + "\\" + innerFilePath);
					continue;
				}

				lines.Add(trimmedLine);
			}
		}

		internal static bool EndOfName(char v)
		{
			return v == '(' || v == ')' || v == '{' || v == '}' || v == ' ' || v == ',';
		}

		internal static string ProcessMacroLine(string line, List<string> parameters)
		{
			var outline = line;

			for (var i = 0; i < parameters.Count; i++)
			{
				outline = outline.Replace("`" + (i + 1), parameters[i]);
			}

			return outline;
		}
	}
}
