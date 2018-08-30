using System;
namespace DigitalOceanUploader.Shared
{
	/// <summary>
	/// Upload exception event args.
	/// </summary>
	public class UploadException : EventArgs
	{
		/// <summary>
		/// Gets the exception.
		/// </summary>
		/// <value>The exception.</value>
		public Exception Exception { get; private set; }

		/// <summary>
		/// Gets the message.
		/// </summary>
		/// <value>The message.</value>
		public string Message { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.UploadException"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="ex">Ex.</param>
		public UploadException(string message, Exception ex)
		{
			Message = message;
			Exception = ex;
		}
	}
}
