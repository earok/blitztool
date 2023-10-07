using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzProject
	{
		public enum StringReadingMode
		{
			NA = 0,
			Speech = 1,
		}

		public BlitzProject(string startingFilePath, string outputFilePath)
		{
			//Step one - extract every line
			var lines = new List<string>();
			Functions.ExtractAllLines(lines, startingFilePath);

			//Step two - extract every macro
			var macros = new Dictionary<string, BlitzMacro>();
			var lineIndex = 0;
			var outLines = new List<string>();
			while (lineIndex < lines.Count)
			{
				if (lines[lineIndex].ToLower().StartsWith("macro "))
				{
					var macroName = lines[lineIndex].Substring(6).Split(";")[0].ToLower();
					var macro = new BlitzMacro();

					lineIndex++; //Skip the macro name
					while (lines[lineIndex].ToLower().StartsWith("end macro") == false)
					{
						macro.AddLine(lines[lineIndex]);
						lineIndex++;
					}

					lineIndex++; //Skip end macro
					macros[macroName] = macro;
				}
				else
				{
					outLines.Add(lines[lineIndex]);
					lineIndex++;
				}
			}

			//We've extracted all of the lines without any of the macros
			lines = outLines;

			//Step three - we need to unmacro until all of the macros (including sub macros) are completely unmacroed
			var unmacroComplete = false;

			while (unmacroComplete == false)
			{
				unmacroComplete = true;
				lineIndex = 0;
				outLines = new List<string>();
				while (lineIndex < lines.Count)
				{
					if (lines[lineIndex].Contains('!') == false)
					{
						//There aren't any possible macros on this line, so just continue
						outLines.Add(lines[lineIndex]);
						lineIndex++;
						continue;
					}

					StringReadingMode stringReadingMode = StringReadingMode.NA;
					var outLine = "";

					var i = 0;
					while (i < lines[lineIndex].Length)
					{
						var c = lines[lineIndex][i];

						switch (stringReadingMode)
						{
							//Could be anything
							case StringReadingMode.NA:

								switch (c)
								{
									case '"':
										stringReadingMode = StringReadingMode.Speech;
										break;

									case '!':

										//This could be a macro, check to see if it is
										var macroName = "";
										var hasParameters = false;

										for (var j = i + 1; j < lines[lineIndex].Length; j++)
										{
											//This macro does have parameters
											if (lines[lineIndex][j] == '{')
											{
												hasParameters = true;
												break;
											}

											//This macro has no parameters
											if (Functions.EndOfName(lines[lineIndex][j]))
											{											
												break;
											}

											macroName += lines[lineIndex][j];
										}

										if (macros.ContainsKey(macroName.ToLower()))
										{
											//Hit! This is a macro to be processed
											//We flag unmacro complete as false as there could be "submacros" still to process
											unmacroComplete = false;

											//Skip the actual macro name
											i += 1 + macroName.Length;

											var parameters = new List<string>();
											if (hasParameters)
											{
												i += 1;
												var thisParameter = "";
												for (var j = i; i < lines[lineIndex].Length; j++)
												{
													i++;
													switch (lines[lineIndex][j])
													{
														case '}':
															if (thisParameter.Length > 0)
															{
																parameters.Add(thisParameter);
															}
															break;

														case ',':
															parameters.Add(thisParameter);
															thisParameter = "";
															break;

														default:
															thisParameter += lines[lineIndex][j];
															break;
													}
												}
											}

											//Is this an line macro?
											var macro = macros[macroName.ToLower()];

											if (macro.lines.Count == 1)
											{
												//Handle inline macros
												outLine += Functions.ProcessMacroLine(macro.lines[0], parameters);
											}
											else
											{
												//Add each of these as a line
												foreach (var line in macro.lines)
												{
													outLines.Add(Functions.ProcessMacroLine(line, parameters));
												}
											}

											continue;
										}

										break;
								}

								outLine += c;
								i++;
								continue;

							case StringReadingMode.Speech:

								if (c == '"')
								{
									stringReadingMode = StringReadingMode.NA;
								}
								outLine += c;
								i++;
								continue;
						}
					}

					outLines.Add(outLine);
					lineIndex++;
				}
				lines = outLines;
			}

			//Export in Amiga friendly format
			using (var writer = new StreamWriter(outputFilePath))
			{
				foreach (var line in lines)
				{
					writer.Write(line);
					writer.Write("\n");
				}
			}
		}
	}
}
