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
		public HashSet<string> ReferencedStatements = new HashSet<string>();

		public BlitzStatement(string signature, bool isBlitzMode)
		{
			Signature = signature;
			IsBlitzMode = isBlitzMode;

			if (signature.ToLower().StartsWith("function"))
			{
				IsFunction = true;
			}

			if (signature.ToLower().Contains(";blitz")) //Force blitz mode
			{
				IsBlitzMode = true;
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

			//No statements to find
			v = v.Split(";")[0];
			if (v.Contains("{") == false)
			{
				return;
			}

			var symbolName = "";
			foreach (var c in v)
			{
				if (c == '{')
				{
					//Assume this whole word is a function?
					if (symbolName.Length > 0 && symbolName.StartsWith("!") == false)
					{
						ReferencedStatements.Add(symbolName.ToLower());
					}
					symbolName = "";
				}
				else if (Functions.EndOfName(c) || c == ' ')
				{
					symbolName = "";
				}
				else
				{
					symbolName += c;
				}
			}
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

					//We've referenced a statement that hasn't been processed yet
					if (ReferencedStatements.Contains(unprocessed))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
