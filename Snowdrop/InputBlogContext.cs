﻿using System.Collections.Generic;

namespace Snowdrop
{
	public class InputBlogContext
	{
		public string IncludePath { get; set; }
		public List<RawFile> Posts { get; set; }
		public List<RawFile> Pages { get; set; }
		public List<RawFile> Templates { get; set; }
	}
}
