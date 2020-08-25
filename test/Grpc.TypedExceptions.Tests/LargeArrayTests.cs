using System;
using System.Linq;
using RendleLabs.Grpc.TypedExceptions;
using Xunit;

namespace Grpc.TypedExceptions.Tests
{
    public class LargeArrayTests
    {
        [Fact]
        public void Chunks()
        {
            var source = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14};

            var actuals = LargeArray.Chunk(source, 4).ToList();
            
            Assert.Equal(4, actuals.Count);
            Assert.Equal(4, actuals[0].Length);
            Assert.Equal(4, actuals[1].Length);
            Assert.Equal(4, actuals[2].Length);
            Assert.Equal(2, actuals[3].Length);
            
            Assert.Equal(1, actuals[0][0]);
            Assert.Equal(2, actuals[0][1]);
            Assert.Equal(3, actuals[0][2]);
            Assert.Equal(4, actuals[0][3]);
            
            Assert.Equal(5, actuals[1][0]);
            Assert.Equal(6, actuals[1][1]);
            Assert.Equal(7, actuals[1][2]);
            Assert.Equal(8, actuals[1][3]);
            
            Assert.Equal(9, actuals[2][0]);
            Assert.Equal(10, actuals[2][1]);
            Assert.Equal(11, actuals[2][2]);
            Assert.Equal(12, actuals[2][3]);
            
            Assert.Equal(13, actuals[3][0]);
            Assert.Equal(14, actuals[3][1]);
        }

        [Fact]
        public void Combines()
        {
            var source = new[]
            {
                new byte[] {1, 2, 3, 4},
                new byte[] {5, 6, 7, 8},
                new byte[] {9, 10},
            };

            var expected = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};

            var actual = LargeArray.Combine(source);
            
            Assert.Equal(expected, actual);
        }
    }
}
