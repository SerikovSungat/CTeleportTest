using System.Runtime.Serialization;

namespace IntegrationBus.Application.Exceptions
{
	public abstract class ApplicationExceptionBase : Exception, IApplicationException
	{
		protected ApplicationExceptionBase(ErrorCode errorCode)
		{
			this.ErrorCode = errorCode;
		}

		protected ApplicationExceptionBase(SerializationInfo info, StreamingContext context, ErrorCode errorCode) : base(info, context)
		{
			this.ErrorCode = errorCode;
		}

		protected ApplicationExceptionBase(string message, ErrorCode errorCode) : base(message)
		{
			this.ErrorCode = errorCode;
		}

		protected ApplicationExceptionBase(string message, Exception innerException, ErrorCode errorCode) : base(message, innerException)
		{
			this.ErrorCode = errorCode;
		}

		public ErrorCode ErrorCode { get; }

	}
}
