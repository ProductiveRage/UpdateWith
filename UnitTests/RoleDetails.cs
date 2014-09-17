using System;
using ProdutiveRage.UpdateWith;

namespace UnitTests
{
	/// <summary>
	/// This is a marginally more complex type that doesn't have an UpdateWith method declared within it but relies on an extension method instead
	/// </summary>
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
	}
}
