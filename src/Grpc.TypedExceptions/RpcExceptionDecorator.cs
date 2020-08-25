using Grpc.Core;

namespace RendleLabs.Grpc.TypedExceptions
{
    internal static class RpcExceptionDecorator
    {
        private const int ChunkSize = 16000; // Max header size is 16384, go smaller
        private static readonly string DetailKey = $"__rpc_exception_detail{Metadata.BinaryHeaderSuffix}";
        private static readonly string DetailChunkCount = $"__rpc_exception_detail_chunk_count";
        private static readonly string DetailChunkKey = $"__rpc_exception_detail_{{0}}{Metadata.BinaryHeaderSuffix}";
        
        public static RpcException Decorate(RpcException exception)
        {
            var metadata = new Metadata();
            foreach (var trailer in exception.Trailers)
            {
                metadata.Add(trailer);
            }

            var detailBytes = ((IRpcExceptionDetail) exception).DetailBytes();
            var length = detailBytes.Length;

            if (length < ChunkSize)
            {
                metadata.Add(DetailKey, detailBytes);
            }
            else
            {
                // Hopefully this will never get used.
                int i = 0;
                
                foreach (var chunk in LargeArray.Chunk(detailBytes, ChunkSize))
                {
                    var key = string.Format(DetailChunkKey, i);
                    metadata.Add(key, chunk);
                    i++;
                }
                
                metadata.Add(DetailChunkCount, i.ToString());
            }
            
            metadata.Add(DetailKey, ((IRpcExceptionDetail)exception).DetailBytes());
            
            throw new RpcException(exception.Status, metadata, exception.Message);
        }
    }
}