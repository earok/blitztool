using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlitzTool
{
	public class BlitzNewType
	{
		public string Signature;
		public string Name;
		public List<string> Fields = new List<string>();

		public HashSet<string> Dependencies = new HashSet<string>();
		public bool IsASM;
		public bool IsCSharp;

		public BlitzNewType(string signature)
		{
			Signature = signature;

			if (signature.ToLower().Contains(";asm"))
			{
				IsASM = true;
			}
			if (signature.ToLower().Contains(";cs"))
			{
				IsCSharp = true;
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

		internal void ProcessASMOffsets(List<string> finalOutput, string baseName, ref int offset, Dictionary<string, BlitzNewType> newTypes, Dictionary<string, BlitzConstant> blitzConstants)
		{
			foreach (var field in Fields)
			{
				var fieldData = Functions.GetFieldData(field, blitzConstants);
				if (newTypes.ContainsKey(fieldData.fieldType.ToLower()) && fieldData.isPointer == false)
				{
					var subNewType = newTypes[fieldData.fieldType.ToLower()];
					subNewType.ProcessASMOffsets(finalOutput, baseName + "_" + fieldData.fieldName, ref offset, newTypes, blitzConstants);
				}
				else
				{
					finalOutput.Add("#" + baseName + "_" + fieldData.fieldName + " = " + offset);
					offset += Functions.GetFieldLength(field, newTypes, blitzConstants);
				}

			}
		}

		internal void ProcessCSharp(List<string> finalOutput, Dictionary<string, BlitzConstant> blitzConstants, Dictionary<string, BlitzNewType> newTypes)
		{
			finalOutput.Add(string.Format("\tpublic class {0} : BlitzType", Name));
			finalOutput.Add("\t{");

			//Process all fields
			var offset = 0;
			foreach (var field in Fields)
			{
				var fieldData = Functions.GetFieldData(field, blitzConstants);
				var typeName = fieldData.fieldType;
				if (fieldData.isPointer)
				{
					typeName = "l";
				}

				switch (typeName)
				{
					case "$":
					case "s":
					case "l":

						typeName = "BlitzLong";
						break;

					case "w":

						typeName = "BlitzWord";
						break;

					case "b":

						typeName = "BlitzByte";
						break;

					case "q":

						typeName = "BlitzQuick";
						break;
				}

				var typeArray = "";
				var arraySize = "()";

				if (fieldData.arrayLength != 1)
				{
					typeArray = "[]";
					arraySize = "[" + fieldData.arrayLength + "]";
				}

				finalOutput.Add(string.Format("\t\tpublic {0}{1} {2} = new {3}{4};", typeName, typeArray, fieldData.fieldName, typeName, arraySize));
				finalOutput.Add(string.Format("\t\tpublic const int {0}_Offset = {1};", fieldData.fieldName, offset));
				offset += Functions.GetFieldLength(field, newTypes, blitzConstants);
			}

			//Function for converting to byte array
			AppendSerializationOperation("FromByteArray", finalOutput, blitzConstants, newTypes);
			AppendSerializationOperation("ToByteArray", finalOutput, blitzConstants, newTypes);

			finalOutput.Add(string.Format("\t\tpublic const int SizeOf = {0};", offset));
			finalOutput.Add("\t}");
			finalOutput.Add("");
		}

		private void AppendSerializationOperation(string type, List<string> finalOutput, Dictionary<string, BlitzConstant> blitzConstants, Dictionary<string, BlitzNewType> newTypes)
		{
			int offset;
			finalOutput.Add("");
			finalOutput.Add("\t\tpublic void " + type + "(IList<byte> array, int offset)");
			finalOutput.Add("\t\t{");
			offset = 0;

			foreach (var field in Fields)
			{
				var fieldData = Functions.GetFieldData(field, blitzConstants);
				var length = Functions.GetFieldLength(field, newTypes, blitzConstants); ;

				if (fieldData.arrayLength == 1)
				{
					finalOutput.Add(string.Format("\t\t\t{0}.{2}(array,{1}+offset);", fieldData.fieldName, offset, type));
					offset += length;
				}
				else
				{
					var individualSize = 0;
					if (fieldData.arrayLength > 0)
					{
						individualSize = length / fieldData.arrayLength;
					}

					for (var i = 0; i < fieldData.arrayLength; i++)
					{
						finalOutput.Add(string.Format("\t\t\t{0}[{2}].{3}(array,{1}+offset);", fieldData.fieldName, offset, i, type));
						offset += individualSize;
					}
				}
			}
			finalOutput.Add("\t\t}");
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
