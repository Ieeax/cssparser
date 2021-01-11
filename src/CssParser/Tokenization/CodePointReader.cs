using System;
using System.IO;
using System.Text;

namespace Leeax.Parsing.CSS
{
    public class CodePointReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly char[] _buffer;
        private int _positionDelta; // 1-based
        private int _bufferOccupation;

        public CodePointReader(Stream stream)
            : this(stream, 5)
        {
        }

        public CodePointReader(Stream stream, int bufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size have to be 0 or higher.");
            }

            _reader = new StreamReader(_stream, Encoding.UTF8);
            _buffer = new char[bufferSize];
        }

        private int PreprocessCodePoint(int codePoint)
        {
            // Replace CARRIAGE RETURN and FORM FEED by LINE FEED
            if (codePoint == Constants.CARRIAGE_RETURN
                || codePoint == Constants.FORM_FEED)
            {
                return Constants.LINE_FEED;
            }

            // Replace NULL (0x0000) or surrogate code points with REPLACEMENT CHARACTER (0xFFFD)
            if (codePoint == Constants.NULL
                || (codePoint >= Constants.SURROGATE_LOWER && codePoint <= Constants.SURROGATE_UPPER))
            {
                return Constants.REPLACEMENT_CHARACTER;
            }

            return codePoint;
        }

        #region Internal reading methods
        private int ReadInternal()
        {
            if (_positionDelta > 0)
            {
                // Return the char in the buffer and update delta
                return _buffer[_positionDelta-- - 1];
            }

            var value = _reader.Read();

            if (value == Constants.EOF)
            {
                return value;
            }

            // Shift chars by factor 1 to the back, to make space for the new one
            for (int i = _buffer.Length - 1; i > 0; i--)
            {
                _buffer[i] = _buffer[i - 1];
            }

            // Set the newly read char to the buffer
            _buffer[0] = (char)PreprocessCodePoint(value);

            // Increment buffer occupation
            if (_bufferOccupation != _buffer.Length)
            {
                _bufferOccupation++;
            }

            return _buffer[0];
        }

        private char[] ReadMultipleInternal(int count)
        {
            if (count <= 0)
            {
                return new char[0];
            }

            char[] tempBuffer;
            int countRead;

            // If a delta exists we need to return the items in the buffer first
            if (_positionDelta > 0)
            {
                char[] result;

                // Check if the current delta is enough to get the requested 
                // count completely from the buffer
                if (_positionDelta >= count)
                {
                    result = new char[count];

                    // Copy from buffer into result
                    Array.Copy(_buffer, _positionDelta - count, result, 0, count);

                    // Update delta
                    _positionDelta -= count;

                    return result;
                }

                // Determine the missing count which we need to read from the stream
                var missingCount = count - _positionDelta;

                tempBuffer = new char[missingCount];
                countRead = _reader.Read(tempBuffer, 0, missingCount);

                // Create the result array since we know the real size now
                result = new char[_positionDelta + countRead];
                
                // Take the delta amount from our buffer
                for (int i = 0; i < _positionDelta; i++)
                {
                    result[i] = _buffer[i];
                }

                ShiftBufferByCount(countRead);

                // Pre-process all chars and assign to the result
                for (int i = 0; i < countRead; i++)
                {
                    result[_positionDelta + i] = (char)PreprocessCodePoint(tempBuffer[i]);
                }

                // Set the chars from result to the buffer
                CopyToBuffer(result, countRead, _positionDelta);

                _positionDelta = 0;

                // Increment buffer occupation
                if (_bufferOccupation != _buffer.Length)
                {
                    _bufferOccupation = (_bufferOccupation + countRead > _buffer.Length)
                        ? _buffer.Length
                        : _bufferOccupation + countRead;
                }

                return result;
            }

            tempBuffer = new char[count];
            countRead = _reader.Read(tempBuffer, 0, count);

            if (countRead == 0)
            {
                return new char[0];
            }

            ShiftBufferByCount(countRead);

            // Resize the array, so we dont need to return the array and the read-count
            if (countRead != count)
            {
                Array.Resize(ref tempBuffer, countRead);
            }

            // Pre-process all chars
            for (int i = 0; i < countRead; i++)
            {
                tempBuffer[i] = (char)PreprocessCodePoint(tempBuffer[i]);
            }

            // Set the read chars from stream to the buffer
            CopyToBuffer(tempBuffer, countRead, 0);

            // Increment buffer occupation
            if (_bufferOccupation != _buffer.Length)
            {
                _bufferOccupation = (_bufferOccupation + countRead > _buffer.Length)
                    ? _buffer.Length
                    : _bufferOccupation + countRead;
            }

            return tempBuffer;
        }

        private void CopyToBuffer(char[] sourceArray, int countToCopy, int startIndex)
        {
            for (int i = 0; i < countToCopy; i++)
            {
                // Loops over the amount of items to insert and inverts the order of the items in the source array
                // e.g. bufferSize=5, countRead=3
                //
                //  > buffer[2] = read[0];
                //  > buffer[1] = read[1];
                //  > buffer[0] = read[2];
                //
                // Required because the array which we read (from stream) is in the reversed order 
                // as we save it in our buffer
                _buffer[(countToCopy - 1 - i)] = sourceArray[startIndex + i];
            }
        }

        private void ShiftBufferByCount(int itemsToInsert)
        {
            // Check whether we need to shift any item when we insert the given amount of items
            // at the start of the buffer
            if (_buffer.Length - itemsToInsert > 0)
            {
                // If so, update position of each item which is not getting replaced, beginning by the last
                // e.g. bufferSize=5, itemsToInsert=2
                //
                //  > buffer[4] = buffer[2];
                //  > buffer[3] = buffer[1];
                //  > buffer[2] = buffer[0];
                //
                // Index 1 and 0 can be ignored, because they will be replaced
                for (int i = _buffer.Length - 1; i >= itemsToInsert; i--)
                {
                    _buffer[i] = _buffer[i - itemsToInsert];
                }
            }
        }
        #endregion

        #region Seek methods
        /// <summary>
        /// Reconsumes the last x chars from the internal buffer.
        /// </summary>
        /// <param name="count">Amount of chars to reconsume.</param>
        public bool MoveBackward(int count)
        {
            if (count == 0)
            {
                return true;
            }

            if (_bufferOccupation >= _positionDelta + count)
            {
                _positionDelta += count;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes the next x chars from the buffer or the stream.
        /// </summary>
        /// <param name="count">Amount of chars to consume.</param>
        public void MoveForward(int count)
        {
            if (count == 0)
            {
                return;
            }

            // Temporary solution
            // TODO: Copy? logic and optimize it, e.g. we don't need to return an array (no resize required ...) 
            ReadMultipleInternal(count);
        }
        #endregion

        #region Peek methods
        public int Peek()
        {
            Peek(out int value);
            return value;
        }

        public bool Peek(out int value)
        {
            value = ReadInternal();

            if (value != Constants.EOF)
            {
                MoveBackward(1);
                return true;
            }

            return false;
        }

        public bool Peek(out char value)
        {
            var result = Peek(out int intValue);
            value = (char)intValue;

            return result;
        }

        public char[] PeekMultiple(int count)
        {
            var result = ReadMultipleInternal(count);
            MoveBackward(result.Length);

            return result;
        }
        #endregion

        #region Read methods
        public int Read()
        {
            return ReadInternal();
        }

        public bool Read(out int value)
        {
            value = ReadInternal();
            return value != Constants.EOF;
        }

        public bool Read(out char value)
        {
            value = (char)ReadInternal();
            return value != Constants.EOF;
        }

        public char[] ReadMultiple(int count)
        {
            return ReadMultipleInternal(count);
        }
        #endregion
        
        public void Dispose()
        {
            _stream.Dispose();
            _reader.Dispose();
        }

        public int PositionDelta => _positionDelta;

        public bool EndOfStream => _reader.EndOfStream;
    }
}