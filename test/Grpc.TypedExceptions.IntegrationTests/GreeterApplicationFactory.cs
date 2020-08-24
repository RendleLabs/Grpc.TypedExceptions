using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using RendleLabs.Grpc.TypedExceptions;
using TestApp;

namespace Grpc.TypedExceptions.IntegrationTests
{
    public class GreeterApplicationFactory : WebApplicationFactory<Startup>
    {
        public Greeter.GreeterClient CreateGreeterClient()
        {
            var invoker = this.CreateGrpcChannel();
            return new Greeter.GreeterClient(invoker);
        }

        private CallInvoker CreateGrpcChannel()
        {
            var client = CreateDefaultClient(new ResponseVersionHandler());
            return GrpcChannel.ForAddress(client.BaseAddress, new GrpcChannelOptions
                {
                    HttpClient = client
                })
                .Intercept(new TypedExceptionInterceptor());
        }

        private class ResponseVersionHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                response.Version = request.Version;

                return response;
            }
        }
    }
}