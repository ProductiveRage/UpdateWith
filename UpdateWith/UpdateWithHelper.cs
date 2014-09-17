using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProdutiveRage.UpdateWith
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
			return GetGenerator<T>(
				new StackFrame(skipFrames: numberOfFramesFromCallSite, fNeedFileInfo: false).GetMethod()
			);
		}

		public UpdateWithSignature<T> GetGenerator<T>(MethodBase updateMethod)
		{
			if (updateMethod == null)
				throw new ArgumentNullException("updateMethod");

			var updateMethodDetails = new UpdateMethodSummary<T>(updateMethod);
			var cachedGenerator = _cache.GetIfAvailable<T>(updateMethodDetails.DeclaringType, updateMethodDetails.UpdateArguments);
			if (cachedGenerator != null)
				return cachedGenerator;

			// In this case, the delegate has a single updateValues reference - an array of update values (so argsIsAnArray is true
			var sourceParameter = Expression.Parameter(typeof(T), "source");
			var argsParameter = Expression.Parameter(typeof(object[]), "args");
			var argsIsAnArray = true;
			var generator
				= Expression.Lambda<UpdateWithSignature<T>>(
					GetGeneratorBodyExpression<T>(updateMethodDetails.UpdateArguments, sourceParameter, new[] { argsParameter }, argsIsAnArray),
					sourceParameter,
					argsParameter
				)
				.Compile();
			_cache.Set<T>(updateMethodDetails.DeclaringType, updateMethodDetails.UpdateArguments, generator);
			return generator;
		}

		public UpdateWithSignature1<T> GetUncachedGenerator1<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature1<T>>(updateMethod, 1);
		}
		public UpdateWithSignature2<T> GetUncachedGenerator2<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature2<T>>(updateMethod, 2);
		}
		public UpdateWithSignature3<T> GetUncachedGenerator3<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature3<T>>(updateMethod, 3);
		}
		public UpdateWithSignature4<T> GetUncachedGenerator4<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature4<T>>(updateMethod, 4);
		}
		public UpdateWithSignature5<T> GetUncachedGenerator5<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature5<T>>(updateMethod, 5);
		}
		public UpdateWithSignature6<T> GetUncachedGenerator6<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature6<T>>(updateMethod, 6);
		}
		public UpdateWithSignature7<T> GetUncachedGenerator7<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature7<T>>(updateMethod, 7);
		}
		public UpdateWithSignature8<T> GetUncachedGenerator8<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature8<T>>(updateMethod, 8);
		}
		public UpdateWithSignature9<T> GetUncachedGenerator9<T>(MethodBase updateMethod)
		{
			return (new UncachedGeneratorWrapper<T>(this)).GetUncachedGeneratorExpression<UpdateWithSignature9<T>>(updateMethod, 9);
		}

		private Expression GetGeneratorBodyExpression<T>(
			IEnumerable<ParameterInfo> updateArguments,
			ParameterExpression sourceParameter,
			IEnumerable<ParameterExpression> argsParameters,
			bool argsIsAnArray)
		{
			if (updateArguments == null)
				throw new ArgumentNullException("updateArguments");
			var updateArgumentsArray = updateArguments.ToArray();
			if (updateArgumentsArray.Any(arg => arg == null))
				throw new ArgumentException("null reference encountered", "updateArguments");
			if (!updateArgumentsArray.Any())
				throw new ArgumentException("may not be an empty set", "updateArguments");
			if (sourceParameter == null)
				throw new ArgumentNullException("sourceParameter");
			if (!typeof(T).IsAssignableFrom(sourceParameter.Type))
				throw new ArgumentException("must represent a type that is assignable to T", "sourceParameter");
			if (argsParameters == null)
				throw new ArgumentNullException("argsParameters");
			var argsParametersArray = argsParameters.ToArray();
			if (argsParametersArray.Any(arg => arg == null))
				throw new ArgumentException("null reference encountered", "argsParameters");
			if (!argsParametersArray.Any())
				throw new ArgumentException("may not be an empty set", "argsParameters");
			if (argsIsAnArray)
			{
				if (argsParametersArray.Length > 1)
					throw new ArgumentException("must only have a single element if the argument parameter is to be an array of argument values", "argsParameters");
			}
			else
			{
				if (argsParametersArray.Length != updateArgumentsArray.Length)
					throw new ArgumentException("number of elements must match that of updateArguments if the argument parameter is not an array of argument values", "argsParameters");
			}

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
					Expression argumentValueExpression;
					if (argsIsAnArray)
					{
						// If there is a single argument that is an array of argument values, then take the element from that array (the
						// array expression itself will always the first - and only - element in the argsParametersArray)
						argumentValueExpression = Expression.ArrayAccess(argsParametersArray[0], Expression.Constant(argumentWithIndex.Index));
					}
					else
					{
						// If there will be one concrete argument provided for each argument value, then take it direct from the argument
						// parameter expression array (this requires the less flexible UpdateWithSignature1, UpdateWithSignature2, etc..
						// delegates but means that an array need not be created to pass the arguments in)
						argumentValueExpression = argsParametersArray[argumentWithIndex.Index];
					}
					var indicatesChangeFromValueMethod = argument.ParameterType.GetMethod("IndicatesChangeFromValue");
					var getValueMethod = argument.ParameterType.GetMethod("GetValue");
					return new
					{
						Argument = argument,
						InnerType = optionalValueInnerType,
						IsChangeIndicatedRetriever = GetOptionalValueMethodCallThatTakesFallBackPropertyExpression(indicatesChangeFromValueMethod, sourceParameter, argument, argumentValueExpression, sourceProperty),
						ValueRetriever = GetOptionalValueMethodCallThatTakesFallBackPropertyExpression(getValueMethod, sourceParameter, argument, argumentValueExpression, sourceProperty)
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

					return new
					{
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
			var bodyExpressions = new List<Expression>
			{
				Expression.IfThen(
					Expression.Equal(sourceParameter, Expression.Constant(null)),
					Expression.Throw(
						Expression.Constant(new ArgumentNullException("source")),
						typeof(T)
					)
				)
			};
			if (argsIsAnArray)
			{
				var paramsArgParameter = argsParametersArray[0];
				bodyExpressions.Add(
					Expression.IfThen(
						Expression.Equal(paramsArgParameter, Expression.Constant(null)),
						Expression.Throw(
							Expression.Constant(new ArgumentNullException("updateValues")),
							typeof(T)
						)
					)
				);
				var numberOfUpdateArgumentsRequired = updateArguments.Count(); // For extension methods, this is the number of arguments other than the source reference
				bodyExpressions.Add(
					Expression.IfThen(
						Expression.NotEqual(Expression.ArrayLength(paramsArgParameter), Expression.Constant(numberOfUpdateArgumentsRequired)),
						Expression.Throw(
							Expression.Constant(new ArgumentException("there must be precisely " + numberOfUpdateArgumentsRequired + " values provided", "updateValues")),
							typeof(T)
						)
					)
				);
			}
			var returnTarget = Expression.Label(typeof(T));
			bodyExpressions.Add(
				Expression.IfThenElse(
					doesNotIndicatesChange,
					Expression.Return(returnTarget, sourceParameter),
					Expression.Return(returnTarget, newInstanceGenerator)
				)
			);
			bodyExpressions.Add(
				Expression.Label(returnTarget, Expression.Constant(null, typeof(T)))
			);
			return Expression.Block(bodyExpressions);
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

			return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Optional<>));
		}

		/// <summary>
		/// This class is used to wrap up validation rather than repeat it multiple times for GetGenerator, GetUncachedGenerator1, GetUncachedGenerator2, etc..
		/// </summary>
		private class UpdateMethodSummary<T>
		{
			public UpdateMethodSummary(MethodBase updateMethod)
			{
				// If this is an instance method then the arguments should all be update values. For an extension method, the first argument should be a source reference and
				// the subsequent arguments be the update values. There could potentially be a static method on the class, similar in form to the instance method (it would
				// need a "source" reference from somewhere, but it shouldn't be one of the method arguments so this case would be treated like an instance update method).
				var updateMethodArguments = updateMethod.GetParameters();
				if (updateMethodArguments.Length < 1)
					throw new ArgumentException("The update method must have at least one argument since there must be at least one property specified to update");
				if (IsUpdateArgumentAnOptionalValueType(updateMethodArguments[0].ParameterType))
				{
					DeclaringType = updateMethod.DeclaringType;
					if (!typeof(T).IsAssignableFrom(DeclaringType))
						throw new ArgumentException("When the first update method argument is not used to specify the source reference, the method's DeclaringType is used and it must be assignable to T, which it is not here");
					UpdateArguments = updateMethodArguments.ToList().AsReadOnly();
				}
				else
				{
					if (updateMethodArguments.Length < 2)
						throw new ArgumentException("Where the first update method is a source reference, there must be at least two arguments; the source reference and at least one property to update");
					DeclaringType = updateMethodArguments.First().ParameterType;
					UpdateArguments = updateMethodArguments.Skip(1).ToList().AsReadOnly();
				}
				UpdateMethod = updateMethod;
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public MethodBase UpdateMethod { get; private set; }

			/// <summary>
			/// This will never be null, it will always be assignable to the type T
			/// </summary>
			public Type DeclaringType { get; private set; }

			/// <summary>
			/// This will never be null, empty nor contain any null references. If the UpdateMethod is a static extension method then the first argument of that method will
			/// be a source reference and not represent a property value to update and so will not be included in this set. Every parameter in this set will be of type
			/// OptionalValue.
			/// </summary>
			public IEnumerable<ParameterInfo> UpdateArguments { get; private set; }
		}

		/// <summary>
		/// This wrapper class is required to nest the types TSource and TUpdateSignature since TUpdateSignature will reference TSource (without this, there would
		/// be an error "The type parameter cannot be used with type arguments" as one type parameter would reference another, which the compiler is not happy with)
		/// </summary>
		private class UncachedGeneratorWrapper<TSource>
		{
			private readonly UpdateWithHelper _updateWitHelper;
			public UncachedGeneratorWrapper(UpdateWithHelper updateWitHelper)
			{
				if (updateWitHelper == null)
					throw new ArgumentNullException("updateWitHelper");

				_updateWitHelper = updateWitHelper;
			}

			public TUpdateSignature GetUncachedGeneratorExpression<TUpdateSignature>(MethodBase updateMethod, int numberOfUpdateArguments)
			{
				if (updateMethod == null)
					throw new ArgumentNullException("updateMethod");
				if (numberOfUpdateArguments <= 0)
					throw new ArgumentOutOfRangeException("must be greater than zero", "numberOfUpdateArguments");

				// In this case, the delegate will have one concrete argument per update value (as opposed to a single argument that is an array
				// of update values), so argsIsAnArray is false)
				var updateMethodDetails = new UpdateMethodSummary<TSource>(updateMethod);
				var sourceParameter = Expression.Parameter(typeof(TSource), "source");
				var argParameters = Enumerable.Range(0, numberOfUpdateArguments)
					.Select(i => Expression.Parameter(typeof(object), "arg" + i))
					.ToArray(); // Call ToArray() to evaluate this once otherwise the reference won't match when used twice below
				var argsIsAnArray = false;
				return
					Expression.Lambda<TUpdateSignature>(
						_updateWitHelper.GetGeneratorBodyExpression<TSource>(updateMethodDetails.UpdateArguments, sourceParameter, argParameters, argsIsAnArray),
						new[] { sourceParameter }.Concat(argParameters)
					)
					.Compile();
			}
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