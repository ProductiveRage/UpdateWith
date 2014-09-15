using System;
using System.Reflection;
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

		/// <summary>
		/// There is no update argument on NamedAndNumberedType for id, so the value must be maintained by taking it from the source reference
		/// </summary>
		[Fact]
		public void PropertyValueFallbackDueToUnavailableUpdateArgument()
		{
			var currentInstance = new NamedAndNumberedType("test", 1);
			var newInstance = currentInstance.UpdateWith(name: "test-new");
			Assert.Equal("test-new", newInstance.Name);
			Assert.Equal(1, newInstance.Id);
		}

		/// <summary>
		/// There is no update argument on NamedAndNumberedType for id, so the value must be maintained by taking it from the source reference
		/// </summary>
		[Fact]
		public void ConstructorArgumentDefaultValueFallbackDueToUnavailableUpdateArgument()
		{
			var currentInstance = new NamedAndPrivateNumberedType("test", 1);
			var newInstance = currentInstance.UpdateWith(name: "test-new");
			Assert.Equal("test-new", newInstance.Name);
			var privateIdField = newInstance.GetType().GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.Equal(-1, privateIdField.GetValue(newInstance));
		}

		[Fact]
		public void NullValuesMayBeSpecifiedAndUpdateWithExtensionMethodsAreSupported()
		{
			var currentInstance = new RoleDetails("test", new DateTime(2014, 9, 15), new DateTime(2014, 10, 16));
			var newInstance = currentInstance.UpdateWith(title: "test-new", endDateIfAny: null);
			Assert.Equal(null, newInstance.EndDateIfAny);
		}

		[Fact]
		public void UnmappableUpdateArgumentsResultInAnArgumentException()
		{
			var currentInstance = new FaultyNamedType("test");
			Assert.Throws<ArgumentException>(() =>
			{
				Console.WriteLine(currentInstance.UpdateWith(title: "test-new").Name);
			});
		}

		[Fact]
		public void UnmatchedNumberOfUpdateArgumentsPassedInsideTheUpdateWithMethodResultInAnArgumentException()
		{
			var currentInstance = new FaultyNamedAndNumberedType("test", 1);
			Assert.Throws<ArgumentException>(() =>
			{
				Console.WriteLine(currentInstance.UpdateWith(name: "test-new").Name);
			});
		}

		[Fact]
		public void UnmatchedConstructorArgumentsResultInAnArgumentException()
		{
			var currentInstance = new FaultyNamedAndPrivateNumberedType("test", 1);
			Assert.Throws<ArgumentException>(() =>
			{
				Console.WriteLine(currentInstance.UpdateWith(name: "test-new").Name);
			});
		}
	}
}
