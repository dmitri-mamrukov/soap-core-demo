using SoapCoreDemo.Models;
using SoapCoreDemo.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SoapCoreDemo.Tests
{
    internal class TestServiceClient : ClientBase<ISampleService>
    {
        public TestServiceClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress)
        {
        }

        public string EchoString(string text) => Channel.EchoString(text);

        public MyCustomModel EchoMyCustomModel(MyCustomModel model) => Channel.EchoMyCustomModel(model);
    }

    public class SampleServiceTestWithClientBase
    {
        private readonly TestServiceClient _client;

        public SampleServiceTestWithClientBase()
        {
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(new Uri("http://localhost:50776/Service.asmx"));
            _client = new TestServiceClient(binding, endpoint);
        }

        [Fact]
        public void EchoString()
        {
            var input = "Hello world!";

            var result = _client.EchoString(input);

            Assert.Equal(input, result);
        }

        // TODO
        // For some reason, MyCustomModel is not properly passed to the service method.
        // Deserialization issue due to lack of full support of SOAP in .NET Core?
        [Fact]
        public void EchoMyCustomModel()
        {
            var input = new MyCustomModel
            {
                Id = 1,
                Name = "Jon Doe",
                Email = "test@email.net"
            };

            var result = _client.EchoMyCustomModel(input);

            Assert.Equal(0, result.Id);
            Assert.Equal(null, result.Name);
            Assert.Equal(null, result.Email);
        }
    }

    public struct RequestInfo
    {
        public string Action;
        public string Xml;
    }

    public class Common
    {
        public static readonly int Port = 50776;

        public static string CreateSoapRequest(string body)
        {
            return
                @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                @"<soapenv:Envelope " +
                    @"xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" " +
                    @"xmlns:web=""http://services.com/webservices""> " +
                    @"<soapenv:Header/>" +
                    @"<soapenv:Body>" + body + @"</soapenv:Body>" +
                @"</soapenv:Envelope>";
        }

        public static string CreateSoapResponse(string body)
        {
            return
                @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                @"<s:Envelope " +
                    @"xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" " +
                    @"xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" " +
                    @"xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">" +
                    @"<s:Body>" + body + @"</s:Body>" +
                @"</s:Envelope>";
        }

        public static async Task<List<HttpResponseMessage>> DoRequests(IList<RequestInfo> requests)
        {
            using (var client = new HttpClient())
            {
                var responses = new List<HttpResponseMessage>();
                foreach (var request in requests)
                {
                    var httpContent = new StringContent(request.Xml, Encoding.UTF8, "text/xml");
                    httpContent.Headers.Add("SOAPAction", $"http://services.com/webservices/{request.Action}");
                    var baseUrl = $"http://localhost:{Port}/Service.asmx?op={request.Action}";

                    responses.Add(await client.PostAsync(baseUrl, httpContent));
                }

                return responses;
            }
        }
    }

    public class SampleServiceTestWithHttpClient
    {
        public static async Task<string> DoEchoStringCall(string text)
        {
            var body =
                @"<web:EchoString>" +
                    @"<web:text>" + text + @"</web:text>" +
                @"</web:EchoString>";
            var requests = new List<RequestInfo>
            {
                new RequestInfo
                {
                    Action = "EchoString",
                    Xml = Common.CreateSoapRequest(body)
                }
            };

            var responses = await Common.DoRequests(requests);
            Assert.Equal(1, responses.Count);

            return await responses[0].Content.ReadAsStringAsync();
        }

        public static async Task<string> DoEchoMyCustomModelCall(MyCustomModel model)
        {
            var body =
                @"<web:EchoMyCustomModel>" +
                    @"<web:model>" +
                        @"<web:Id>" + model.Id + @"</web:Id>" +
                        @"<web:Name>" + model.Name + @"</web:Name>" +
                        @"<web:Email>" + model.Email + @"</web:Email>" +
                    @"</web:model>" +
                @"</web:EchoMyCustomModel>";
            var requests = new List<RequestInfo>
            {
                new RequestInfo
                {
                    Action = "EchoMyCustomModel",
                    Xml = Common.CreateSoapRequest(body)
                }
            };

            var responses = await Common.DoRequests(requests);
            Assert.Equal(1, responses.Count);

            return await responses[0].Content.ReadAsStringAsync();
        }

        [Fact]
        public async Task EchoStringAsync()
        {
            var input = "Hello world!";
            var expectedResult =
                @"<EchoStringResponse xmlns=""http://services.com/webservices"">" +
                    @"<EchoStringResult>Hello world!</EchoStringResult>" +
                @"</EchoStringResponse>";
            expectedResult = Common.CreateSoapResponse(expectedResult);

            var result = await DoEchoStringCall(input);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task EchoMyCustomModelAsync()
        {
            var input = new MyCustomModel
            {
                Id = 1,
                Name = "John Doe",
                Email = "test@email.net"
            };
            var expectedResult =
                @"<EchoMyCustomModelResponse xmlns=""http://services.com/webservices"">" +
                    @"<EchoMyCustomModelResult " +
                        @"xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" " +
                        @"xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">" +
                        @"<Id>1</Id>" +
                        @"<Name>John Doe</Name>" +
                        @"<Email>test@email.net</Email>" +
                    @"</EchoMyCustomModelResult>" +
                @"</EchoMyCustomModelResponse>";
            expectedResult = Common.CreateSoapResponse(expectedResult);

            var result = await DoEchoMyCustomModelCall(input);

            Assert.Equal(expectedResult, result);
        }
    }
}