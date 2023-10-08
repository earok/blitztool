using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlitzTool
{
	public class BlitzNewType
	{
		public string Signature;
		public string Name;
		public List<string> Fields = new List<string>();

		public HashSet<string> Dependencies = new HashSet<string>();
		public bool IsASM;

		public BlitzNewType(string signature)
		{
			Signature = signature;

			if (signature.ToLower().Contains(";asm"))
			{
				IsASM = true;
			}

			Name = "";
			for (var i = "newtype ".Length; i < signature.Length; i++)
			{
				if (Functions.EndOfName(signature[i]))
				{
					break;
				}
				Name += signature[i];
			}

			if (Name.StartsWith("."))
			{
				Name = Name.Substring(1);
			}
		}

		internal void AddField(string v)
		{
			v = v.Split(";")[0]; //Remove comments

			var lines = v.Split(":"); //Split into individual fields

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue; //Don't add empty lines
				}

				if (line.Contains("."))
				{
					Dependencies.Add(line.Split(".")[1].Split("[")[0].Trim().ToLower());
				}

				Fields.Add(line);
			}
		}

		internal void Process(List<string> finalOutput)
		{
			finalOutput.Add("NewType ." + Name);
			finalOutput.AddRange(Fields);
			finalOutput.Add("End NewType");
			finalOutput.Add("");
		}

		internal void ProcessASMOffsets(List<string> finalOutput, string baseName, ref int offset, Dictionary<string, BlitzNewType> newTypes)
		{
			foreach (var field in Fields)
			{
				var fieldData = Functions.GetFieldData(field);
				if (newTypes.ContainsKey(fieldData.fieldType.ToLower()) && fieldData.isPointer == false)
				{
					var subNewType = newTypes[fieldData.fieldType.ToLower()];
					subNewType.ProcessASMOffsets(finalOutput, baseName + "_" + fieldData.fieldName, ref offset, newTypes);
				}
				else
				{
					finalOutput.Add("#" + baseName + "_" + fieldData.fieldName + " = " + offset);
					offset += Functions.GetFieldLength(field, newTypes);
				}

			}
		}

		internal bool UnprocessedDependencies(HashSet<string> unprocessedNewTypes)
		{
			foreach (var dependency in Dependencies)
			{
				//We can ignore a self reference
				if (dependency == Name.ToLower())
				{
					continue;
				}

				if (unprocessedNewTypes.Contains(dependency))
				{
					return true;
				}
			}

			return false;
		}
	}
}
