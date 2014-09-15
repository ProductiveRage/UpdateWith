using UpdateWithExamples;

namespace UnitTests
{
	public class NamedType
	{
		public NamedType(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public NamedType UpdateWith(OptionalValue<string> name = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<NamedType>()(this, name);
		}
	}
}
