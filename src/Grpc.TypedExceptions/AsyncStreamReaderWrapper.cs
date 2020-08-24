using System;
using System.Linq;
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
                var detail = ex.Trailers.FirstOrDefault(e => e.Key == TypedExceptionInterceptor.DetailKey);
                if (!(detail is null))
                {
                    throw RpcExceptionBuilder.Build(ex, detail.ValueBytes);
                }
                throw;
            }
        }

        public T Current => _wrappedReader.Current;
    }
}