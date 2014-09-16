using UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// This is the simplest form to test against
	/// </summary>
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
