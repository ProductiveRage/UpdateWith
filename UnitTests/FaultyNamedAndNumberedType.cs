using ProdutiveRage.UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// The UpdateWith method is faulty on this class since the method itself only has a single update argument but it passes two arguments to
	/// the GetGenerator result
	/// </summary>
	public class FaultyNamedAndNumberedType
	{
		public FaultyNamedAndNumberedType(string name, int id)
		{
			Name = name;
			Id = id;
		}

		public string Name { get; private set; }
		public int Id { get; private set; }

		public FaultyNamedAndNumberedType UpdateWith(Optional<string> name = new Optional<string>())
		{
			return DefaultUpdateWithHelper.GetGenerator<FaultyNamedAndNumberedType>()(this, name, 12);
		}
	}
}
