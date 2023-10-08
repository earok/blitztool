using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzStatement
	{
		public string Signature;
		public string Name;
		public bool IsFunction;
		public bool IsBlitzMode;

		public List<string> Lines = new List<string>();

		public BlitzStatement(string signature, bool isBlitzMode)
		{
			Signature = signature;
			IsBlitzMode = isBlitzMode;

			if (signature.ToLower().StartsWith("function"))
			{
				IsFunction = true;
			}

			Name = "";
			for (var i = signature.IndexOf(' ') + 1; i < signature.Length; i++)
			{
				if (Functions.EndOfName(signature[i]))
				{
					break;
				}
				Name += signature[i];
			}

		}

		internal void AddLine(string v)
		{
			Lines.Add(v);
		}

		internal void Process(List<string> finalOutput, List<BlitzGlobal> globals)
		{
			finalOutput.Add(Signature);

			foreach (var global in globals)
			{
				finalOutput.Add("Shared " + global.Value);
			}

			finalOutput.AddRange(Lines);

			if (IsFunction)
			{
				finalOutput.Add("End Function");
			}
			else
			{
				finalOutput.Add("End Statement");
			}

			finalOutput.Add("");
		}

		internal bool UnprocessedDependencies(HashSet<string> unprocessedStatements)
		{
			foreach (var line in Lines)
			{
				if (line.Contains('{') == false)
				{
					continue; //This line has absolutely no statements, so ignore it
				}

				var lowerLine = line.ToLower();

				foreach (var unprocessed in unprocessedStatements)
				{
					//We can reference ourselve
					if (unprocessed == Name.ToLower())
					{
						continue;
					}

					//This line has an unprocessed dependency
					if (lowerLine.Contains(unprocessed + '{'))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
