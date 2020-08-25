using System;
using System.Collections.Generic;
using System.Linq;

namespace RendleLabs.Grpc.TypedExceptions
{
    internal static class LargeArray
    {
        public static IEnumerable<byte[]> Chunk(byte[] source, int chunkSize)
        {
            int length = source.Length;
            for (int i = 0; i < length; i+=chunkSize)
            {
                int size = i + chunkSize > length ? length - i : chunkSize;
                var chunk = new byte[size];
                Array.Copy(source, i, chunk, 0, size);
                yield return chunk;
            }
        }

        public static byte[] Combine(IList<byte[]> source)
        {
            var size = source.Sum(a => a.Length);
            var target = new byte[size];

            int index = 0;

            foreach (var chunk in source)
            {
                Array.Copy(chunk, 0, target, index, chunk.Length);
                index += chunk.Length;
            }

            return target;
        }
    }
}