using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WorldExplorer.Tools
{
    public class Section : Setting, IEnumerable<Setting>
    {
        private readonly List<Setting> _children = new();

        public int Depth
        {
            get
            {
                var depth = 0;
                if (Parent != null)
                {
                    depth += Parent.Depth + 1;
                }

                return depth;
            }
        }

        public object? this[string path]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentNullException(nameof(path));
                }

                var item = GetItemAtPath(path);
                if (item == null)
                {
                    throw new KeyNotFoundException($"Could not find item at path \"{path}\".");
                }

                if (item is Section)
                {
                    throw new InvalidOperationException("Found a section while looking for a value.");
                }

                return item.Value;
            }
            set => Add(path, value);
        }

        public object? this[string path, object? defaultValue, bool addIfMissing = true]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentNullException(nameof(path));
                }

                var item = GetItemAtPath(path);
                switch (item)
                {
                    case null:
                    {
                        if (addIfMissing)
                        {
                            this[path] = defaultValue;
                        }

                        return defaultValue;
                    }
                    case Section:
                        throw new InvalidOperationException("Found a section while looking for a value.");
                    default:
                        return item.Value;
                }
            }
        }

        public Section()
        {
        }

        public Section(string name) : this()
        {
            Name = name;
        }

        public IEnumerator<Setting> GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        public bool ContainsItem(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _children.Any(child => string.Compare(child.Name, name, ignoreCase) == 0);
        }

        public Setting? GetItemAtPath(string path, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var sections = path.Split('.');
            var lastSection = this;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (string.IsNullOrEmpty(section))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                var lastSetting = lastSection?.GetItem(section, ignoreCase);
                if (lastSetting != null)
                {
                    lastSection = lastSetting as Section;
                    if (i + 1 >= sections.Length)
                        // We're at the last section so return it
                    {
                        return lastSetting;
                    }
                }
                else
                {
                    return null;
                }
            }

            throw new InvalidOperationException("No sections found in path \"" + path + "\".");
        }

        public Setting? GetItem(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _children.FirstOrDefault(child => string.Compare(child.Name, name, ignoreCase) == 0);
        }

        public void Add(Setting item)
        {
            if (item.Name != null && ContainsItem(item.Name))
            {
                throw new ApplicationException("Item already exists with that name!");
            }

            _children.Add(item);
            item.Parent = this;
        }

        public void Add(string path, object? value, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var sections = path.Split('.');
            var lastSection = this;
            for (var i = 0; i < sections.Length; i++)
            {
                var sectionName = sections[i];
                if (string.IsNullOrEmpty(sectionName))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                var lastSetting = lastSection._children.Count == 0 ? null : lastSection.GetItem(sectionName, ignoreCase);
                if (lastSetting != null)
                {
                    if (lastSetting is Section section)
                    {
                        lastSection = section;
                    }
                    else
                    {
                        if (i + 1 >= sections.Length)
                        {
                            // Is the last section in the path
                            lastSetting.Value = value;
                            return;
                        }
                    }
                }
                else
                {
                    if (i + 1 >= sections.Length)
                    {
                        // Is the last section in the path
                        Setting setting = new(sectionName, value);

                        lastSection.Add(setting);
                    }
                    else
                    {
                        // Section doesn't exist, create it
                        Section section = new(sectionName);

                        lastSection.Add(section);

                        lastSection = section;
                    }
                }
            }
        }

        public void Delete(string path, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var sections = path.Split('.');
            var lastSection = this;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (string.IsNullOrEmpty(section))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                var lastSetting = lastSection.GetItem(section, ignoreCase);
                if (lastSetting != null)
                {
                    if (lastSetting is Section sec)
                    {
                        lastSection = sec;
                    }
                    else if (i + 1 >= sections.Length)
                    {
                        // We're at the last section so remove it
                        lastSection._children.Remove(lastSetting);

                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            throw new InvalidOperationException("No sections in the path \"" + path + "\"");
        }

        public T? Get<T>(string path, T? defaultValue = default, bool addIfMissing = true, bool ingoreCase = true)
        {
            var value = this[path, defaultValue, addIfMissing];
            if (value == null) return defaultValue;
            var type = typeof(T);
            if (type.IsEnum)
            {
                if (value is T)
                {
                    return (T)value;
                }

                if (value.GetType() != typeof(string))
                {
                    return defaultValue;
                }

                try
                {
                    return (T)Enum.Parse(type, (string)value, ingoreCase);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    public class Setting : MarshalByRefObject
    {
        public string? Name;
        public Section? Parent;
        public object? Value;

        protected Setting()
        {
        }

        public Setting(Section parent, string? name, object? value)
        {
            Parent = parent;
            Name = name;
            Value = value;
        }

        public Setting(string? name, object? value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            if (Parent != null && Parent.Name != null)
            {
                return $"{Parent}.{Name}";
            }

            return Name ?? "";
        }

        public override bool Equals(object? obj)
        {
            if (obj is Setting setting)
            {
                return Name == setting.Name && Value == setting.Value;
            }

            return false;
        }

        protected bool Equals(Setting other)
        {
            return Name == other.Name && Equals(Parent, other.Parent) && Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Parent, Value);
        }
    }

    internal abstract class SettingsIO : MarshalByRefObject
    {
        public delegate bool ValueParserExtensionHandler(string value, out object obj);

        private static readonly List<ValueParserExtensionHandler> ValueParsers = new();

        private static INISettingsIO? _mINI;

        public static SettingsIO Ini
        {
            get
            {
                if (_mINI == null)
                {
                    _mINI = new INISettingsIO();
                }

                return _mINI;
            }
        }

        public static void RegisterValueParser(ValueParserExtensionHandler parser)
        {
            ValueParsers.Add(parser);
        }

        public static void UnregisterValueParser(ValueParserExtensionHandler parser)
        {
            ValueParsers.Remove(parser);
        }


        private void Error(string message)
        {
            Console.WriteLine($"> Error: {message}");
        }

        private void Warning(string message)
        {
            Console.WriteLine($"> Warning: {message}");
        }

        private static bool TryParseValue(string value, out object? obj)
        {
            if (string.IsNullOrEmpty(value))
            {
                obj = null;
                return true;
            }

            if (string.Compare(value, "true", true, CultureInfo.InvariantCulture) == 0)
            {
                obj = true;
                return true;
            }

            if (string.Compare(value, "false", true, CultureInfo.InvariantCulture) == 0)
            {
                obj = false;
                return true;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                obj = intValue;
                return true;
            }

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                obj = floatValue;
                return true;
            }

            foreach (var parser in ValueParsers)
            {
                if (parser(value, out obj))
                {
                    return true;
                }
            }

            obj = value;
            return true;
        }

        public abstract Section ParseFile(string filePath);
        public abstract void WriteFile(string filePath, Section root);

        #region Ini Implementation

        private class INISettingsIO : SettingsIO
        {
            private char _c;
            private Section? _currentSection;
            private Reader? _reader;
            private Section? _root;

            private bool IsAtEnd()
            {
                if (_reader == null) return true;
                return _reader.AmountLeft <= 0;
            }

            private void Reset()
            {
                _c = '\0';
                _root = null;
                _reader = null;
                _currentSection = null;
            }

            private void Next()
            {
                if (_reader == null) return;
                _c = _reader.Read();
            }

            private string ReadLine(bool useCurrentChar = true)
            {
                StringBuilder sb = new();
                if (!useCurrentChar)
                {
                    Next();
                }

                while (_c != '\n')
                {
                    sb.Append(_c);
                    if (IsAtEnd())
                    {
                        break;
                    }

                    Next();
                }

                return sb.ToString();
            }

            private string? ReadIdentifier(bool useCurrentChar = true)
            {
                StringBuilder sb = new();
                if (!useCurrentChar)
                {
                    Next();
                }

                if (char.IsLetter(_c) || _c == '_')
                {
                    do
                    {
                        sb.Append(_c);
                        Next();
                    } while (char.IsLetterOrDigit(_c) || _c == '_');
                }
                else
                {
                    Error("Invalid character \"" + _c + "\", expecting a indentifier character.");
                    return null;
                }

                return sb.ToString();
            }

            private void SkipWhiteSpace()
            {
                if (char.IsWhiteSpace(_c))
                {
                    Next();
                }

                while (_c != '\n' && char.IsWhiteSpace(_c))
                {
                    Next();
                }
            }

            private static void PrintSection(TextWriter writer, Section section)
            {
                if (!string.IsNullOrEmpty(section.Name))
                {
                    writer.Write("[{0}]\n", section.Name);
                }

                foreach (var setting in section)
                {
                    if (setting is Section section1)
                    {
                        PrintSection(writer, section1);
                    }
                    else
                        // Is a setting, write it
                    {
                        writer.Write("{0} = {1}\n", setting.Name, setting.Value);
                    }
                }
            }

            public override Section ParseFile(string filePath)
            {
                Reset();
                using (var file = File.OpenRead(filePath))
                using (_reader = new Reader(file))
                {
                    _root = new Section();

                    if (_reader.Length == 0)
                    {
                        return _root;
                    }

                    while (!IsAtEnd())
                    {
                        Next();

                        if (_c == '[')
                        {
                            StringBuilder nameBuilder = new();
                            Next();
                            while (!IsAtEnd() && _c != ']')
                            {
                                if (_c == '\n')
                                {
                                    Warning("Unexpected newline inside section header.");
                                    break;
                                }

                                nameBuilder.Append(_c);
                                Next();
                            }

                            while (_c != '\n')
                            {
                                Next();
                                if (!char.IsWhiteSpace(_c))
                                {
                                    Error("Expecting a new line after section header.");
                                }
                            }

                            Section section = new(nameBuilder.ToString());
                            _currentSection = section;
                            _root.Add(section);
                        }
                        else if (char.IsLetter(_c))
                        {
                            var name = ReadIdentifier();
                            SkipWhiteSpace();
                            if (_c == '=')
                            {
                                // Skip the equals sign
                                Next();
                                var valueString = ReadLine().Trim();

                                if (!TryParseValue(valueString, out var value))
                                {
                                    Error("Invalid/Unsupported value \"" + valueString + "\".");
                                }

                                Setting setting = new(name, value);

                                if (_currentSection != null)
                                    // Add the parsed setting to the current section
                                {
                                    _currentSection.Add(setting);
                                }
                                else
                                    // Not section exists, add it to root
                                {
                                    _root.Add(setting);
                                }
                            }
                            else
                            {
                                Error("Expecting equals sign after identifier.");
                            }
                        }
                        else if (!IsAtEnd())
                        {
                            _reader.Read();
                        }
                    } // while (!IsAtEnd())
                } // using

                var temp = _root;
                Reset();
                return temp;
            }

            public override void WriteFile(string filePath, Section root)
            {
                Reset();
                using (Stream file = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new(file))
                {
                    PrintSection(writer, root);

                    writer.Flush();
                }

                Reset();
            }
        }

        #endregion
    }
}