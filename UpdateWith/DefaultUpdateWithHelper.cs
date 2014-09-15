using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UpdateWithExamples
{
	public static class DefaultUpdateWithHelper
	{
		private readonly static UpdateWithHelper _instance = new UpdateWithHelper(
			DefaultValues.UpdateArgumentToPropertyComparison,
			DefaultValues.UpdateArgumentToConstructorArgumentComparison,
			DefaultValues.PropertyToConstructorArgumentComparison,
			DefaultValues.GeneratorCache,
			DefaultValues.AmbiguityLogger
		);

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static UpdateWithHelper.UpdateWithSignature<T> GetGenerator<T>(int numberOfFramesFromCallSite = 1)
		{
			// See notes in the UpdateWithHelper about the use of MethodImplOptions.NoInlining and the meaning of numberOfFramesFromCallSite
			if (numberOfFramesFromCallSite < 1)
				throw new ArgumentOutOfRangeException("numberOfFramesFromCallSite");

			return _instance.GetGenerator<T>(numberOfFramesFromCallSite + 1);
		}

		public static class DefaultValues
		{
			static DefaultValues()
			{
				GeneratorCache = new Cache();
				AmbiguityLogger = message => { };
			}

			public static UpdateWithHelper.ICacheGenerators GeneratorCache { get; private set; }
			public static Action<string> AmbiguityLogger { get; private set; }

			public static bool UpdateArgumentToPropertyComparison(ParameterInfo updateArgument, PropertyInfo sourceProperty)
			{
				if (updateArgument == null)
					throw new ArgumentNullException("updateArgument");
				if (sourceProperty == null)
					throw new ArgumentNullException("sourceProperty");
				return updateArgument.Name.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase);
			}

			public static bool UpdateArgumentToConstructorArgumentComparison(ParameterInfo updateArgument, ConstructorInfo constructor, ParameterInfo constructorArgument)
			{
				if (updateArgument == null)
					throw new ArgumentNullException("updateArgument");
				if (constructor == null)
					throw new ArgumentNullException("constructor");
				if (constructorArgument == null)
					throw new ArgumentNullException("sourceProperty");
				return updateArgument.Name.Equals(constructorArgument.Name, StringComparison.OrdinalIgnoreCase);
			}

			public static bool PropertyToConstructorArgumentComparison(PropertyInfo sourceProperty, ConstructorInfo constructor, ParameterInfo constructorArgument)
			{
				if (sourceProperty == null)
					throw new ArgumentNullException("sourceProperty");
				if (constructor == null)
					throw new ArgumentNullException("constructor");
				if (constructorArgument == null)
					throw new ArgumentNullException("sourceProperty");
				return sourceProperty.Name.Equals(constructorArgument.Name, StringComparison.OrdinalIgnoreCase);
			}
		}

		private class Cache : UpdateWithHelper.ICacheGenerators
		{
			private readonly ConcurrentDictionary<CacheKeyData, object> _cache;
			public Cache()
			{
				_cache = new ConcurrentDictionary<CacheKeyData, object>(
					new CacheKeyDataComparer()
				);
			}

			/// <summary>
			/// This should never be called with null declaringType or updateArgumentParameters references and updateArgumentParameters should not be empty
			/// nor contain any null references. The declaringType should have a paramterType that is assignable to typeparam T. This should return null
			/// if the cache can not provide the requested data.
			/// </summary>
			public UpdateWithHelper.UpdateWithSignature<T> GetIfAvailable<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters)
			{
				// This will throw argument exceptions for declaringType and updateArgumentParameters if required
				var cacheKey = new CacheKeyData(typeof(T), declaringType, updateArgumentParameters);

				// If the item is available as the correct type, return it - otherwise return null (if not available or if not the required type)
				object cachedResult;
				if (_cache.TryGetValue(cacheKey, out cachedResult))
					return cachedResult as UpdateWithHelper.UpdateWithSignature<T>;
				return null;
			}

			/// <summary>
			/// This should never be called with null declaringType, updateArgumentParameters or generator references and updateArgumentParameters should
			/// not be empty nor contain any null references. The declaringType should have a paramterType that is assignable to typeparam T.
			/// </summary>
			public void Set<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters, UpdateWithHelper.UpdateWithSignature<T> generator)
			{
				if (generator == null)
					throw new ArgumentException("generator");

				// This will throw argument exceptions for declaringType and updateArgumentParameters if required
				var cacheKey = new CacheKeyData(typeof(T), declaringType, updateArgumentParameters);

				// Add the item if it doesn't already exist and overwrite it if it does
				_cache.AddOrUpdate(cacheKey, generator, (existingCacheKey, existingValue) => generator);
			}

			private class CacheKeyData
			{
				private readonly ParameterInfo[] _updateParameters;
				public CacheKeyData(Type targetType, Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters)
				{
					if (targetType == null)
						throw new ArgumentException("targetType");
					if (declaringType == null)
						throw new ArgumentException("declaringType");
					if (updateArgumentParameters == null)
						throw new ArgumentException("updateArgumentParameters");

					_updateParameters = updateArgumentParameters.ToArray();
					if (_updateParameters.Any(p => p == null))
						throw new ArgumentException("Null reference encountered in updateArgumentParameters set");

					DeclaringType = declaringType;
					TargetType = targetType;
				}

				/// <summary>
				/// This will never be null (this is the source type, it should be assignable to the TargetType but may not necessarily be the same)
				/// </summary>
				public Type DeclaringType { get; private set; }

				/// <summary>
				/// This will never be null (this is the type that is being instantiated)
				/// </summary>
				public Type TargetType { get; private set; }

				/// <summary>
				/// This will never return a null reference (it will throw an IndexOutOfRangeException for an invalid index)
				/// </summary>
				public ParameterInfo GetUpdateParameter(int index)
				{
					if ((index < 0) || (index >= _updateParameters.Length))
						throw new IndexOutOfRangeException();
					return _updateParameters[index];
				}

				/// <summary>
				/// This will always be at least one
				/// </summary>
				public int NumberOfUpdateParameters { get { return _updateParameters.Length; } }
			}

			private class CacheKeyDataComparer : IEqualityComparer<CacheKeyData>
			{
				public bool Equals(CacheKeyData x, CacheKeyData y)
				{
					if (x == null)
						throw new ArgumentNullException("x");
					if (y == null)
						throw new ArgumentNullException("y");

					if (!x.DeclaringType.Equals(y.DeclaringType))
						return false;
					if (!x.TargetType.Equals(y.TargetType))
						return false;

					if (x.NumberOfUpdateParameters != y.NumberOfUpdateParameters)
						return false;

					for (var index = 0; index < x.NumberOfUpdateParameters ; index++)
					{
						var parameterX = x.GetUpdateParameter(index);
						var parameterY = y.GetUpdateParameter(index);
						if ((parameterX.Name != parameterY.Name) || !parameterX.ParameterType.Equals(parameterY.ParameterType))
							return false;
					}
					return true;
				}

				public int GetHashCode(CacheKeyData obj)
				{
					if (obj == null)
						throw new ArgumentNullException("obj");
					var hash = obj.DeclaringType.GetHashCode() ^ obj.TargetType.GetHashCode();
					for (var index = 0; index < obj.NumberOfUpdateParameters; index++)
						hash = hash ^ obj.GetUpdateParameter(index).GetHashCode();
					return hash;
				}
			}
		}
	}
}