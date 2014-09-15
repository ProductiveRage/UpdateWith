using Xunit;

namespace UnitTests
{
	public class DefaultUpdateWithHelperTests
	{
		[Fact]
		public void SingleConstructorArgumentMappedToSingleUpdateArgument()
		{
			var currentInstance = new NamedType("test");
			var newInstance = currentInstance.UpdateWith(name: "test-new");
			Assert.Equal("test-new", newInstance.Name);
		}

		[Fact]
		public void SingleConstructorArgumentMappedToSingleUpdateArgument_SameData()
		{
			var currentInstance = new NamedType("test");
			var newInstance = currentInstance.UpdateWith(name: "test");
			Assert.Equal(currentInstance, newInstance);
		}

		[Fact]
		public void SingleConstructorArgumentMappedToSingleUpdateArgument_NoUpdateArguments()
		{
			var currentInstance = new NamedType("test");
			var newInstance = currentInstance.UpdateWith();
			Assert.Equal(currentInstance, newInstance);
		}
	}
}
