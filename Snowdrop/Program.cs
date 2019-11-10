using Microsoft.Extensions.DependencyInjection;

namespace Snowdrop
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			var serviceCollection = new ServiceCollection();
			ConfigureServices(serviceCollection);
			var serviceProvider = serviceCollection.BuildServiceProvider();
			var app = serviceProvider.GetService<App>();
			return app.Run(args);
		}

		private static void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection.AddTransient<IBlogGenerator, BlogGenerator>();
			serviceCollection.AddTransient<IBlogEngine, BlogEngine>();
			serviceCollection.AddTransient<App>();
		}
	}
}
