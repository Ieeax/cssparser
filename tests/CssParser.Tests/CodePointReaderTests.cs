using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Leeax.Parsing.CSS.Tests
{
    public class CodePointReaderTests
    {
        [Fact]
        public void ReconsumeTest()
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes("a")));

            Assert.True(reader.PositionDelta == 0);

            reader.Read();

            Assert.True(reader.PositionDelta == 0);

            // Cursor should move (1) backwards
            reader.MoveBackward(1);

            Assert.True(reader.PositionDelta == 1);
        }

        [Theory]
        [InlineData("abc", 2, 2)]
        [InlineData("abc", 5, 3)]
        public void ReadMultipleTest(string value, int countToRead, int expectedReadCount)
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value)));

            var result = reader.ReadMultiple(countToRead);

            Assert.True(result.Length == expectedReadCount);
        }

        [Theory]
        [InlineData('a', 'a')]
        [InlineData('\r', '\n')]
        [InlineData('\f', '\n')]
        [InlineData('\n', '\n')]
        public void ReadNextTest(char value, int expectedValue)
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value.ToString())));

            var codePoint = reader.Read();

            Assert.True(codePoint == expectedValue);
            Assert.True(reader.EndOfStream);
        }

        [Theory]
        [InlineData('a', 'a')]
        [InlineData('\r', '\n')]
        [InlineData('\f', '\n')]
        [InlineData('\n', '\n')]
        public void ReadNextOutTest(char value, char expectedValue)
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value.ToString())));

            // Read the next code point and compare with expected
            Assert.True(reader.Read(out int codePoint));
            Assert.True(codePoint == expectedValue);

            // Method should return false (reached end of stream)
            Assert.False(reader.Read(out codePoint));

            Assert.True(codePoint == -1);
            Assert.True(reader.EndOfStream);
        }

        [Theory]
        [InlineData('a', 'a')]
        [InlineData('\r', '\n')]
        [InlineData('\f', '\n')]
        [InlineData('\n', '\n')]
        public void PeekNextTest(char value, int expectedValue)
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value.ToString())));

            Assert.True(reader.PositionDelta == 0);

            var codePoint = reader.Peek();

            // Position delta should be 1
            Assert.True(reader.PositionDelta == 1);
            Assert.True(codePoint == expectedValue);

            // Do the same again, to ensure the delta-position works as expected
            codePoint = reader.Peek();

            // Position delta should be 1
            Assert.True(reader.PositionDelta == 1);
            Assert.True(codePoint == expectedValue);
        }

        [Theory]
        [InlineData('a', 'a')]
        [InlineData('\r', '\n')]
        [InlineData('\f', '\n')]
        [InlineData('\n', '\n')]
        public void PeekNextOutTest(char value, char expectedValue)
        {
            var reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value.ToString())));

            Assert.True(reader.Peek(out int codePoint));

            // Position delta should be 1
            Assert.True(reader.PositionDelta == 1);
            Assert.True(codePoint == expectedValue);

            // Set cursor to end of stream
            reader.MoveForward(1);

            // Method should return false
            Assert.False(reader.Peek(out codePoint));

            Assert.True(codePoint == -1);
            Assert.True(reader.EndOfStream);
        }
    }
}