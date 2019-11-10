using CommandLine;
using System.IO;

namespace Snowdrop
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			var runner = new EngineRunner();
			return Parser.Default.ParseArguments<NewOptions, GenerateOptions>(args)
				.MapResult(
					(NewOptions options) => runner.RunNew(options),
					(GenerateOptions options) => runner.RunGenerate(options),
					errors => runner.HandleErrors(errors)
				);
		}
	}
}
