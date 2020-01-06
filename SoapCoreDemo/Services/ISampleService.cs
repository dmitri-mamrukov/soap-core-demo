using SoapCoreDemo.Models;
using System.ServiceModel;

namespace SoapCoreDemo.Services
{
    [ServiceContract(Namespace = "http://services.com/webservices")]
    public interface ISampleService
    {
        [OperationContract]
        string EchoString(string text);

        [OperationContract]
        MyCustomModel EchoMyCustomModel(MyCustomModel model);
    }
}