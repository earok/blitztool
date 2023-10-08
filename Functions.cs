using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public struct FieldData
	{
		public bool isPointer;
		public string fieldName;
		public string fieldType;
		public int arrayLength;
	}

	public static class Functions
	{
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
			return v == '(' || v == ')' || v == '{' || v == '}' || v == ' ' || v == ',' || v == ';';
		}

		internal static FieldData GetFieldData(string field, Dictionary<string, BlitzConstant> blitzConstants)
		{
			var arrayLength = 1;
			var isPointer = field.StartsWith("*");
			var type = field.Split(".")[1].Split(";")[0].Trim();
			var name = field.Split(".")[0].Replace("*", "");

			if (type.Contains("["))
			{
				var arrayLengthString = type.Split("[")[1].Split("]")[0];

				//This is a constant, so revert to our actual list of constants
				if (arrayLengthString.StartsWith("#"))
				{
					arrayLengthString = blitzConstants[arrayLengthString.Substring(1)].Value;
				}

				arrayLength = int.Parse(arrayLengthString);
				type = type.Split("[")[0];
			}

			return new FieldData()
			{
				arrayLength = arrayLength,
				fieldName = name,
				fieldType = type,
				isPointer = isPointer
			};
		}

		internal static int GetFieldLength(string field, Dictionary<string, BlitzNewType> newTypesDictionary, Dictionary<string, BlitzConstant> blitzConstants)
		{
			var fieldData = GetFieldData(field, blitzConstants);

			if (fieldData.isPointer)
			{
				return fieldData.arrayLength * 4;
			}

			//Basic type
			switch (fieldData.fieldType)
			{
				case "b":
					return 1 * fieldData.arrayLength;
				case "w":
					return 2 * fieldData.arrayLength;
				case "l":
					return 4 * fieldData.arrayLength;
				case "q":
					return 4 * fieldData.arrayLength;
				case "s":
					return 4 * fieldData.arrayLength;
				case "$":
					return 4 * fieldData.arrayLength;
			}

			if (newTypesDictionary.ContainsKey(fieldData.fieldType.ToLower()))
			{
				var baseType = newTypesDictionary[fieldData.fieldType.ToLower()];
				var size = 0;
				foreach (var baseField in baseType.Fields)
				{
					size += GetFieldLength(baseField, newTypesDictionary, blitzConstants);
				}
				return size * fieldData.arrayLength;
			}

			throw new Exception("unknown type ." + fieldData.fieldType);
		}

		internal static string ProcessMacroLine(string line, List<string> parameters, int useCount)
		{
			var outline = line.Replace("@@@", useCount.ToString("D3"));

			for (var i = 0; i < parameters.Count; i++)
			{
				outline = outline.Replace("`" + (i + 1), parameters[i]);
			}

			return outline;
		}
	}
}
