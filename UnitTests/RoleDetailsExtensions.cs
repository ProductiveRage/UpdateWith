using System;
using ProdutiveRage.UpdateWith;

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
			Optional<string> title = new Optional<string>(),
			Optional<DateTime> startDate = new Optional<DateTime>(),
			Optional<DateTime?> endDateIfAny = new Optional<DateTime?>())
		{
			return DefaultUpdateWithHelper.GetGenerator<RoleDetails>()(source, title, startDate, endDateIfAny);
		}
	}
}
