using Fluid;
using Fluid.Values;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.Extensions.FileProviders;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snowdrop
{
	public class BlogGenerator : IBlogGenerator
	{
		private const string PostsFolder = "posts";

		private class GenerationContext
		{
			public string IncludePath { get; set; }
			public List<Page> Pages { get; set; }
			public List<RawFile> Templates { get; set; }
		}

		private class ViewSite
		{
			public List<ViewPost> Posts { get; set; }
		}

		private class ViewPage
		{
			public string Title { get; set; }
		}

		private class ViewPost
		{
			public string Title { get; set; }
			public string AbsoluteUrl { get; set; }
		}

		private readonly MarkdownPipeline pipeline;
		private readonly Serializer frontMatterSerializer = new Serializer();

		public BlogGenerator()
		{
			pipeline = new MarkdownPipelineBuilder()
				.UseAdvancedExtensions()
				.UseYamlFrontMatter()
				.Build();
		}

		public OutputBlogContext Generate(InputBlogContext context)
		{
			var pages = new List<Page>();
			pages.AddRange(ProcessPosts(context.Posts));
			pages.AddRange(ProcessPages(context.Pages));

			var generationContext = new GenerationContext { IncludePath = context.IncludePath, Pages = pages, Templates = context.Templates };
			var generatedPages = GenerateHtml(generationContext).ToList();

			return new OutputBlogContext { Pages = generatedPages };
		}

		private IEnumerable<Page> ProcessPosts(IEnumerable<RawFile> files)
		{
			foreach(RawFile file in files)
			{
				var document = Markdown.Parse(file.Content, pipeline);
				var post = new Post();

				foreach(Block block in document)
				{
					if(block is YamlFrontMatterBlock yaml)
					{
						var source = string.Join('\n', yaml.Lines);
						post.Header = frontMatterSerializer.Deserialize<PostHeader>(source);
					}
					break;
				}

				post.RawContent = file.Content;
				var page = new Page { FileName = file.Name, RawContent = null, Post = post };
				yield return page;
			}
		}

		private IEnumerable<Page> ProcessPages(IEnumerable<RawFile> files)
		{
			foreach(RawFile file in files)
			{
				var page = new Page { FileName = file.Name, RawContent = file.Content, Post = null };
				yield return page;
			}
		}

		private string GetRelativeUrl(string absoluteUrl, string currentUrl)
		{
			string absolute = absoluteUrl.Replace('\\', '/');
			string current = currentUrl.Replace('\\', '/');
			
			var absoluteParts = absolute.Split('/');
			var currentParts = current.Split('/');
			var builder = new StringBuilder();
			int a = 0;
			int c = 0;
			int count = Math.Min(absoluteParts.Length, currentParts.Length);
	
			// Ignore matching left sides.
			for(; a < count; ++a)
			{
				if(absoluteParts[a].Equals(currentParts[c], StringComparison.InvariantCultureIgnoreCase))
				{
					++c;
				}
				else
				{
					break;
				}
			}

			// Go back to the mismatch location, skip the last part because it's a page file and not a folder.
			for(; c < currentParts.Length - 1; ++c)
			{
				builder.Append("../");
			}

			for(; a < absoluteParts.Length; ++a)
			{
				builder.Append(absoluteParts[a]);
				if(a < absoluteParts.Length - 1)
				{
					builder.Append('/');
				}
			}

			return builder.ToString();
		}

		private string PathToUrl(string path)
		{
			return path.Replace('\\', '/');
		}

		private string GetEndPath(Page page)
		{
			return Path.ChangeExtension(page.Post == null ? page.FileName : Path.Combine(PostsFolder, page.FileName), ".html");
		}

		private IEnumerable<GeneratedPage> GenerateHtml(GenerationContext context)
		{
			var viewSite = new ViewSite
			{
				Posts = context.Pages.Where(p => p.Post != null).Select(p => new ViewPost { Title = p.Post.Header.Title, AbsoluteUrl = PathToUrl(GetEndPath(p)) }).ToList()
			};

			foreach(Page page in context.Pages)
			{
				var generated = new GeneratedPage();
				generated.RelativePath = GetEndPath(page);
				var templateContext = new TemplateContext();
				templateContext.FileProvider = new PhysicalFileProvider(context.IncludePath);
				templateContext.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
				templateContext.MemberAccessStrategy.Register<ViewPage>();
				templateContext.MemberAccessStrategy.Register<ViewPost>();
				templateContext.MemberAccessStrategy.Register<ViewSite>();
				templateContext.Filters.AddFilter("relative_url", (input, arguments, context) => new StringValue(GetRelativeUrl(input.ToStringValue(), PathToUrl(generated.RelativePath))));

				string templateContent = null;
				
				var viewPage = new ViewPage();
				string content = null;

				if(page.Post != null)
				{
					string layout = null;
					if(page.Post.Header != null)
					{
						viewPage.Title = page.Post.Header.Title;
						layout = page.Post.Header.Layout;
					}
					viewPage.Title = viewPage.Title ?? "Untitled";
					content = Markdown.ToHtml(page.Post.RawContent, pipeline);

					var htmlTemplate = context.Templates.Find(f => Path.GetFileNameWithoutExtension(f.Name).Equals(layout, StringComparison.InvariantCulture));
					if(htmlTemplate == null)
					{
						Console.WriteLine($"Couldn't find template {layout}");
						// Dump the markdown itself if template doesn't exist.
						templateContent = content;
					}
					else
					{
						templateContent = htmlTemplate.Content;
					}
				}
				else
				{
					templateContent = page.RawContent;
				}

				templateContext.SetValue("site", viewSite);
				templateContext.SetValue("page", viewPage);
				templateContext.SetValue("content", content);
				if(FluidTemplate.TryParse(templateContent, out FluidTemplate template))
				{
					generated.Content = template.Render(templateContext, System.Text.Encodings.Web.HtmlEncoder.Default);
				}
				else
				{
					Console.WriteLine($"Invalid Liquid in {page.FileName}");
					generated.Content = page.RawContent;
				}

				yield return generated;
			}
		}

		private string GetValueOrDefault(Dictionary<string, object> values, string key)
		{
			if(values == null)
			{
				return null;
			}

			if(values.TryGetValue(key, out object value) && value is string str)
			{
				return str;
			}

			return null;
		}
	}
}
