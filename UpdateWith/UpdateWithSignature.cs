namespace UpdateWith
{
	public delegate T UpdateWithSignature<T>(T source, params object[] updateValues);

	public delegate T UpdateWithSignature1<T>(T source, object arg0);
	public delegate T UpdateWithSignature2<T>(T source, object arg0, object arg1);
	public delegate T UpdateWithSignature3<T>(T source, object arg0, object arg1, object arg2);
	public delegate T UpdateWithSignature4<T>(T source, object arg0, object arg1, object arg2, object arg3);
	public delegate T UpdateWithSignature5<T>(T source, object arg0, object arg1, object arg2, object arg3, object arg4);
	public delegate T UpdateWithSignature6<T>(T source, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5);
	public delegate T UpdateWithSignature7<T>(T source, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
	public delegate T UpdateWithSignature8<T>(T source, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);
	public delegate T UpdateWithSignature9<T>(T source, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8);
}