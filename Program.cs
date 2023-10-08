namespace BlitzTool
{
	internal class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: BlitzTool.exe [inputpath] [outputpath]");
				return;
			}

			new BlitzProject(args[0], args[1]);
		}
	}
}
