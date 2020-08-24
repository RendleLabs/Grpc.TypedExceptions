using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace RendleLabs.Grpc.TypedExceptions
{
    internal class ClientStreamWriterWrapper<T> : IClientStreamWriter<T>
    {
        private readonly IClientStreamWriter<T> _wrappedWriter;

        public ClientStreamWriterWrapper(IClientStreamWriter<T> wrappedWriter)
        {
            _wrappedWriter = wrappedWriter;
        }

        public async Task WriteAsync(T message)
        {
            try
            {
                await _wrappedWriter.WriteAsync(message);
            }
            catch (RpcException ex)
            {
                var detail = ex.Trailers.FirstOrDefault(e => e.Key == TypedExceptionInterceptor.DetailKey);
                if (!(detail is null))
                {
                    throw RpcExceptionBuilder.Build(ex, detail.ValueBytes);
                }
                throw;
            }
        }

        public WriteOptions WriteOptions
        {
            get => _wrappedWriter.WriteOptions;
            set => _wrappedWriter.WriteOptions = value;
        }

        public Task CompleteAsync() => _wrappedWriter.CompleteAsync();
    }
}