using UpdateWithExamples;

namespace UnitTests
{
	/// <summary>
	/// This has an UpdateWith method with an update argument that can't be mapped to any constructor argument and so is faulty
	/// </summary>
	public class FaultyNamedType
	{
		public FaultyNamedType(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public FaultyNamedType UpdateWith(OptionalValue<string> title = new OptionalValue<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<FaultyNamedType>()(this, title);
		}
	}
}
