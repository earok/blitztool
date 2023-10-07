using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzMacro
	{
		public List<string> lines = new List<string>();

		internal void AddLine(string line)
		{
			lines.Add(line);
		}
	}
}
