using UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// This class' UpdateWith fails because there is no way for it to provide a value for the constructor's id argument
	/// </summary>
	public class FaultyNamedAndPrivateNumberedType
	{
		private readonly int _id;
		public FaultyNamedAndPrivateNumberedType(string name, int id)
		{
			Name = name;
			_id = id;
		}

		public string Name { get; private set; }

		public FaultyNamedAndPrivateNumberedType UpdateWith(OptionalValue<string> name = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<FaultyNamedAndPrivateNumberedType>()(this, name);
		}
	}
}
