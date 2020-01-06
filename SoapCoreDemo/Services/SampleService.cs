using SoapCoreDemo.Models;

namespace SoapCoreDemo.Services
{
    public class SampleService : ISampleService
    {
        public string EchoString(string text)
        {
            return text;
        }

        public MyCustomModel EchoMyCustomModel(MyCustomModel model)
        {
            return model;
        }
    }
}