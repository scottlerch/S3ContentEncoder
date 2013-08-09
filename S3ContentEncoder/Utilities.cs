using System;
using System.Threading;

namespace S3ContentEncoder
{
	internal static class Utilities
	{
		internal static T RetryOnException<T>(Func<T> func, Action<Exception> onException = null)
		{
			const int waitAfterExceptionMsec = 1000;
			const int maxNumberOfRetries = 10;

			var numberOfRetries = 0;

			while(true)
			{
				try
				{
					return func();
				}
				catch (Exception ex)
				{
					if (onException != null)
					{
						onException(ex);
					}

					numberOfRetries++;
					if (numberOfRetries >= maxNumberOfRetries)
					{
						throw;
					}

					Thread.Sleep(waitAfterExceptionMsec * numberOfRetries);
				}
			}
		}
	}
}
