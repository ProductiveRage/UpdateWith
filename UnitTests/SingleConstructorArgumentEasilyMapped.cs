using UpdateWithExamples;

namespace UnitTests
{
	public class SingleConstructorArgumentEasilyMapped
	{
		public SingleConstructorArgumentEasilyMapped(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public SingleConstructorArgumentEasilyMapped UpdateWith(OptionalValue<string> name = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<SingleConstructorArgumentEasilyMapped>()(this, name);
		}
	}
}
