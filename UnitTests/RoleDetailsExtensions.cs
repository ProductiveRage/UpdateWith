using System;
using UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// Extension methods are also supported, the first argument must be the source reference and the remaining arguments follow the same format as for an instance
	/// UpdateWith method; a set of OptionalValue mirroring the fields that must be updateable.
	/// </summary>
	public static class RoleDetailsExtensions
	{
		public static RoleDetails UpdateWith(
			this RoleDetails source,
			OptionalValue<string> title = new OptionalValue<string>(),
			OptionalValue<DateTime> startDate = new OptionalValue<DateTime>(),
			OptionalValue<DateTime?> endDateIfAny = new OptionalValue<DateTime?>())
		{
			return DefaultUpdateWithHelper.GetGenerator<RoleDetails>()(source, title, startDate, endDateIfAny);
		}
	}
}
