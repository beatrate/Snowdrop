using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Snowdrop
{
	public class BlogEngine : IBlogEngine
	{
		private const string InternalTemplateFolder = "Templates";
		private const string PostsFolder = "posts";
		private const string TemplatesFolder = "templates";
		private const string SiteFolder = "site";

		private readonly string internalTemplatePath;
		private readonly string basePath;
		private readonly string postsPath;
		private readonly string templatesPath;
		private readonly string sitePath;

		private readonly IBlogGenerator generator;

		public BlogEngine(IBlogGenerator generator)
		{
			internalTemplatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), InternalTemplateFolder);
			basePath = Directory.GetCurrentDirectory();
			postsPath = Path.Combine(basePath, PostsFolder);
			templatesPath = Path.Combine(basePath, TemplatesFolder);
			sitePath = Path.Combine(basePath, SiteFolder);
			this.generator = generator;
		}

		public void InitializeBlog()
		{
			Directory.CreateDirectory(postsPath);
			Directory.CreateDirectory(sitePath);
			Directory.CreateDirectory(templatesPath);
			File.WriteAllText(Path.Combine(basePath, "index.liquid"), File.ReadAllText(Path.Combine(internalTemplatePath, "index.liquid")));
			File.WriteAllText(Path.Combine(basePath, "about.liquid"), File.ReadAllText(Path.Combine(internalTemplatePath, "about.liquid")));
			File.WriteAllText(Path.Combine(postsPath, "hello.md"), File.ReadAllText(Path.Combine(internalTemplatePath, "hello.md")));
			File.WriteAllText(Path.Combine(templatesPath, "post.liquid"), File.ReadAllText(Path.Combine(internalTemplatePath, "post.liquid")));
			File.WriteAllText(Path.Combine(templatesPath, "navbar.liquid"), File.ReadAllText(Path.Combine(internalTemplatePath, "navbar.liquid")));
			File.WriteAllText(Path.Combine(basePath, "style.css"), File.ReadAllText(Path.Combine(internalTemplatePath, "style.css")));

			File.WriteAllText(Path.Combine(basePath, "Dockerfile"), File.ReadAllText(Path.Combine(internalTemplatePath, "Dockerfile")));
		}

		public void GenerateBlog()
		{
			var posts = FindWithExtension(postsPath, ".md").ToList();
			var pages = FindWithExtension(basePath, ".html", ".liquid").ToList();
			var templates = FindWithExtension(templatesPath, ".html", ".liquid").ToList();
			var styles = FindWithExtension(basePath, ".css").ToList();

			var input = new InputBlogContext { IncludePath = templatesPath, Posts = posts, Pages = pages, Templates = templates };
			var output = generator.Generate(input);
			if(Directory.Exists(sitePath))
			{
				Directory.Delete(sitePath, true);
			}
			
			Directory.CreateDirectory(sitePath);

			foreach(GeneratedPage page in output.Pages)
			{
				var path = Path.GetFullPath(page.RelativePath, sitePath);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllText(path, page.Content);
			}

			foreach(RawFile file in styles)
			{
				File.WriteAllText(Path.Combine(sitePath, file.Name), file.Content);
			}
		}

		private IEnumerable<RawFile> FindWithExtension(string path, params string[] extensions)
		{
			return Directory.GetFiles(path)
				.Where(f =>
				{
					string currentExtension = Path.GetExtension(f);
					return extensions.Any(e => currentExtension.Equals(e, StringComparison.InvariantCultureIgnoreCase));
				})
				.Select(f => new RawFile { Path = f, Name = Path.GetFileName(f), Content = File.ReadAllText(f) });
		}
	}
}
