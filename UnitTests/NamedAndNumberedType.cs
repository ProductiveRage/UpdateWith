using UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// This is still a very simple type but one for which one property is not available in the UpdateWith method and will have to be satisfied
	/// by the property value on the source reference
	/// </summary>
	public class NamedAndNumberedType
	{
		public NamedAndNumberedType(string name, int id)
		{
			Name = name;
			Id = id;
		}

		public string Name { get; private set; }
		public int Id { get; private set; }

		public NamedAndNumberedType UpdateWith(OptionalValue<string> name = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<NamedAndNumberedType>()(this, name);
		}
	}
}
