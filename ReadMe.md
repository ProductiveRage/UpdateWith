# An F#-inspired "UpdateWith" solution for C# immutable classes

This is a small library to try to make code less laborious when dealing with "updates" of immutable types - where an update is a request to change one or more properties on an instance where a new instance is returned (since the initial instance may not be altered, since it is immutable).

Inspired by F# which makes it as easy as 

    let updatedRole = {currentRole with title="Penguin Manager"}

whether one or more properties are being changed.

To illustrate, here is a C# immutable class with a "magic" UpdateWith method -

    public class RoleDetails
    {
      public RoleDetails(string title, DateTime startDate, DateTime? endDateIfAny)
      {
        if (string.IsNullOrWhiteSpace(title))
          throw new ArgumentException("title");
        if ((endDateIfAny != null) && (endDateIfAny <= startDate))
          throw new ArgumentException("title");
          
        Title = title.Trim();
        StartDate = startDate;
        EndDateIfAny = endDateIfAny;
      }

      /// <summary>
      /// This will never be null or blank, it will not have any leading or trailing whitespace
      /// </summary>
      public string Title { get; private set; }
      
      public DateTime StartDate { get; private set; }
      
      /// <summary>
      /// If non-null, this will greater than the StartDate
      /// </summary>
      public DateTime? EndDateIfAny { get; private set; }
      
      public RoleDetails UpdateWith(
        Optional<string> title = new Optional<string>(),
        Optional<DateTime> startDate = new Optional<DateTime>(),
        Optional<DateTime?> endDateIfAny = new Optional<DateTime?>())
      {
        return DefaultUpdateWithHelper.GetGenerator<RoleDetails>()(this, title, startDate, endDateIfAny);
      }
    }

This can be called with the format

    var updatedRole = currentRole.UpdateWith(title: "Penguin Manager");
    
As many or as few properties may be specified as required. The **Optional** is a struct with an implicit operator that means that values that it represents may be passed when an **Optional** is required (as in the above example which passes a **string** for the "title" argument, not an **Optional<string>**). If the property values provided are all the same as the values on the current instance, then that instance is returned back since there is no point creating a new instance with the exact same data.

The "GetGenerator" method inspects the calling method to retrieve the argument names in order to map them on to properties (to determine whether any changes to the current state are being specified) and to the constructor arguments, in order to find a constructor that will allow all of the specified "to-update" properties to be passed through to create a new instance. The return type from the GetGenerator method is a Func that takes the type "T" and an array of property value arguments and returns a new instance (or the same instance, if no values have been changed) of T. This Func is a compiled LINQ expression which is cached, so it should be as quick to execute as hand-crafted code. The compiled expression is cached so that repeated calls do not require it to be rebuilt each time but there *is* some overhead to looking at the calling method's arguments to see if there is already a cached expression that can be reused.

If you require the absolute peak of performance you can do the following, which avoids the cache lookups and the inspection of the UpdateWith method's signature by maintaining a static reference rather than calling GetGenerator repeatedly -

      private static UpdateWithSignature<RoleDetails> updateWith
        = DefaultUpdateWithHelper.GetGenerator<RoleDetails>(typeof(RoleDetails).GetMethod("UpdateWith"));
      public RoleDetails UpdateWith(
        Optional<string> title = new Optional<string>(),
        Optional<DateTime> startDate = new Optional<DateTime>(),
        Optional<DateTime?> endDateIfAny = new Optional<DateTime?>())
      {
        return updateWith(this, title, startDate, endDateIfAny);
      }
      
(Doing so, you sacrifice the convenience of relying on stack analysis to determine the calling method and must specify the method yourself).
      
If an UpdateWith method is specified that doesn't allow the setting of *every* value that a constructor requires, some fallbacks are supported. For example, if the above example had "title" and "startDate" arguments but no "endDateIfAny", then the "EndDateIfAny" property from the current instance will be used to provide a value for the "endDateIfAny" constructor argument. If there are constructor arguments that can not be satisfied by an update argument *or* a property, if the constructor argument has a default value then that may be used. A constructor that has an argument that has no default and can not be matched to an update argument or a property is not eligible for use. On the other hand, if a constructor does not have arguments that *all* of the update arguments can be mapped to then it is also not eligible for use (since this would mean that update arguments would effectively get ignored). If there are no eligible constructors (and all public constructors will be considered) then an exception will be raised when the GetGenerator method is called. If there are multiple constructors that could be used, they will be sorted by the number of constructor arguments that are satisifed by update arguments and then by the number of constructor arguments that are satisfied by existing properties and the first item taken.

*Note: There is actually a way to eke out a little more performance; details can be found in the "Further performance optimisations" section in my blog post [Implementing F#-inspired "with" updates for immutable classes in C#](http://productiverage.com/implementing-f-sharp-inspired-with-updates-for-immutable-classes-in-c-sharp) - but, as I note there, if you think you're getting to the point at which such optimisations are appropriate, it might be best to resort to hand-rolled code!*.

## More detail

The **DefaultUpdateWithHelper** is a static wrapper around the **UpdateWithHelper** class. The **DefaultUpdateWithHelper** does not allow any configuration, though the **UpdateWithHelper** class does in case customisations are required. Its constructors is as follows

    public UpdateWithHelper(
      UpdateArgumentToPropertyComparison updateArgumentToPropertyComparison,
      UpdateArgumentToConstructorArgumentComparison updateArgumentToConstructorArgumentComparison,
      PropertyToConstructorArgumentComparison propertyToConstructorArgumentComparison,
      ICacheGenerators cache,
      Action<string> ambiguityLogger)

The comparers are used to determine whether a property matches an update argument, or a constructor argument matches an update argument or a property value matches a constructor argument (for cases where an update method only satisfies a subset of the properties that a constructor requires to be specified) and are delegates with the signatures

    public delegate bool UpdateArgumentToPropertyComparison(
      ParameterInfo updateArgument,
      PropertyInfo sourceProperty);
      
    public delegate bool UpdateArgumentToConstructorArgumentComparison(
      ParameterInfo updateArgument,
      ConstructorInfo constructor,
      ParameterInfo constructorArgument);
      
    public delegate bool PropertyToConstructorArgumentComparison(
      PropertyInfo sourceProperty,
      ConstructorInfo constructor,
      ParameterInfo constructorArgument);

The cache has the interface

    public interface ICacheGenerators
    {
      UpdateWithSignature<T> GetIfAvailable<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters);
      void Set<T>(Type declaringType, IEnumerable<ParameterInfo> updateArgumentParameters, UpdateWithSignature<T> generator);
    }
    
and is used to prevent the regeneration of compiled expressions when existing ones may be reused.

Finally the "ambiguityLogger" is used to record messages where the Comparison delegates find multiple matches that may be used - in this case, an arbitrary decision is made but it may be useful to know that the current configuration results in ambiguity in matching.

The **DefaultUpdateWithHelper** class has a static nested type **DefaultValues** which has properties with the values that it uses when building its view of a default **UpdateWithHelper**. If a custom implementation is required that wants to use the default implementations for some of the configuration options, these defaults may be used to fill in the blanks.