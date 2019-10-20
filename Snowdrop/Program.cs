using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;

namespace Snowdrop
{
	public class Post
	{
		public DateTime Date { get; set; }
		public string Name { get; set; }
		public string Content { get; set; }
	}

	public class Engine
	{
		public void CreateBlog(string name)
		{

		}
	}

	[Verb("new", HelpText = "Create a new blog.")]
	public class NewOptions
	{
		public string Name { get; set; }
	}

	public class EngineRunner
	{
		private Engine engine = new Engine();

		public int RunNew(NewOptions options)
		{
			engine.CreateBlog(options.Name);
			return 0;
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

	internal class Program
	{
		private static int Main(string[] args)
		{
			var runner = new EngineRunner();
			return Parser.Default.ParseArguments<NewOptions>(args)
				.MapResult(
					(NewOptions options) => runner.RunNew(options),
					errors => runner.HandleErrors(errors)
				);
		}
	}
}
