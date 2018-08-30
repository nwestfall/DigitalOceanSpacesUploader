using System;
namespace DigitalOceanUploader.Shared
{
	/// <summary>
	/// Upload status.
	/// </summary>
	public class UploadStatus : EventArgs
	{
		/// <summary>
		/// Gets or sets the part number.
		/// </summary>
		/// <value>The part number.</value>
		public int PartNumber { get; set; }

		/// <summary>
		/// Gets or sets the estimated parts.
		/// </summary>
		/// <value>The estimated parts.</value>
		public int EstimatedParts { get; set; }

		/// <summary>
		/// Gets or sets the bytes uploaded.
		/// </summary>
		/// <value>The bytes uploaded.</value>
		public long BytesUploaded { get; set; }

		/// <summary>
		/// Gets or sets the total bytes.
		/// </summary>
		/// <value>The total bytes.</value>
		public long TotalBytes { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.UploadStatus"/> class.
		/// </summary>
		/// <param name="partNumber">Part number.</param>
		/// <param name="estimatedParts">Estimated parts.</param>
		/// <param name="bytesUploaded">Bytes uploaded.</param>
		/// <param name="totalBytes">Total bytes.</param>
		public UploadStatus(int partNumber, int estimatedParts, long bytesUploaded, long totalBytes)
		{
			PartNumber = partNumber;
			EstimatedParts = estimatedParts;
			BytesUploaded = bytesUploaded;
			TotalBytes = totalBytes;
		}
	}
}
