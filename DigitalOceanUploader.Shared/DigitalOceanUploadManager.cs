using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using HeyRed.Mime;
using Amazon.S3;

namespace DigitalOceanUploader.Shared
{
	/// <summary>
	/// Digital ocean upload manager.
	/// </summary>
	public class DigitalOceanUploadManager : IDisposable
	{
		KeyManager _keyManager;
		string _serviceUrl;
		string _spaceName;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.DigitalOceanUploadManager"/> class.
		/// </summary>
		/// <param name="accessKey">Access key.</param>
		/// <param name="secretKey">Secret key.</param>
		/// <param name="spaceName">Space name.</param>
		/// <param name="serviceUrl">Service URL.</param>
		public DigitalOceanUploadManager(string accessKey, string secretKey, string spaceName, string serviceUrl = "https://nyc3.digitaloceanspaces.com")
			: this(spaceName, serviceUrl)
		{
			if (string.IsNullOrEmpty(accessKey))
				throw new ArgumentNullException(nameof(accessKey));
			if (string.IsNullOrEmpty(secretKey))
				throw new ArgumentNullException(nameof(secretKey));

			_keyManager = new KeyManager(accessKey, secretKey);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.DigitalOceanUploadManager"/> class.
		/// </summary>
		/// <param name="keyManager">Key manager.</param>
		/// <param name="spaceName">Space name.</param>
		/// <param name="serviceUrl">Service URL.</param>
		public DigitalOceanUploadManager(KeyManager keyManager, string spaceName, string serviceUrl = "https://nyc3.digitaloceanspaces.com")
			: this(spaceName, serviceUrl)
		{
			if (keyManager == null)
				throw new ArgumentNullException(nameof(keyManager));

			_keyManager = keyManager;
		}

		private DigitalOceanUploadManager(string spaceName, string serviceUrl = "https://nyc3.digitaloceanspaces.com")
		{
			if (string.IsNullOrEmpty(spaceName))
				throw new ArgumentNullException(nameof(spaceName));
			if (string.IsNullOrEmpty(serviceUrl))
				throw new ArgumentNullException(nameof(serviceUrl));

			_spaceName = spaceName;
			_serviceUrl = serviceUrl;
		}

		/// <summary>
		/// Cleans up previous attempts.
		/// </summary>
		/// <returns>The up previous attempts.</returns>
		public async Task CleanUpPreviousAttempts()
		{
			using(var client = CreateNewClient())
			{
				var currentMultiParts = await client.ListMultipartUploadsAsync(_spaceName);
				foreach(var multiPart in currentMultiParts.MultipartUploads)
				{
					await client.AbortMultipartUploadAsync(currentMultiParts.BucketName, multiPart.Key, multiPart.UploadId);
				}
			}
		}

		/// <summary>
		/// Uploads the file.
		/// </summary>
		/// <returns>The upload id.</returns>
		/// <param name="filePath">File path.</param>
		/// <param name="uploadName">Upload name.</param>
		/// <param name="maxPartRetry">Max part retry.</param>
		public async Task<string> UploadFile(string filePath, string uploadName, int maxPartRetry = 3, long maxPartSize = 6000000L)
		{
			if (string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));
			if (string.IsNullOrWhiteSpace(uploadName))
				throw new ArgumentNullException(nameof(uploadName));
			if (maxPartRetry < 1)
				throw new ArgumentException("Max Part Retry needs to be greater than or equal to 1", nameof(maxPartRetry));
			if (maxPartSize < 1)
				throw new ArgumentException("Max Part Size needs to be greater than 0", nameof(maxPartSize));

			var fileInfo = new FileInfo(filePath);
			var contentType = MimeGuesser.GuessFileType(filePath).MimeType;
			Amazon.S3.Model.InitiateMultipartUploadResponse multiPartStart;

			using (var client = CreateNewClient())
			{
				multiPartStart = await client.InitiateMultipartUploadAsync(new Amazon.S3.Model.InitiateMultipartUploadRequest()
				{
					BucketName = _spaceName,
					ContentType = contentType,
					Key = uploadName
				});

				var estimatedParts = (int)(fileInfo.Length / maxPartSize);
				if (estimatedParts == 0)
					estimatedParts = 1;

				UploadStatusEvent?.Invoke(this, new UploadStatus(0, estimatedParts, 0, fileInfo.Length));

				try
				{
					var i = 0L;
					var n = 1;
					Dictionary<string, int> parts = new Dictionary<string, int>();
					while(i < fileInfo.Length)
					{
						long partSize = maxPartSize;
						var lastPart = (i + partSize) >= fileInfo.Length;
						if (lastPart)
							partSize = fileInfo.Length - i;
						bool complete = false;
						int retry = 0;
						Amazon.S3.Model.UploadPartResponse partResp = null;
						do
						{
							retry++;
							try
							{
								partResp = await client.UploadPartAsync(new Amazon.S3.Model.UploadPartRequest()
								{
									BucketName = _spaceName,
									FilePath = filePath,
									FilePosition = i,
									IsLastPart = lastPart,
									PartSize = partSize,
									PartNumber = n,
									UploadId = multiPartStart.UploadId,
									Key = uploadName
								});
								complete = true;
							}
							catch (Exception ex)
							{
								UploadExceptionEvent?.Invoke(this, new UploadException($"Failed to upload part {n} on try #{retry}", ex));
							}
						} while (!complete && retry <= maxPartRetry);

						if (!complete || partResp == null)
							throw new Exception($"Unable to upload part {n}");

						parts.Add(partResp.ETag, n);
						i += partSize;
						UploadStatusEvent?.Invoke(this, new UploadStatus(n, estimatedParts, i, fileInfo.Length));
						n++;
					}

					// upload complete
					var completePart = await client.CompleteMultipartUploadAsync(new Amazon.S3.Model.CompleteMultipartUploadRequest()
					{
						UploadId = multiPartStart.UploadId,
						BucketName = _spaceName,
						Key = uploadName,
						PartETags = parts.Select(p => new Amazon.S3.Model.PartETag(p.Value, p.Key)).ToList()
					});
				}
				catch(Exception ex)
				{
					var abortPart = await client.AbortMultipartUploadAsync(_spaceName, uploadName, multiPartStart.UploadId);
					UploadExceptionEvent?.Invoke(this, new UploadException("Something went wrong upload file and it was aborted", ex));
				}
			}

			return multiPartStart?.UploadId;
		}

		/// <summary>
		/// Occurs when upload exception event.
		/// </summary>
		public event EventHandler<UploadException> UploadExceptionEvent = delegate { };

		/// <summary>
		/// Occurs when upload status event.
		/// </summary>
		public event EventHandler<UploadStatus> UploadStatusEvent = delegate { };

		private AmazonS3Client CreateNewClient()
		{
			return new AmazonS3Client(KeyManager.SecureStringToString(_keyManager.AccessKey), KeyManager.SecureStringToString(_keyManager.SecretKey), new AmazonS3Config()
			{
				ServiceURL = _serviceUrl
			});
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_keyManager?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~DigitalOceanUploadManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
