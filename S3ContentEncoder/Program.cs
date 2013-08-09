using CommandLine;
using CommandLine.Text;
using System;
using System.Threading.Tasks;

namespace S3ContentEncoder
{
	internal class Options
	{
		[Option('a', "accessKey", Required = true)]
		public string AccessKey { get; set; }

		[Option('s', "secretKey", Required = true)]
		public string SecretKey { get; set; }

		[Option('b', "bucket", Required = true)]
		public string Bucket { get; set; }

		[Option('p', "prefix", Required = true)]
		public string Prefix { get; set; }

		[Option('e', "encoding", DefaultValue = "gzip")]
		public string Encoding { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	internal class Program
	{
		static void Main(string[] args)
		{
			var options = new Options();

			if (Parser.Default.ParseArguments(args, options))
			{
				ContentEncoding encoding;
				if (!Enum.TryParse(options.Encoding, true, out encoding))
				{
					Console.WriteLine("Invalid encoding specified");
					return;
				}

				Console.WriteLine("Encoding all files under {0}/{1} with {2}...", options.Bucket, options.Prefix, encoding);

				var s3Facade = new S3Facade(options.AccessKey, options.SecretKey);

				Parallel.ForEach(s3Facade.ListAllObjects(options.Bucket, options.Prefix), key =>
				{
					if (!s3Facade.HasEncoding(encoding, options.Bucket, key))
					{
						Console.WriteLine("Encoding file {0}...", key);
						s3Facade.Upload(options.Bucket, key, s3Facade.Download(options.Bucket, key), encoding);
					}
				});

				Console.WriteLine("Complete!");
			}
		}
	}
}
