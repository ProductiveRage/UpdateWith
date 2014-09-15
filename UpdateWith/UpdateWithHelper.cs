using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UpdateWithExamples
{
	public class UpdateWithHelper
	{
		private readonly UpdateArgumentToPropertyComparison _updateArgumentToPropertyComparison;
		private readonly UpdateArgumentToConstructorArgumentComparison _updateArgumentToConstructorArgumentComparison;
		private readonly PropertyToConstructorArgumentComparison _propertyToConstructorArgumentComparison;
		private readonly ICacheGenerators _cache;
		private readonly Action<string> _ambiguityLogger;
		public UpdateWithHelper(
			UpdateArgumentToPropertyComparison updateArgumentToPropertyComparison,
			UpdateArgumentToConstructorArgumentComparison updateArgumentToConstructorArgumentComparison,
			PropertyToConstructorArgumentComparison propertyToConstructorArgumentComparison,
			ICacheGenerators cache,
			Action<string> ambiguityLogger)
		{
			if (updateArgumentToPropertyComparison == null)
				throw new ArgumentNullException("updateArgumentToPropertyComparison");
			if (updateArgumentToConstructorArgumentComparison == null)
				throw new ArgumentNullException("updateArgumentToConstructorArgumentComparison");
			if (propertyToConstructorArgumentComparison == null)
				throw new ArgumentNullException("propertyToConstructorArgumentComparison");
			if (cache == null)
				throw new ArgumentNullException("cache");
			if (ambiguityLogger == null)
				throw new ArgumentNullException("ambiguityLogger");

			_updateArgumentToPropertyComparison = updateArgumentToPropertyComparison;
			_updateArgumentToConstructorArgumentComparison = updateArgumentToConstructorArgumentComparison;
			_propertyToConstructorArgumentComparison = propertyToConstructorArgumentComparison;
			_cache = cache;
			_ambiguityLogger = ambiguityLogger;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public UpdateWithSignature<T> GetGenerator<T>(int numberOfFramesFromCallSite = 1)
		{
			if (numberOfFramesFromCallSite < 1)
				throw new ArgumentOutOfRangeException("numberOfFramesFromCallSite");

			// The numberOfFramesFromCallSite is required in case the call to this method has been nested - the basic approach would be to call it directly from the "Update"
			// method, in which numberOfFramesFromCallSite would be one. In the case of the static DefaultUpdateWithHelper class, if that class' method is called directly
			// then the numberOfFramesFromCallSite would be two since there is an extra level of indirection.
			// - Note that this method and any between the update method and this point must specify the "MethodImplOptions.NoInlining" value on the "MethodImpl" attribute
			//   to ensure that the stack is not flattened by method inlining, since this could invalidate the numberOfFramesFromCallSite value and everything would could
			//   tumbling down. Theoretically, the calling update method should also specify this attribute. Realistically, this attribute is probably not strictly required
			//   here since, according to http://msdn.microsoft.com/en-us/library/ms973858.aspx, "Methods that are greater than 32 bytes of IL will not be inlined" nor are
			//   they in the update methods since those methods have to use the OptionalValue generic struct for the update arguments and "If any of the method's formal
			//   arguments are structs, the method will not be inlined". Possibly the most compelling reason (aside from strict correctness and the note in the article
			//   that "I would carefully consider explicitly coding for these heuristics because they might change in future versions of the JIT") to use this is in the
			//   DefaultUpdateWithHelper's pass through method since it's short, has no logic and doesn't take struct arguments.
			var updateMethod = new StackFrame(skipFrames: numberOfFramesFromCallSite, fNeedFileInfo: false).GetMethod();

			// If this is an instance method then the arguments should all be update values. For an extension method, the first argument should be a source reference and
			// the subsequent arguments be the update values. There could potentially be a static method on the class, similar in form to the instance method (it would
			// need a "source" reference from somewhere, but it shouldn't be one of the method arguments so this case would be treated like an instance update method).
			var updateMethodArguments = updateMethod.GetParameters();
			if (updateMethodArguments.Length < 1)
				throw new ArgumentException("The update method must have at least one argument since there must be at least one property specified to update");
			Type declaringType;
			IEnumerable<ParameterInfo> updateArguments;
			if (IsUpdateArgumentAnOptionalValueType(updateMethodArguments[0].ParameterType))
			{
				declaringType = updateMethod.DeclaringType;
				if (!typeof(T).IsAssignableFrom(declaringType))
					throw new ArgumentException("When the first update method argument is not used to specify the source reference, the method's DeclaringType is used and it must be assignable to T, which it is not here");
				updateArguments = updateMethodArguments;
			}
			else
			{
				if (updateMethodArguments.Length < 2)
					throw new ArgumentException("Where the first update method is a source reference, there must be at least two arguments; the source reference and at least one property to update");
				declaringType = updateMethodArguments.First().ParameterType;
				updateArguments = updateMethodArguments.Skip(1);
			}

			var cachedGenerator = _cache.GetIfAvailable<T>(declaringType, updateArguments);
			if (cachedGenerator != null)
				return cachedGenerator;

			var sourceParameter = Expression.Parameter(typeof(T), "source");
			var argsParameter = Expression.Parameter(typeof(object[]), "args");
			var sourceType = typeof(T);
			var updateArgumentValueDetails = updateArguments
				.Select((p, i) => new { Argument = p, Index = i })
				.Select(argumentWithIndex =>
				{
					var argument = argumentWithIndex.Argument;
					var optionalValueInnerType = GetUpdateArgumentOptionalValueInnerType(argument);
					var sourcePropertyOptions = sourceType.GetProperties().Where(p =>
						!p.GetIndexParameters().Any() &&
						optionalValueInnerType.IsAssignableFrom(p.PropertyType) &&
						_updateArgumentToPropertyComparison(argument, p)
					);
					if (!sourcePropertyOptions.Any())
						throw new ArgumentException("Unable to map argument onto an assignabled property on the source type: \"" + argument.Name + "\"");
					var sourceProperty = sourcePropertyOptions.First();
					if (sourcePropertyOptions.Count() > 1)
					{
						_ambiguityLogger(string.Format(
							"Multiple property matches were found for update argument \"{0}\", \"{1}\" is the first matched identified and so is being used",
							argument.Name,
							sourceProperty.Name
						));
					}
					var argArrayElement = Expression.ArrayAccess(argsParameter, Expression.Constant(argumentWithIndex.Index));
					var indicatesChangeFromValueMethod = argument.ParameterType.GetMethod("IndicatesChangeFromValue");
					var getValueMethod = argument.ParameterType.GetMethod("GetValue");
					return new
					{
						Argument = argument,
						InnerType = optionalValueInnerType,
						IsChangeIndicatedRetriever = GetOptionalValueMethodCallThatTakesFallBackPropertyExpression(indicatesChangeFromValueMethod, sourceParameter, argument, argArrayElement, sourceProperty),
						ValueRetriever = GetOptionalValueMethodCallThatTakesFallBackPropertyExpression(getValueMethod, sourceParameter, argument, argArrayElement, sourceProperty)
					};
				})
				.ToArray();

			var constructorOptions = sourceType.GetConstructors()
				.Select(constructor =>
				{
					// Only consider constructors where every updateArgument can be used
					var constructorArguments = constructor.GetParameters();
					var atLeastOneUpdateArgumentNotPossibleToUse = updateArgumentValueDetails.Any(
						updateArgument => !constructorArguments.Any(constructorArgument =>
							_updateArgumentToConstructorArgumentComparison(updateArgument.Argument, constructor, constructorArgument)
						)
					);
					if (atLeastOneUpdateArgumentNotPossibleToUse)
						return null;

					// Try to find a way to satisfy all of the constructor arguments
					var constructorArgumentValueRetrievers = new List<Expression>();
					var constructorArgumentValueIndicatesChangeRetrievers = new List<Expression>();
					var numberOfArgumentsSatisfedByUpdateValues = 0;
					var numberOfArgumentsSatisfedByPropertyValuesPassedBackIn = 0;
					var numberOfArgumentsSatisfedByDefaultConstructorValues = 0;
					foreach (var constructorArgument in constructorArguments)
					{
						// Ideally, use an update argument
						var correspondingUpdateArguments = updateArgumentValueDetails.Where(updateArgument =>
							constructorArgument.ParameterType.IsAssignableFrom(updateArgument.InnerType) &&
							_updateArgumentToConstructorArgumentComparison(updateArgument.Argument, constructor, constructorArgument)
						);
						if (correspondingUpdateArguments.Any())
						{
							var correspondingUpdateArgument = correspondingUpdateArguments.First();
							if (correspondingUpdateArguments.Count() > 1)
							{
								_ambiguityLogger(string.Format(
									"Multiple update property matches were found for constructor argument \"{0}\", \"{1}\" is the first matched identified and so is being used",
									constructorArgument.Name,
									correspondingUpdateArgument.Argument.Name
								));
							}
							constructorArgumentValueRetrievers.Add(correspondingUpdateArgument.ValueRetriever);
							constructorArgumentValueIndicatesChangeRetrievers.Add(correspondingUpdateArgument.IsChangeIndicatedRetriever);
							numberOfArgumentsSatisfedByUpdateValues++;
							continue;
						}

						// If none of the update arguments match, then try for a property on the source type that matches the name and type
						var sourcePropertyOptions = sourceType.GetProperties().Where(p =>
							!p.GetIndexParameters().Any() &&
							constructorArgument.ParameterType.IsAssignableFrom(p.PropertyType) &&
							_propertyToConstructorArgumentComparison(p, constructor, constructorArgument)
						);
						if (sourcePropertyOptions.Any())
						{
							var sourceProperty = sourcePropertyOptions.First();
							if (sourcePropertyOptions.Count() > 1)
							{
								_ambiguityLogger(string.Format(
									"Multiple property matches were found for constructor argument \"{0}\" that couldn't be populated with an update argument, \"{1}\" is the first matched identified and so is being used",
									constructorArgument.Name,
									sourceProperty.Name
								));
							}
							constructorArgumentValueRetrievers.Add(
								Expression.Property(sourceParameter, sourceProperty)
							);
							numberOfArgumentsSatisfedByPropertyValuesPassedBackIn++;
							continue;
						}

						// If these approaches can't provide a match then allow for a default value on the constructor argument, if there is one
						if (constructorArgument.HasDefaultValue)
						{
							constructorArgumentValueRetrievers.Add(
								Expression.Constant(constructorArgument.DefaultValue)
							);
							numberOfArgumentsSatisfedByDefaultConstructorValues++;
							continue;
						}

						return null;
					}

					return new {
						Constructor = constructor,
						ArgumentValueRetrievers = constructorArgumentValueRetrievers,
						ArgumentValueIndicatesChangeRetrievers = constructorArgumentValueIndicatesChangeRetrievers,
						NumberOfArgumentsSatisfedByUpdateValues = numberOfArgumentsSatisfedByUpdateValues,
						NumberOfArgumentsSatisfedByPropertyValuesPassedBackIn = numberOfArgumentsSatisfedByPropertyValuesPassedBackIn,
						NumberOfArgumentsSatisfedByDefaultConstructorValues = numberOfArgumentsSatisfedByDefaultConstructorValues
					};
				})
				.Where(constructorOption => constructorOption != null);
			if (!constructorOptions.Any())
				throw new ArgumentException("Unable to identify any constructors that whose arguments may be satisfied and that use all of the update arguments");

			// All of the eligible constructors will have had at least one argument that matched a name of an update argument, there could feasibly be
			// multiple arguments populated by one or more of the update arguments (it's also feasible that one or more of the update arguments does
			// not get used if there is ambiguous name matching). So the best default approach seems to be (for cases where there are multiple options)
			// to take the constructor that used the update arguments the most times. Then (if there's a tie) the one that uses the most property
			// values fed back into it should be favoured, based on the assumption that if a two constructors are equivalent but one may have an
			// argument populated by the source object's current state, then this is maintaining state and the other argument is relying on defaults.
			// I can't think of a justification either way for using the NumberOfArgumentsSatisfedByDefaultConstructorValues (on the one hand, relying
			// on defaults means state may be getting lost and default may be better avoided - but on the other hand, where is the data coming from if
			// it's not from an update value or an existing property, it must be defaulting one way or another).
			var bestConstructor = constructorOptions
				.OrderByDescending(c => c.NumberOfArgumentsSatisfedByUpdateValues)
				.ThenByDescending(c => c.NumberOfArgumentsSatisfedByPropertyValuesPassedBackIn)
				.ThenBy(c => c.NumberOfArgumentsSatisfedByDefaultConstructorValues)
				.First();

			// Create an expression that will generate the new instance. But wrap it in a condition that returns the original reference if no changes were
			// required. Note that since all of the update parameters must be matchable to constructor arguments, the ArgumentValueIndicatesChangeRetrievers
			// will never be an empty set (and should always have the same number of elements as there are update parameters) so we can call First() on this
			// data safe in the knowledge that it will always have at least one value.
			Expression newInstanceGenerator = Expression.New(
				bestConstructor.Constructor,
				bestConstructor.ArgumentValueRetrievers
			);
			Expression doesNotIndicatesChange = Expression.Not(bestConstructor.ArgumentValueIndicatesChangeRetrievers.First());
			foreach (var indicatesChangeRetriever in bestConstructor.ArgumentValueIndicatesChangeRetrievers.Skip(1))
				doesNotIndicatesChange = Expression.AndAlso(doesNotIndicatesChange, Expression.Not(indicatesChangeRetriever));

			// Validate the input to catch null source, inputValues or an inputValues array that doesn't have one value for each update argument in the
			// original update method. Then use the ArgumentValueIndicatesChangeRetrievers expressions to determine whether a new instance is required
			// or if the source reference can be passed straight back.
			var numberOfUpdateArgumentsRequired = updateArguments.Count(); // For extension methods, this is the number of arguments other than the source reference
			var returnTarget = Expression.Label(typeof(T));
			var generator = 
				Expression.Lambda<UpdateWithSignature<T>>(
					Expression.Block(
						Expression.IfThen(
							Expression.Equal(sourceParameter, Expression.Constant(null)),
							Expression.Throw(
								Expression.Constant(new ArgumentNullException("source")),
								typeof(T)
							)
						),
						Expression.IfThen(
							Expression.Equal(argsParameter, Expression.Constant(null)),
							Expression.Throw(
								Expression.Constant(new ArgumentNullException("updateValues")),
								typeof(T)
							)
						),
						Expression.IfThen(
							Expression.NotEqual(Expression.ArrayLength(argsParameter), Expression.Constant(numberOfUpdateArgumentsRequired)),
							Expression.Throw(
								Expression.Constant(new ArgumentException("there must be precisely " + numberOfUpdateArgumentsRequired + " values provided", "updateValues")),
								typeof(T)
							)
						),
						Expression.IfThenElse(
							doesNotIndicatesChange,
							Expression.Return(returnTarget, sourceParameter),
							Expression.Return(returnTarget, newInstanceGenerator)
						),
						Expression.Label(returnTarget, Expression.Constant(null, typeof(T)))
					),
					sourceParameter,
					argsParameter
				)
				.Compile();
			_cache.Set<T>(declaringType, updateArguments, generator);
			return generator;
		}

		private static Expression GetOptionalValueMethodCallThatTakesFallBackPropertyExpression(
			MethodInfo methodToCall,
			ParameterExpression sourceParameter,
			ParameterInfo updateArgument,
			Expression updateArgumentValue,
			PropertyInfo fallbackProperty)
		{
			if (methodToCall == null)
				throw new ArgumentNullException("methodToCall");
			if (sourceParameter == null)
				throw new ArgumentNullException("sourceParameter");
			if (updateArgument == null)
				throw new ArgumentNullException("updateArgument");
			if (updateArgumentValue == null)
				throw new ArgumentNullException("updateArgumentValue");
			if (fallbackProperty == null)
				throw new ArgumentNullException("fallbackProperty");

			if (!IsUpdateArgumentAnOptionalValueType(updateArgument.ParameterType))
				throw new ArgumentException("must be an OptionalValue<>", "updateArgument");

			if (methodToCall.DeclaringType != updateArgument.ParameterType)
				throw new ArgumentException("must be a method method on updateArgument's ParameterType", "methodToCall");
			if (methodToCall.GetParameters().Length != 1)
				throw new ArgumentException("must be a single-argument method", "methodToCall");

			if (!GetUpdateArgumentOptionalValueInnerType(updateArgument).IsAssignableFrom(fallbackProperty.PropertyType))
				throw new ArgumentException("must have a PropertyType that is assignable to the OptionalValue<> generic typeparam", "fallbackProperty");
			if (!sourceParameter.Type.IsAssignableFrom(fallbackProperty.DeclaringType))
				throw new ArgumentException("must be declared on a type that is assignable to the sourceParameter's Type", "fallbackProperty");

			return Expression.Call(
				Expression.Convert(
					updateArgumentValue,
					updateArgument.ParameterType
				),
				methodToCall,
				Expression.Property(sourceParameter, fallbackProperty)
			);
		}

		private static Type GetUpdateArgumentOptionalValueInnerType(ParameterInfo updateArgument)
		{
			if (updateArgument == null)
				throw new ArgumentNullException("updateArgument");
			if (!IsUpdateArgumentAnOptionalValueType(updateArgument.ParameterType))
				throw new ArgumentException("Update argument is not of type OptionalValue: \"" + updateArgument.Name + "\"");

			return updateArgument.ParameterType.GetGenericArguments().Single();
		}

		private static bool IsUpdateArgumentAnOptionalValueType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(OptionalValue<>));
		}

		public delegate bool UpdateArgumentToPropertyComparison(ParameterInfo updateArgument, PropertyInfo sourceProperty);
		public delegate bool UpdateArgumentToConstructorArgumentComparison(ParameterInfo updateArgument, ConstructorInfo constructor, ParameterInfo constructorArgument);
		public delegate bool PropertyToConstructorArgumentComparison(PropertyInfo sourceProperty, ConstructorInfo constructor, ParameterInfo constructorArgument);
		public interface ICacheGenerators
		{
			/// <summary>
			/// This should never be called with null declaringType or updateArgumentParameters references and updateArgumentParameters should not be empty
			/// nor contain any null references. The declaringType should have a paramterType that is assignable to typeparam T. This should return null
			/// if the cache can not provide the requested data.
			/// </summary>
			UpdateWithSignature<T> GetIfAvailable<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters);

			/// <summary>
			/// This should never be called with null declaringType, updateArgumentParameters or generator references and updateArgumentParameters should
			/// not be empty nor contain any null references. The declaringType should have a paramterType that is assignable to typeparam T.
			/// </summary>
			void Set<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters, UpdateWithSignature<T> generator);
		}
	}
}