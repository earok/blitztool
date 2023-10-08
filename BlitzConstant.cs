using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzTool
{
	public class BlitzConstant
	{
		public string Source;
		public bool IsCSharp;

		public string Name;
		public string Value;

		public BlitzConstant(string source)
		{
			Source = source;
			IsCSharp = source.ToLower().Contains(";cs");

			Name = source.Split("=")[0].Substring(1).Trim();
			Value = source.Split("=")[1].Split(";")[0].Trim();
		}
	}
}
