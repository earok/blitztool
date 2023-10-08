using System;
using System.Collections.Generic;
using System.Globalization;
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
			var macros = new Dictionary<string, BlitzMacro>();
			var statements = new Dictionary<string, BlitzStatement>();
			var constants = new List<BlitzConstant>();
			var globals = new List<BlitzGlobal>();
			var newTypes = new Dictionary<string, BlitzNewType>();
			var blitzEnums = new Dictionary<string, BlitzEnum>();
			var defTypes = new HashSet<string>();
			var arrayDims = new List<string>();

			var i = 0;

			//Step one - extract every line
			var lines = new List<string>();
			Functions.ExtractAllLines(lines, startingFilePath);

			//Step two - extract every macro
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
					var reachedComment = false;

					i = 0;
					while (i < lines[lineIndex].Length)
					{
						var c = lines[lineIndex][i];

						switch (stringReadingMode)
						{
							//Could be anything
							case StringReadingMode.NA:

								switch (c)
								{
									case ';':
										reachedComment = true;
										break;

									case '"':
										stringReadingMode = StringReadingMode.Speech;
										break;

									case '!':

										//Ignore macros after comments
										if (reachedComment)
										{
											break;
										}

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
												var bracketcount = 0;
												var isFinishedMacroParameters = false;
												for (var j = i; i < lines[lineIndex].Length; j++)
												{
													i++;
													switch (lines[lineIndex][j])
													{
														case '{':
															//Sub macro/statement etc
															bracketcount++;
															thisParameter += lines[lineIndex][j];
															break;

														case '}':
															if (bracketcount > 0)
															{
																bracketcount--;
																thisParameter += lines[lineIndex][j];
																continue;
															}

															if (thisParameter.Length > 0)
															{
																parameters.Add(thisParameter);
															}
															isFinishedMacroParameters = true;
															break;

														case ',':

															//This is a comma inside a submacro/subfunction etc
															if (bracketcount > 0)
															{
																thisParameter += lines[lineIndex][j];
																continue;
															}
															parameters.Add(thisParameter);
															thisParameter = "";
															break;

														default:
															thisParameter += lines[lineIndex][j];
															break;
													}

													if (isFinishedMacroParameters)
													{
														break;
													}
												}
											}

											//Is this an line macro?
											var macro = macros[macroName.ToLower()];


											if (macro.lines.Count == 1)
											{
												//Handle inline macros
												outLine += Functions.ProcessMacroLine(macro.lines[0], parameters, macro.UseCount);
											}
											else
											{
												//Add each of these as a line
												foreach (var line in macro.lines)
												{
													outLines.Add(Functions.ProcessMacroLine(line, parameters, macro.UseCount));
												}
											}

											macro.UseCount++;

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

			lines = outLines;


			//Step four - with our unmacroed lines, split into sections
			outLines = new List<string>();
			i = 0;
			var isBlitzMode = false;
			while (i < lines.Count)
			{
				var lowerLine = lines[i].ToLower().Trim();

				if (lowerLine == "blitz")
				{
					isBlitzMode = true;
					outLines.Add(lines[i]);
				}
				else if (lowerLine == "qamiga" || lowerLine == "amiga")
				{
					isBlitzMode = false;
					outLines.Add(lines[i]);
				}
				else if (lowerLine.StartsWith("dim "))
				{
					arrayDims.Add(lines[i]);

					//Add this array anyway, we still want to declare it at the right place
					outLines.Add(lines[i]);
				}
				else if (lowerLine.StartsWith("deftype ."))
				{
					defTypes.Add(lines[i]);
				}
				else if (lowerLine.StartsWith("#"))
				{
					//This is a constant
					constants.Add(new BlitzConstant(lines[i]));
				}
				else if (lowerLine.StartsWith("global "))
				{
					globals.Add(new BlitzGlobal(lines[i]));
				}
				else if (lowerLine.StartsWith("enum "))
				{

					var blitzenum = new BlitzEnum(lines[i]);
					blitzEnums.Add(blitzenum.Name.ToLower(), blitzenum);
					i++; //Skip the name of the newtype

					while (i < lines.Count)
					{
						lowerLine = lines[i].ToLower();
						if (lowerLine.StartsWith("end enum"))
						{
							break;
						}
						blitzenum.AddConstant(lines[i]);
						i++;
					}
				}
				else if (lowerLine.StartsWith("newtype "))
				{

					var newtype = new BlitzNewType(lines[i]);
					newTypes.Add(newtype.Name.ToLower(), newtype);
					i++; //Skip the name of the newtype

					while (i < lines.Count)
					{
						lowerLine = lines[i].ToLower();
						if (lowerLine.StartsWith("end newtype"))
						{
							break;
						}
						newtype.AddField(lines[i]);
						i++;
					}
				}
				else if (lowerLine.StartsWith("statement ") || lowerLine.StartsWith("function ") || lowerLine.StartsWith("function."))
				{
					var statement = new BlitzStatement(lines[i], isBlitzMode);
					statements.Add(statement.Name.ToLower(), statement);
					i++; //Skip the name of the statement

					while (i < lines.Count)
					{
						lowerLine = lines[i].ToLower();
						if (lowerLine.StartsWith("end function") || lowerLine.StartsWith("end statement"))
						{
							break;
						}
						statement.AddLine(lines[i]);
						i++;
					}
				}
				else
				{
					//This is a regular line of code, just leave it as it is without the comment
					outLines.Add(lines[i].Split(";")[0]);
				}

				i++;
			}

			var finalOutput = new List<string>
			{
				"goto endofheader"
			};

			//Write the enums section
			if (constants.Count > 0)
			{
				finalOutput.Add("");
				finalOutput.Add(";ENUMS SECTION");
				foreach (var blitzEnum in blitzEnums.Values)
				{
					blitzEnum.Process(finalOutput);
				}
			}

			//Write the constants section
			if (constants.Count > 0)
			{
				finalOutput.Add(";CONSTANTS SECTION");
				foreach (var constant in constants)
				{
					finalOutput.Add(constant.Source);
				}
			}

			//Write the newtypes section
			if (newTypes.Count > 0)
			{
				finalOutput.Add(";NEWTYPES SECTION");

				var newTypesToAdd = new List<BlitzNewType>(newTypes.Values);
				var unprocessedNewTypes = new HashSet<string>();

				foreach (var newType in newTypes.Values)
				{
					unprocessedNewTypes.Add(newType.Name.ToLower());
				}

				while (newTypesToAdd.Count > 0)
				{
					//Add each statement, checking that there are no dependencies that haven't been added yet
					for (var j = 0; j < newTypesToAdd.Count; j++)
					{
						if (newTypesToAdd[j].UnprocessedDependencies(unprocessedNewTypes))
						{
							continue;
						}
						unprocessedNewTypes.Remove(newTypesToAdd[j].Name.ToLower());
						newTypesToAdd[j].Process(finalOutput);
						newTypesToAdd.RemoveAt(j);
						break;
					}
				}

			}

			//Write the newtype assembly support
			var assemblyNewTypes = newTypes.Values.Where(p => p.IsASM).ToList();
			if (assemblyNewTypes.Count > 0)
			{
				finalOutput.Add(";NEWTYPE ASM SUPPORT");
				foreach (var newType in assemblyNewTypes)
				{
					var offset = 0;
					newType.ProcessASMOffsets(finalOutput, newType.Name, ref offset, newTypes);
				}
			}

			if (arrayDims.Count > 0)
			{
				finalOutput.Add(";ARRAY SECTION");
				foreach (var arrayDim in arrayDims)
				{
					finalOutput.Add(arrayDim);//.Split("(")[0] + "(0)");
				}
			}

			if (defTypes.Count > 0)
			{
				finalOutput.Add(";DEFTYPES SECTION");
				foreach (var defType in defTypes)
				{
					finalOutput.Add(defType);
				}
			}

			//Write the statements/functions section
			isBlitzMode = false;
			if (statements.Count > 0)
			{
				finalOutput.Add(";STATEMENTS SECTION");

				var statementsToAdd = new List<BlitzStatement>(statements.Values);
				var unprocessedStatements = new HashSet<string>();

				foreach (var statement in statements.Values)
				{
					unprocessedStatements.Add(statement.Name.ToLower());
				}

				while (statementsToAdd.Count > 0)
				{
					//Add each statement, checking that there are no dependencies that haven't been added yet
					for (var j = 0; j < statementsToAdd.Count; j++)
					{
						if (statementsToAdd[j].UnprocessedDependencies(unprocessedStatements))
						{
							continue;
						}
						unprocessedStatements.Remove(statementsToAdd[j].Name.ToLower());

						//Switch in and out of blitz mode depending on what the function needs
						if (statementsToAdd[j].IsBlitzMode != isBlitzMode)
						{
							isBlitzMode = statementsToAdd[j].IsBlitzMode;
							if (isBlitzMode)
							{
								finalOutput.Add("BLITZ");
							}
							else
							{
								finalOutput.Add("QAMIGA");
							}
						}

						statementsToAdd[j].Process(finalOutput, globals);
						statementsToAdd.RemoveAt(j);
						break;
					}
				}

			}

			finalOutput.Add("AMIGA");
			finalOutput.Add(".endofheader");

			finalOutput.AddRange(outLines);



			//Export in Amiga friendly format
			using (var writer = new StreamWriter(outputFilePath))
			{
				foreach (var line in finalOutput)
				{
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					writer.Write(line); //Strips all comments
					writer.Write("\n");
				}
			}
		}
	}
}
