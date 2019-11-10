namespace Snowdrop
{
	public interface IBlogGenerator
	{
		OutputBlogContext Generate(InputBlogContext context);
	}
}
