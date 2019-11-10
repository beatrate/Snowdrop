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
	public class Post
	{
		public string Title { get; set; }
		public string RawContent { get; set; }
	}

	public class Page
	{
		public string RawContent { get; set; }
	}

	public interface IPostFilter
	{
		IEnumerable<Post> GetPosts(string path);
	}

	public interface IPageFilter
	{
		IEnumerable<Page> GetPages(string path);
	}

	public class BlogInput
	{
		public List<Post> Posts { get; set; }
		public List<Page> Pages { get; set; }
	}

	public class GeneratedPage
	{

	}

	public interface IPageGenerator
	{
		IEnumerable<GeneratedPage> From(BlogInput input);
	}

	public class Engine
	{
		private const string PostsDirectory = "posts";
		private const string SiteDirectory = "site";

		private readonly MarkdownPipeline pipeline;
		private readonly Serializer frontMatterSerializer = new Serializer();

		public Engine()
		{
			pipeline = new MarkdownPipelineBuilder()
				.UseAdvancedExtensions()
				.UseYamlFrontMatter()
				.Build();
		}

		public void CreateBlog()
		{
			string basePath = Directory.GetCurrentDirectory();
			string postsPath = Path.Combine(basePath, PostsDirectory);

			Directory.CreateDirectory(Path.Combine(basePath, SiteDirectory));
			Directory.CreateDirectory(postsPath);
			File.WriteAllText(Path.Combine(postsPath, "hello.md"), GetExamplePost());
		}

		public void GenerateBlog()
		{
			string basePath = Directory.GetCurrentDirectory();
			string postsPath = Path.Combine(basePath, PostsDirectory);
			string sitePath = Path.Combine(basePath, SiteDirectory);

			if(Directory.Exists(sitePath))
			{
				Directory.Delete(sitePath, true);
			}
			Directory.CreateDirectory(sitePath);

			foreach(string file in Directory.GetFiles(basePath))
			{
				if(Path.GetExtension(file) == ".html")
				{
					string endFile = Path.Combine(sitePath, Path.GetFileNameWithoutExtension(file) + ".html");
					if(FluidTemplate.TryParse(File.ReadAllText(file), out FluidTemplate template))
					{
						var context = new TemplateContext();
						File.WriteAllText(endFile, template.Render(context));
					}
					else
					{
						Console.WriteLine($"{Path.GetFileName(file)} didn't parse");
					}
				}
			}

			foreach(string file in Directory.GetFiles(postsPath).Where(f => Path.GetExtension(f) == ".md"))
			{
				var document = Markdown.Parse(File.ReadAllText(file), pipeline);
				foreach(Block block in document)
				{
					if(block is YamlFrontMatterBlock yaml)
					{
						var values = frontMatterSerializer.Deserialize<Dictionary<string, object>>(string.Join('\n', yaml.Lines));
					}
					break;
				}
			}
		}

		private string GetExamplePost()
		{
			return @"
---
layout: post
title: ""Hello  World!""
---

# Welcome

**Hello world**, this is an example post.".Trim();
		}
	}
}
