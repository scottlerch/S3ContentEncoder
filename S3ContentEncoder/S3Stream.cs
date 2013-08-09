using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;

namespace S3ContentEncoder
{
	/// <summary>
	/// S3 download stream that adds extra layer of retry logic and only resumes 
	/// where it left off since last successful read.
	/// </summary>
	internal class S3Stream : Stream
	{
		private readonly AmazonS3Client s3Client;

		private readonly string bucketName;
		private readonly string objectKey;

		private readonly long totalBytes;
		private long byteOffset;

		private GetObjectResponse response;

		public S3Stream(AmazonS3Client s3Client, string bucketName, string objectKey)
		{
			this.s3Client = s3Client;
			this.bucketName = bucketName;
			this.objectKey = objectKey;

			var metaRequest = new GetObjectMetadataRequest
			{
				BucketName = bucketName,
				Key = objectKey,
			};

			using (var metadata = this.s3Client.GetObjectMetadata(metaRequest))
			{
				this.totalBytes = metadata.ContentLength;

				switch (metadata.Headers.Get("Content-Encoding"))
				{
					case "gzip":
						this.ContentEncoding = ContentEncoding.Gzip;
						break;
					default:
						this.ContentEncoding = ContentEncoding.None;
						break;
				}
			}
		}

		public ContentEncoding ContentEncoding { get; private set; }

		private void EnsureResponseExists()
		{
			if (this.response == null)
			{
				var request = new GetObjectRequest
				{
					BucketName = this.bucketName,
					Key = this.objectKey,
					ByteRangeLong = new Amazon.S3.Model.Tuple<long, long>(this.byteOffset, this.totalBytes),
				};

				this.response = this.s3Client.GetObject(request);
			}
		}

		private void CleanupResponse()
		{
			if (this.response != null)
			{
				this.response.Dispose();
				this.response = null;
			}
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
		}

		public override long Length
		{
			get { return this.totalBytes; }
		}

		public override long Position
		{
			get
			{
				return this.byteOffset;
			}
			set
			{
				throw new InvalidOperationException("This stream does not allow seeking");
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return Utilities.RetryOnException(() =>
				{
					this.EnsureResponseExists();

					int bytesRead = this.response.ResponseStream.Read(buffer, offset, count);

					if (bytesRead > 0)
					{
						this.byteOffset += bytesRead;
						return bytesRead;
					}

					if (this.byteOffset == this.totalBytes)
					{
						// End of stream
						return 0;
					}

					throw new InvalidDataException(string.Format("Only received {0} of {1} bytes", this.byteOffset, this.totalBytes));
				},
				ex => this.CleanupResponse());
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException("This stream does not allow seeking");
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException("This stream does not allow writing");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException("This stream does not allow writing");
		}

		protected override void Dispose(bool disposing)
		{
			if (this.response != null)
			{
				this.response.Dispose();
				this.response = null;
			}
		}
	}
}
