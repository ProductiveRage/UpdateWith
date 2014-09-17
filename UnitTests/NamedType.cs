using ProdutiveRage.UpdateWith;

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

		public NamedType UpdateWith(Optional<string> name = new Optional<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<NamedType>()(this, name);
		}
	}
}
