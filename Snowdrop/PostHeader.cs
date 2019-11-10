using SharpYaml.Serialization;

namespace Snowdrop
{
	public class PostHeader
	{
		[YamlMember("title")]
		public string Title { get; set; }
		[YamlMember("layout")]
		public string Layout { get; set; }
	}
}
