using CommandLine;
using System.Collections.Generic;

namespace Snowdrop
{
	public class App
	{
		private readonly IBlogEngine engine;

		public App(IBlogEngine engine)
		{
			this.engine = engine;
		}

		public int Run(string[] arguments)
		{
			return Parser.Default.ParseArguments<NewOptions, GenerateOptions>(arguments)
				.MapResult(
					(NewOptions options) => RunNew(options),
					(GenerateOptions options) => RunGenerate(options),
					errors => HandleErrors(errors)
				);
		}

		private int RunNew(NewOptions options)
		{
			engine.InitializeBlog();
			return 0;
		}

		private int RunGenerate(GenerateOptions options)
		{
			engine.GenerateBlog();
			return 0;
		}

		private int HandleErrors(IEnumerable<Error> errors)
		{
			return 1;
		}
	}
}
