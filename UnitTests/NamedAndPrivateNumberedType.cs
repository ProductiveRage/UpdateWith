using UpdateWithExamples;

namespace UnitTests
{
	/// <summary>
	/// This is still a very simple type but one for which one property is not available in either the UpdateWith method or through any public
	/// properties. The only way it can be satisfied is by falling back to the constructor argument's default value.
	/// by the property value on the source reference
	/// </summary>
	public class NamedAndPrivateNumberedType
	{
		private readonly int _id;
		public NamedAndPrivateNumberedType(string name, int id = -1)
		{
			Name = name;
			_id = id;
		}

		public string Name { get; private set; }

		public NamedAndPrivateNumberedType UpdateWith(OptionalValue<string> name = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<NamedAndPrivateNumberedType>()(this, name);
		}
	}
}
