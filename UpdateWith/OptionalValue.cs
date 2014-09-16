namespace UpdateWith
{
	public struct OptionalValue<T>
	{
		private T _valueIfSet;
		private bool _valueHasBeenSet;

		public T GetValue(T valueIfNoneSet)
		{
			return _valueHasBeenSet ? _valueIfSet : valueIfNoneSet;
		}

		public bool IndicatesChangeFromValue(T value)
		{
			if (!_valueHasBeenSet)
				return false;

			if ((value == null) && (_valueIfSet == null))
				return false;
			else if ((value == null) || (_valueIfSet == null))
				return true;

			return !value.Equals(_valueIfSet);
		}

		public static implicit operator OptionalValue<T>(T value)
		{
			return new OptionalValue<T>
			{
				_valueIfSet = value,
				_valueHasBeenSet = true
			};
		}
	}
}