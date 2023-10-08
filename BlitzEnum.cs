using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzEnum
	{
		public string Name;
		public List<string> Constants = new List<string>();

		public BlitzEnum(string v)
		{
			Name = v.Substring("enum ".Length);
		}

		internal void AddConstant(string v)
		{
			Constants.Add(v);
		}

		internal void Process(List<string> finalOutput)
		{
			var i = 0;
			foreach (var c in Constants)
			{
				finalOutput.Add("#" + Name + "_" + c + " = " + i);
				i++;
			}
		}
	}
}
