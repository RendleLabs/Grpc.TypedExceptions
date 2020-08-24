using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace RendleLabs.Grpc.TypedExceptions
{
    public class TypedExceptionInterceptor : Interceptor
    {
        public static readonly string DetailKey = $"__rpc_exception_detail{Metadata.BinaryHeaderSuffix}";
        
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.UnaryServerHandler(request, context, continuation);
            }
            catch (RpcException ex)
            {
                if (ex is IRpcExceptionDetail detail)
                {
                    var metadata = new Metadata();
                    foreach (var trailer in ex.Trailers)
                    {
                        metadata.Add(trailer);
                    }
                    metadata.Add(DetailKey, detail.DetailBytes());
                    throw new RpcException(ex.Status, metadata, ex.Message);
                }

                throw;
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.ClientStreamingServerHandler(requestStream, context, continuation);
            }
            catch (RpcException ex)
            {
                if (ex is IRpcExceptionDetail detail)
                {
                    var metadata = new Metadata();
                    foreach (var trailer in ex.Trailers)
                    {
                        metadata.Add(trailer);
                    }
                    metadata.Add(DetailKey, detail.DetailBytes());
                    throw new RpcException(ex.Status, metadata, ex.Message);
                }

                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            }
            catch (RpcException ex)
            {
                if (ex is IRpcExceptionDetail detail)
                {
                    var metadata = new Metadata();
                    foreach (var trailer in ex.Trailers)
                    {
                        metadata.Add(trailer);
                    }
                    metadata.Add(DetailKey, detail.DetailBytes());
                    throw new RpcException(ex.Status, metadata, ex.Message);
                }

                throw;
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            }
            catch (RpcException ex)
            {
                if (ex is IRpcExceptionDetail detail)
                {
                    var metadata = new Metadata();
                    foreach (var trailer in ex.Trailers)
                    {
                        metadata.Add(trailer);
                    }
                    metadata.Add(DetailKey, detail.DetailBytes());
                    throw new RpcException(ex.Status, metadata, ex.Message);
                }

                throw;
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                return base.BlockingUnaryCall(request, context, continuation);
            }
            catch (RpcException ex)
            {
                if (ex is IRpcExceptionDetail detail)
                {
                    var metadata = new Metadata();
                    foreach (var trailer in ex.Trailers)
                    {
                        metadata.Add(trailer);
                    }
                    metadata.Add(DetailKey, detail.DetailBytes());
                    throw new RpcException(ex.Status, metadata, ex.Message);
                }

                throw;
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, context);

            var responseTask = Handle(call);
            
            return new AsyncUnaryCall<TResponse>(responseTask, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        private static async Task<TResponse> Handle<TResponse>(AsyncUnaryCall<TResponse> call)
        {
            try
            {
                await call.ResponseHeadersAsync;
                return await call.ResponseAsync;
            }
            catch (RpcException ex)
            {
                var detail = ex.Trailers.FirstOrDefault(e => e.Key == DetailKey);
                if (!(detail is null))
                {
                    throw RpcExceptionBuilder.Build(ex, detail.ValueBytes);
                }
                throw;
            }
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(context);
            
            var wrapped = new ClientStreamWriterWrapper<TRequest>(call.RequestStream);
            
            var responseTask = Handle(call);
            
            return new AsyncClientStreamingCall<TRequest, TResponse>(wrapped, responseTask, _ => call.ResponseHeadersAsync,
                _ => call.GetStatus(), _ => call.GetTrailers(), _ => call.Dispose(), null);
        }

        private static async Task<TResponse> Handle<TRequest, TResponse>(AsyncClientStreamingCall<TRequest, TResponse> call)
        {
            try
            {
                await call.ResponseHeadersAsync;
                return await call.ResponseAsync;
            }
            catch (RpcException ex)
            {
                var detail = ex.Trailers.FirstOrDefault(e => e.Key == DetailKey);
                if (!(detail is null))
                {
                    throw RpcExceptionBuilder.Build(ex, detail.ValueBytes);
                }
                throw;
            }
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, context);
            return new AsyncServerStreamingCall<TResponse>(new AsyncStreamReaderWrapper<TResponse>(call.ResponseStream), call.ResponseHeadersAsync,
                call.GetStatus, call.GetTrailers, call.Dispose);
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(context);
            
            var clientWrapper = new ClientStreamWriterWrapper<TRequest>(call.RequestStream);
            var streamWrapper = new AsyncStreamReaderWrapper<TResponse>(call.ResponseStream);
            
            return new AsyncDuplexStreamingCall<TRequest, TResponse>(clientWrapper, streamWrapper, _ => call.ResponseHeadersAsync,
                _ => call.GetStatus(), _ => call.GetTrailers(), _ => call.Dispose(), null);
        }
    }
}