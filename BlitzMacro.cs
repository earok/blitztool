﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzMacro
	{
		public List<string> lines = new List<string>();
		public int UseCount = 0;

		internal void AddLine(string line)
		{
			lines.Add(line);
		}
	}
}
