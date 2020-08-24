using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace Grpc.TypedExceptions.IntegrationTests
{
    public class CatchExceptionTests : IClassFixture<GreeterApplicationFactory>
    {
        private readonly GreeterApplicationFactory _factory;

        public CatchExceptionTests(GreeterApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CatchesUnaryException()
        {
            var client = _factory.CreateGreeterClient();
            var request = new HelloRequest
            {
                Name = "Alice"
            };

            var exception = await Assert.ThrowsAsync<RpcException<HelloError>>(async () => await client.SayHelloAsync(request));
            Assert.Equal("Foo", exception.Detail.Value);
        }

        [Fact]
        public async Task CatchesServerStreamingException()
        {
            var client = _factory.CreateGreeterClient();
            var request = new HelloRequest
            {
                Name = "Alice"
            };

            var response = client.ServerStreamHello(request);

            await Assert.ThrowsAsync<RpcException<HelloError>>(async () => await response.ResponseStream.MoveNext());
            var exception = await Assert.ThrowsAsync<RpcException<HelloError>>(async () => await response.ResponseStream.MoveNext());
            Assert.Equal("Foo", exception.Detail.Value);
        }

        [Fact]
        public async Task CatchesClientStreamingException()
        {
            var client = _factory.CreateGreeterClient();
            var request = new HelloRequest
            {
                Name = "Alice"
            };

            var call = client.ClientStreamHello();

            await call.RequestStream.WriteAsync(request);
            await call.RequestStream.CompleteAsync();
            
            var exception = await Assert.ThrowsAsync<RpcException<HelloError>>(async () => await call.ResponseAsync);
            Assert.Equal("Foo", exception.Detail.Value);
        }

        [Fact]
        public async Task CatchesDuplexStreamingException()
        {
            var client = _factory.CreateGreeterClient();
            var request = new HelloRequest
            {
                Name = "Alice"
            };

            var call = client.BothStreamHello();
            
            await call.RequestStream.WriteAsync(request);

            var exception = await Assert.ThrowsAsync<RpcException<HelloError>>(call.ResponseStream.MoveNext);
            Assert.Equal("Foo", exception.Detail.Value);
        }
    }
}
