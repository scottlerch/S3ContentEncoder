using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace S3ContentEncoder
{
	/// <summary>
	/// This is a facade to the S3 SDK that understands the Content-Encoding HTTP header
	/// and will compress/decompress data as necessary.
	/// </summary>
	internal class S3Facade
	{
		private readonly AmazonS3Client s3Client;
		private readonly TransferUtility transferUtility;

		public S3Facade(string accessKey, string secretKey)
		{
			this.s3Client = new AmazonS3Client(accessKey, secretKey);
			this.transferUtility = new TransferUtility(this.s3Client);
		}

		public IEnumerable<string> ListAllObjects(string bucket, string prefix = null)
		{
			prefix = prefix != null && !prefix.EndsWith("/") ? prefix + '/' : prefix;

			string marker = null;
			bool done = false;

			while (!done)
			{
				var response = this.s3Client.ListObjects(
					new ListObjectsRequest
					{
						BucketName = bucket,
						Prefix = prefix,
						Marker = marker,
					});

				marker = response.NextMarker;
				done = !response.IsTruncated;

				foreach (var obj in response.S3Objects.Where(o => !o.Key.EndsWith("/")))
				{
					yield return obj.Key;
				}
			}
		}

		public bool HasEncoding(ContentEncoding encoding, string bucket, string key)
		{
			var response = this.s3Client.GetObjectMetadata(
				new GetObjectMetadataRequest
				{
					BucketName = bucket,
					Key = key,
				});

			return encoding.ToString().Equals(response.Headers.Get("Content-Encoding"), StringComparison.InvariantCultureIgnoreCase);
		}

		public Stream Download(string bucket, string key)
		{
			var response = this.s3Client.GetObjectMetadata(
				new GetObjectMetadataRequest
				{
					BucketName = bucket,
					Key = key,
				});

			if (response.Headers.Get("Content-Encoding") == "gzip")
			{
				return new GZipStream(new S3Stream(this.s3Client, bucket, key), CompressionMode.Decompress);
			}

			return new S3Stream(this.s3Client, bucket, key);
		}

		public void Upload(string bucket, string key, Stream stream, ContentEncoding encoding = ContentEncoding.None)
		{
			var tempFileName = Path.GetTempFileName();
			var internalStream = stream;

			if (encoding == ContentEncoding.Gzip)
			{
				using (var tempFile = File.Create(tempFileName))
				using (var gzipStream = new GZipStream(tempFile, CompressionMode.Compress))
				{
					stream.CopyTo(gzipStream);
				}

				internalStream = File.OpenRead(tempFileName);
			}

			var transferRequest = new TransferUtilityUploadRequest()
				.WithAutoCloseStream(true)
				.WithBucketName(bucket)
				.WithKey(key)
				.WithInputStream(internalStream) as TransferUtilityUploadRequest;

			if (encoding != ContentEncoding.None)
			{
				transferRequest.AddHeader("Content-Encoding", encoding.ToString().ToLower());
			}

			this.transferUtility.Upload(transferRequest);

			File.Delete(tempFileName);
		}
	}
}
