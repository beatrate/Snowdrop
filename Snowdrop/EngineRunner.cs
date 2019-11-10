using CommandLine;
using System;
using System.Collections.Generic;

namespace Snowdrop
{
	public class EngineRunner
	{
		private Engine engine = new Engine();

		public int RunNew(NewOptions options)
		{
			try
			{
				engine.CreateBlog();
				Console.WriteLine("Created");
				return 0;
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
				return 1;
			}
		}

		public int RunGenerate(GenerateOptions options)
		{
			try
			{
				engine.GenerateBlog();
				Console.WriteLine($"Generated");
				return 0;
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
				return 1;
			}
		}

		public int HandleErrors(IEnumerable<Error> errors)
		{
			foreach(Error error in errors)
			{
				Console.WriteLine(error);
			}
			return 1;
		}
	}
}
