using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzGlobal
	{
		public string Value;

		public BlitzGlobal(string v)
		{
			Value = v.Substring("global ".Length);
		}
	}
}
