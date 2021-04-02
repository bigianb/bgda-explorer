using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WorldExplorer.Tools
{
	public struct Location : IComparable, IComparable<Location>, IEquatable<Location>
	{
		public long CharOffset, Line, Column;

		public Location(long charOffset, long line, long column)
		{
			CharOffset = charOffset; Line = line; Column = column;
		}

		public override string ToString()
		{
			return Line + "(" + Column + ")";
		}

		public static Location operator -(Location location, int value)
		{
			location.Column -= value;
			location.CharOffset -= value;
			return location;
		}
		public static Location operator +(Location location, int value)
		{
			location.Column += value;
			location.CharOffset += value;
			return location;
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

		public int CompareTo(object value)
		{
			if (value == null)
				return 1;
			if (!(value is Location))
				throw new ArgumentException("Value must have the type " + typeof(Location).FullName, "value");

			var loc = (Location)value;

			if (this < loc)
				return -1;
			if (this > loc)
				return 1;

			return 0;
		}
		public int CompareTo(Location loc)
		{
			if (this < loc)
				return -1;
			if (this > loc)
				return 1;

			return 0;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Location))
				return false;
			var loc = (Location)obj;
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
	public struct TextBounds : IEquatable<TextBounds>
	{
		public readonly Location Location;
		public readonly long Length;

        public long Offset
        {
            get { return Location.CharOffset; }
        }

		public long EndOffset
		{
			get
			{
				return Location.CharOffset + Length;
			}
		}

		public TextBounds(Location location, long length)
		{
			Location = location;
			Length = length;
		}

		public bool Contains(Location loc)
		{
			return (Location < loc && EndOffset > loc.CharOffset);
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
                return true;
            if (bounds.Location.CharOffset <= Location.CharOffset && bounds.EndOffset > Location.CharOffset)
                return true;
            return false;
        }

		public override string ToString()
		{
			return Location + " " + Length;

		}
		public override bool Equals(object obj)
		{
			if (!(obj is TextBounds))
				return false;
			var bounds = (TextBounds)obj;
			return this == bounds;
		}
		public bool Equals(TextBounds other)
		{
			return this == other;
		}

        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ (Length.GetHashCode());
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
		bool _disposed;
		private long _length, _charOffset, _line, _col;

		public long CharOffset
		{
			get
			{
				return _charOffset;
			}
		}
		public long Line
		{
			get
			{
				return _line;
			}
		}
		public long Column
		{
			get
			{
				return _col;
			}
		}
		public long Length
		{
			get
			{
				return _length;
			}
		}
		public long AmountLeft
		{
			get
			{
				return _length - _charOffset;
			}
		}
		public Location Location
		{
			get
			{
				return new Location(_charOffset, _line, _col);
			}
		}

		public Reader(TextReader baseReader, long length)
		{
			SetInput(baseReader, length);
		}
		public Reader(Stream input)
		{
			SetInput(input);
		}

		private void SetInput(Stream input)
		{
			Reset();
			_length = input.Length;

			// Detect preamble length
			var encodings = Encoding.GetEncodings();
			var testedEncodings = new List<Encoding>();
			foreach (var enc in encodings)
			{
				if (!testedEncodings.Contains(enc.GetEncoding()))
				{
					testedEncodings.Add(enc.GetEncoding());
					byte[] preamble = enc.GetEncoding().GetPreamble();

                    if (preamble.Length == 0 || preamble.Length > input.Length)
                    {
                        continue;
                    }

				    input.Position = 0;

					var testPreamble = new byte[preamble.Length];
					input.Read(testPreamble, 0, testPreamble.Length);
					input.Position = 0;

                    // Find testPreamble in preamble
					bool match = !preamble.Where((t, i) => testPreamble[i] != t).Any();

				    if (match)
					{
						_length -= testPreamble.Length;
						break;
					}
				}
			}

			var reader = new StreamReader(input);

			_baseReader = reader;
		}
		private void SetInput(TextReader input, long inputLength)
		{
			Reset();
			_length = inputLength;
			_baseReader = input;
		}

		public void Dispose()
		{
			if (_disposed) return;

			_baseReader.Dispose();
			_disposed = true;
		}

		public char Read(bool throwex = true)
		{
			int ivalue = _baseReader.Read();

			if (ivalue == -1)
			{
				if (throwex)
					throw new EndOfStreamException("Recieved a negative 1, means end of stream");
				else
					return unchecked((char)(-1));
			}
			
			var value = (char)ivalue;
			_charOffset++;
			_col++;

			// Turn \r's into \n's
			if (value == '\r')
			{
				if (_baseReader.Peek() == '\n')
					return Read();
				else
					value = '\n';
			}

			// Goto next line if value is a \n
			if (value == '\n')
			{
				_line++;
				_col = 1;
			}

			return value;
		}
		public char Peek(bool throwex = true)
		{
			int ivalue = _baseReader.Peek();
			if (ivalue == -1)
			{
				if (throwex)
					throw new EndOfStreamException();
				else
					return unchecked((char)(-1));
			}
			var value = (char)ivalue;
			if (value == '\r')
				value = '\n';
			return value;
		}
		public string ReadLine()
		{
			var builder = new StringBuilder();
			char c = Read(false);
			while (c != '\n' && AmountLeft > 0)
			{
				builder.Append(c);
				c = Read(false);
			}
			return builder.ToString();
		}

		private void Reset()
		{
			_length = 0;
			_charOffset = 0;
			_line = _col = 1;
		}
	}
}