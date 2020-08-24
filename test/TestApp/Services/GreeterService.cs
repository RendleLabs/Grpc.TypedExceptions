using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

#pragma warning disable 1998
        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
#pragma warning restore 1998
        {
            throw new RpcException<HelloError>(new HelloError{ Value = "Foo"}, new Status(StatusCode.Internal, "Foo"));
        }

        public override async Task<HelloReply> ClientStreamHello(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                throw new RpcException<HelloError>(new HelloError{ Value = "Foo"}, new Status(StatusCode.Internal, "Foo"));
            }
            
            throw new RpcException<HelloError>(new HelloError{ Value = "Foo"}, new Status(StatusCode.Internal, "Foo"));
        }

#pragma warning disable 1998
        public override async Task ServerStreamHello(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
#pragma warning restore 1998
        {
            throw new RpcException<HelloError>(new HelloError{ Value = "Foo"}, new Status(StatusCode.Internal, "Foo"));
        }

        public override async Task BothStreamHello(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                throw new RpcException<HelloError>(new HelloError{ Value = "Foo"}, new Status(StatusCode.Internal, "Foo"));
            }
        }
    }
}
