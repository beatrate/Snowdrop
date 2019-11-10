using Fluid;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snowdrop
{
	public class BlogGenerator : IBlogGenerator
	{
		private const string PostsFolder = "posts";

		private class GenerationContext
		{
			public List<Page> Pages { get; set; }
			public List<RawFile> Templates { get; set; }
		}

		private class ViewPage
		{
			public string Title { get; set; }
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

			var generationContext = new GenerationContext { Pages = pages, Templates = context.Templates };
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
				var page = new Page { FileName = Path.ChangeExtension(file.Name, ".html"), RawContent = null, Post = post };
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

		private IEnumerable<GeneratedPage> GenerateHtml(GenerationContext context)
		{
			foreach(Page page in context.Pages)
			{
				var generated = new GeneratedPage();
				generated.RelativePath = page.Post == null ? page.FileName : Path.Combine(PostsFolder, page.FileName);
				var templateContext = new TemplateContext();
				templateContext.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
				templateContext.MemberAccessStrategy.Register<ViewPage>();
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
