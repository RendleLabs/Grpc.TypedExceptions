using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Any = Google.Protobuf.WellKnownTypes.Any;

namespace RendleLabs.Grpc.TypedExceptions
{
    internal static class RpcExceptionBuilder
    {
        private static readonly object Sync = new object();
        private static Dictionary<string, Func<RpcException, ByteString, RpcException>>? _typeDictionary;
        private static readonly Func<Type, bool> IsMessageType = typeof(IMessage).IsAssignableFrom;

        public static bool Build(RpcException baseException, [NotNullWhen(true)]out RpcException? exception)
        {
            var trailers = baseException.Trailers;
            
            var detail = trailers.GetValueBytes(TypedExceptionInterceptor.DetailKey);
            
            if (!(detail is null)) return TryBuild(baseException, detail, out exception);

            var detailChunkCountStr = trailers.GetValue(TypedExceptionInterceptor.DetailChunkCount);
            if (!(detailChunkCountStr is null) && int.TryParse(detailChunkCountStr, out int detailChunkCount))
            {
                var chunks = new List<byte[]>(detailChunkCount);
                for (int i = 0; i < detailChunkCount; i++)
                {
                    var key = string.Format(TypedExceptionInterceptor.DetailChunkKey, i);
                    var chunk = trailers.GetValueBytes(key);
                    if (chunk is null)
                    {
                        exception = default;
                        return false;
                    }
                    chunks.Add(chunk);
                }

                detail = LargeArray.Combine(chunks);

                return TryBuild(baseException, detail, out exception);
            }

            exception = default;
            return false;
        }
        
        private static bool TryBuild(RpcException baseException, byte[] data, [NotNullWhen(true)]out RpcException? exception)
        {
            var any = Any.Parser.ParseFrom(data);
            var fullTypeName = Any.GetTypeName(any.TypeUrl);

            if (string.IsNullOrEmpty(fullTypeName)) fullTypeName = any.TypeUrl;

            var typeDictionary = TypeDictionary();

            if (typeDictionary.TryGetValue(fullTypeName, out var factory))
            {
                exception = factory(baseException, any.Value);
                return true;
            }

            exception = default;
            return false;
        }

        private static Dictionary<string, Func<RpcException, ByteString, RpcException>> TypeDictionary()
        {
            if (!(_typeDictionary is null)) return _typeDictionary;
            
            lock (Sync)
            {
                return _typeDictionary ??= CreateTypeDictionary();
            }
        }

        private static Dictionary<string, Func<RpcException, ByteString, RpcException>> CreateTypeDictionary()
        {
            var typeDictionary = new Dictionary<string, Func<RpcException, ByteString, RpcException>>();

            var messageTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.ExportedTypes)
                .Where(IsMessageType);

            foreach (var messageType in messageTypes)
            {
                var descriptorProperty = messageType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                
                if (descriptorProperty?.GetValue(null) is MessageDescriptor descriptor)
                {
                    if (typeDictionary.ContainsKey(descriptor.FullName)) continue;
                    
                    if (TryCreateFunc(messageType, out var func))
                    {
                        typeDictionary.Add(descriptor.FullName, func);
                    }
                }
            }

            return typeDictionary;
        }

        private static bool TryCreateFunc(Type messageType, [NotNullWhen(true)] out Func<RpcException, ByteString, RpcException>? func)
        {
            func = null;
            
            var parser = messageType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var parseFrom = parser?.GetType().GetMethod("ParseFrom", new[] {typeof(ByteString)});
            if (parseFrom is null) return false;

            func = CreateFunc(messageType, parseFrom, parser!);
            
            return true;
        }

        private static Func<RpcException, ByteString, RpcException> CreateFunc(Type messageType, MethodInfo parseFrom, object parser)
        {
            var exceptionType = typeof(RpcException<>).MakeGenericType(messageType);

            RpcException Create(RpcException original, ByteString detailBytes)
            {
                var detail = parseFrom.Invoke(parser, new object[] {detailBytes});
                try
                {
                    return CreateInstance(exceptionType, detail, original) ?? original;
                }
                catch
                {
                    return original;
                }
            }

            return Create;
        }

        private static RpcException? CreateInstance(Type exceptionType, object detail, RpcException baseException) =>
            Activator.CreateInstance(exceptionType, detail, baseException.Status, baseException.Trailers, baseException.Message) as RpcException;
    }
}