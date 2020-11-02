using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Xunit;
using Amazon.Lambda.TestUtilities;

namespace LoadFunction.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async Task TestHelloWorldFunctionHandler()
        {
            var context = new TestLambdaContext();
            var request = new Input.CloudWatchEventInput
            {
                Country = "PE"
            };

            var function = new Function();
            var response = await function.FunctionHandler(request, context);

            Console.WriteLine("Lambda Response: \n" + response);

            Assert.Equal("Ok", response);
        }
    }
}