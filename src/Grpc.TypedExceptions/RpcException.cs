using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using RendleLabs.Grpc.TypedExceptions;

// ReSharper disable once CheckNamespace
namespace Grpc.Core
{
    public class RpcException<T> : RpcException, IRpcExceptionDetail where T : IMessage
    {
        public T Detail { get; }

        public RpcException(T detail, Status status) : base(status)
        {
            Detail = detail;
        }

        public RpcException(T detail, Status status, string message) : base(status, message)
        {
            Detail = detail;
        }

        public RpcException(T detail, Status status, Metadata trailers) : base(status, trailers)
        {
            Detail = detail;
        }

        public RpcException(T detail, Status status, Metadata trailers, string message) : base(status, trailers, message)
        {
            Detail = detail;
        }

        public byte[] DetailBytes() => new Any
        {
            Value = Detail.ToByteString(),
            TypeUrl = Detail.Descriptor.FullName
        }.ToByteArray();
    }
}
