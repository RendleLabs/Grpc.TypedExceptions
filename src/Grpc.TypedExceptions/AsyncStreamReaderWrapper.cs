using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace RendleLabs.Grpc.TypedExceptions
{
    internal class AsyncStreamReaderWrapper<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _wrappedReader;

        public AsyncStreamReaderWrapper(IAsyncStreamReader<T> wrappedReader)
        {
            _wrappedReader = wrappedReader;
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                return await _wrappedReader.MoveNext(cancellationToken);
            }
            catch (RpcException ex)
            {
                if (RpcExceptionBuilder.Build(ex, out var rpcException))
                {
                    throw rpcException;
                }

                throw;
            }
        }

        public T Current => _wrappedReader.Current;
    }
}