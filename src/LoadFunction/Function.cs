using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LoadFunction.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LoadFunction
{
    public class Function
    {
        private static readonly IConfiguration Configuration;
        private static readonly IAmazonS3 clientS3;

        static Function()
        {
            Configuration = LoadConfiguration();

            if (Configuration.GetSection("aws:AWS_LOCAL").Value == "1")
            {
                var region = Configuration.GetSection("aws:AWS_REGION").Value;
                var accessKeyId = Configuration.GetSection("aws:AWS_ACCESS_KEY_ID").Value;
                var secretAccessKey = Configuration.GetSection("aws:AWS_SECRET_ACCESS_KEY").Value;

                clientS3 = new AmazonS3Client(accessKeyId, secretAccessKey, RegionEndpoint.GetBySystemName(region));
            }
            else
            {
                clientS3 = new AmazonS3Client();
            }
        }

        public async Task<string> FunctionHandler(CloudWatchEventInput input, ILambdaContext context)
        {
            Console.WriteLine($"country: {input.Country}");

            await ReadObjectDataAsync(input.Country);

            return "Ok";
        }

        private static IConfiguration LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            return configuration;
        }

        private async Task ReadObjectDataAsync(string country)
        {
            try
            {
                var bucketName = Configuration.GetSection("S3:bucketName").Value;
                var directory = Configuration.GetSection("S3:directory").Value;
                var filePrefix = $"{directory}/{country}/{country}-homologacion-zonas-virtuales-2020-09-01-v"; // build with the current date.

                Console.WriteLine("bucketName: {0}", bucketName);
                Console.WriteLine("directory: {0}", directory);
                Console.WriteLine("filePrefix: {0}", filePrefix);

                var keyName = await GetFileOfDay(bucketName, filePrefix);

                Console.WriteLine("keyName: {0}", keyName);

                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName
                };

                var file = string.Empty;
                using (GetObjectResponse response = await clientS3.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    file = reader.ReadToEnd(); 
                }

                Console.WriteLine("File read done.");
            }
            catch (AmazonS3Exception e)
            {
                // If bucket or object does not exist
                Console.WriteLine("Error encountered ***. Message:'{0}' when reading object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }
        }

        public async Task<string> GetFileOfDay(string bucket, string filePrefix)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket,
                Prefix = filePrefix,
                MaxKeys = 1
            };

            var listResponse = await clientS3.ListObjectsAsync(request);

            var keyName = string.Empty;

            foreach (S3Object obj in listResponse.S3Objects)
            {
                keyName = obj.Key;
            }

            return keyName;
        }
    }
}
