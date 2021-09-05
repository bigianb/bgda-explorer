using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WorldExplorer.Tools
{
    public readonly struct Location : IComparable, IComparable<Location>, IEquatable<Location>
    {
        public readonly long CharOffset, Line, Column;

        public Location(long charOffset, long line, long column)
        {
            CharOffset = charOffset;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return Line + "(" + Column + ")";
        }

        public static Location operator -(Location location, int value)
        {
            return new Location(location.CharOffset - value, location.Line, location.Column - value);
        }

        public static Location operator +(Location location, int value)
        {
            return new Location(location.CharOffset + value, location.Line, location.Column + value);
        }

        public static bool operator >(Location a, Location b)
        {
            return a.CharOffset > b.CharOffset;
        }

        public static bool operator <(Location a, Location b)
        {
            return a.CharOffset < b.CharOffset;
        }

        public static bool operator >=(Location a, Location b)
        {
            return a.CharOffset >= b.CharOffset;
        }

        public static bool operator <=(Location a, Location b)
        {
            return a.CharOffset <= b.CharOffset;
        }

        public static bool operator ==(Location a, Location b)
        {
            return a.CharOffset == b.CharOffset;
        }

        public static bool operator !=(Location a, Location b)
        {
            return a.CharOffset != b.CharOffset;
        }

        public long DistanceTo(Location loc)
        {
            return DistanceBetween(this, loc);
        }

        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (!(value is Location loc))
            {
                throw new ArgumentException($"Value must have the type {typeof(Location).FullName}", nameof(value));
            }

            if (this < loc)
            {
                return -1;
            }

            if (this > loc)
            {
                return 1;
            }

            return 0;
        }

        public int CompareTo(Location loc)
        {
            if (this < loc)
            {
                return -1;
            }

            if (this > loc)
            {
                return 1;
            }

            return 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Location loc)
            {
                return false;
            }

            return this == loc;
        }

        public bool Equals(Location other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return CharOffset.GetHashCode();
        }

        public static long DistanceBetween(Location a, Location b)
        {
            return a.CharOffset - b.CharOffset;
        }
    }

    public readonly struct TextBounds : IEquatable<TextBounds>
    {
        public readonly Location Location;
        public readonly long Length;

        public long Offset => Location.CharOffset;

        public long EndOffset => Location.CharOffset + Length;

        public TextBounds(Location location, long length)
        {
            Location = location;
            Length = length;
        }

        public bool Contains(Location loc)
        {
            return Location < loc && EndOffset > loc.CharOffset;
        }

        public bool Contains(long charOffset)
        {
            return Location.CharOffset <= charOffset && EndOffset >= charOffset;
        }

        public bool Contains(TextBounds bounds)
        {
            return Location <= bounds.Location && EndOffset >= bounds.EndOffset;
        }

        public bool Intersects(TextBounds bounds)
        {
            if (Location.CharOffset <= bounds.Location.CharOffset && EndOffset > bounds.Location.CharOffset)
            {
                return true;
            }

            if (bounds.Location.CharOffset <= Location.CharOffset && bounds.EndOffset > Location.CharOffset)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Location + " " + Length;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TextBounds bounds)
            {
                return false;
            }

            return this == bounds;
        }

        public bool Equals(TextBounds other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ Length.GetHashCode();
        }

        public static TextBounds operator +(TextBounds v1, TextBounds v2)
        {
            return new TextBounds(v1.Location, v2.EndOffset - v1.Location.CharOffset);
        }

        public static bool operator ==(TextBounds a, TextBounds b)
        {
            return a.Location == b.Location && a.Length == b.Length;
        }

        public static bool operator !=(TextBounds a, TextBounds b)
        {
            return a.Location != b.Location || a.Length != b.Length;
        }
    }

    public class Reader : IDisposable
    {
        private TextReader _baseReader;
        private bool _disposed;

        public long CharOffset { get; private set; }

        public long Line { get; private set; }

        public long Column { get; private set; }

        public long Length { get; private set; }

        public long AmountLeft => Length - CharOffset;
        public Location Location => new(CharOffset, Line, Column);

        public Reader(TextReader baseReader, long length)
        {
            Reset();
            Length = length;
            _baseReader = baseReader;
        }

        public Reader(Stream input)
        {
            Reset();
            Length = input.Length;

            // Detect preamble length
            var encodings = Encoding.GetEncodings();
            List<Encoding> testedEncodings = new();
            foreach (var enc in encodings)
            {
                if (!testedEncodings.Contains(enc.GetEncoding()))
                {
                    testedEncodings.Add(enc.GetEncoding());
                    var preamble = enc.GetEncoding().GetPreamble();

                    if (preamble.Length == 0 || preamble.Length > input.Length)
                    {
                        continue;
                    }

                    input.Position = 0;

                    var testPreamble = new byte[preamble.Length];
                    input.Read(testPreamble, 0, testPreamble.Length);
                    input.Position = 0;

                    // Find testPreamble in preamble
                    var match = !preamble.Where((t, i) => testPreamble[i] != t).Any();

                    if (match)
                    {
                        Length -= testPreamble.Length;
                        break;
                    }
                }
            }

            StreamReader reader = new(input);

            _baseReader = reader;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _baseReader.Dispose();
            _disposed = true;
        }

        public char Read(bool throwEx = true)
        {
            var iValue = _baseReader.Read();

            if (iValue == -1)
            {
                if (throwEx)
                {
                    throw new EndOfStreamException("Received a negative 1, means end of stream");
                }

                return unchecked((char)-1);
            }

            var value = (char)iValue;
            CharOffset++;
            Column++;

            // Turn \r's into \n's
            if (value == '\r')
            {
                if (_baseReader.Peek() == '\n')
                {
                    return Read();
                }

                value = '\n';
            }

            // Goto next line if value is a \n
            if (value == '\n')
            {
                Line++;
                Column = 1;
            }

            return value;
        }

        public char Peek(bool throwEx = true)
        {
            var iValue = _baseReader.Peek();
            if (iValue == -1)
            {
                if (throwEx)
                {
                    throw new EndOfStreamException();
                }

                return unchecked((char)-1);
            }

            var value = (char)iValue;
            if (value == '\r')
            {
                value = '\n';
            }

            return value;
        }

        public string ReadLine()
        {
            StringBuilder builder = new();
            var c = Read(false);
            while (c != '\n' && AmountLeft > 0)
            {
                builder.Append(c);
                c = Read(false);
            }

            return builder.ToString();
        }

        private void Reset()
        {
            Length = 0;
            CharOffset = 0;
            Line = Column = 1;
        }
    }
}