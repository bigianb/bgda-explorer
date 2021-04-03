using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WorldExplorer.Tools
{
    public class Section : Setting, IEnumerable<Setting>
    {
        readonly List<Setting> _children = new List<Setting>();

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

        public object this[string path]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentNullException("path");
                }

                var item = GetItemAtPath(path);
                if (item == null)
                {
                    throw new KeyNotFoundException("Could not find item at path \"" + path + "\".");
                }

                if (item is Section)
                {
                    throw new InvalidOperationException("Found a section while looking for a value.");
                }

                return item.Value;
            }
            set => Add(path, value);
        }
        public object this[string path, object defaultValue, bool addIfMissing = true]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentNullException("path");
                }

                var item = GetItemAtPath(path);
                if (item == null)
                {
                    if (addIfMissing)
                    {
                        this[path] = defaultValue;
                    }

                    return defaultValue;
                }
                if (item is Section)
                {
                    throw new InvalidOperationException("Found a section while looking for a value.");
                }

                return item.Value;
            }
        }

        public bool ContainsItem(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return _children.Any(child => string.Compare(child.Name, name, ignoreCase) == 0);
        }
        public Setting GetItemAtPath(string path, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var sections = path.Split('.');
            var lastSection = this;
            Setting lastSetting = null;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (string.IsNullOrEmpty(section))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                lastSetting = lastSection.GetItem(section, ignoreCase);
                if (lastSetting != null)
                {
                    lastSection = lastSetting as Section;
                    if (i + 1 >= sections.Length)
                    {
                        // We're at the last section so return it
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
        public Setting GetItem(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return _children.FirstOrDefault(child => string.Compare(child.Name, name, ignoreCase) == 0);
        }

        public void Add(Setting item)
        {
            if (ContainsItem(item.Name))
            {
                throw new ApplicationException("Item already exists with that name!");
            }

            _children.Add(item);
            item.Parent = this;
        }
        public void Add(string path, object value, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var sections = path.Split('.');
            var lastSection = this;
            Setting lastSetting = null;
            for (var i = 0; i < sections.Length; i++)
            {
                var sectionName = sections[i];
                if (string.IsNullOrEmpty(sectionName))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                lastSetting = lastSection._children.Count == 0 ? null : lastSection.GetItem(sectionName, ignoreCase);
                if (lastSetting != null)
                {
                    if (lastSetting is Section)
                    {
                        lastSection = (Section)lastSetting;
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
                        var setting = new Setting(sectionName, value);

                        if (lastSection != null)
                        {
                            lastSection.Add(setting);
                        }
                        else
                        {
                            Add(setting);
                        }
                    }
                    else
                    {
                        // Section doesn't exist, create it
                        var section = new Section(sectionName);

                        if (lastSection != null)
                        {
                            lastSection.Add(section);
                        }
                        else
                        {
                            Add(section);
                        }

                        lastSection = section;
                    }
                }
            }
        }
        public void Delete(string path, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var sections = path.Split('.');
            var lastSection = this;
            Setting lastSetting = null;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (string.IsNullOrEmpty(section))
                {
                    throw new FormatException("Invalid path \"" + path + "\" supplied. Nothing after dot.");
                }

                lastSetting = lastSection.GetItem(section, ignoreCase);
                if (lastSetting != null)
                {
                    if (lastSetting is Section)
                    {
                        lastSection = lastSetting as Section;
                    }
                    else if (i + 1 >= sections.Length)
                    {
                        // We're at the last section so remove it
                        if (lastSection == null)
                        {
                            _children.Remove(lastSetting);
                        }
                        else
                        {
                            lastSection._children.Remove(lastSetting);
                        }

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

        public T Get<T>(string path, T defaultValue = default(T), bool addIfMissing = true, bool ingoreCase = true)
        {
            var value = this[path, defaultValue, addIfMissing];
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
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _children.GetEnumerator();
        }
    }
    public class Setting : MarshalByRefObject
    {
        public Section Parent;
        public string Name;
        public object Value;

        public override string ToString()
        {
            if (Parent != null && Parent.Name != null)
            {
                return Parent.ToString() + "." + Name;
            }

            return Name;
        }
        public override bool Equals(object obj)
        {
            if (obj != null && obj is Setting)
            {
                var setting = (Setting)obj;
                return Name == setting.Name && Value == setting.Value;
            }
            return false;
        }
        public override int GetHashCode()
        {
            var hash = 0x154654;

            if (Name != null)
            {
                hash ^= (Name.GetHashCode() + 3) * 2;
            }

            if (Value != null)
            {
                hash ^= (Value.GetHashCode() - 3) << 2;
            }

            return hash;
        }

        public Setting()
        {
        }
        public Setting(Section parent, string name, object value)
        {
            Parent = parent;
            Name = name;
            Value = value;
        }
        public Setting(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
    abstract class SettingsIO : MarshalByRefObject
    {
        #region Ini Implementation

        private class INISettingsIO : SettingsIO
        {
            char _c;
            Section _root;
            Section _currentSection;
            Reader _reader;

            private bool IsAtEnd()
            {
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
                _c = _reader.Read();
            }
            private string ReadLine(bool useCurrentChar = true)
            {
                var sb = new StringBuilder();
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
            private string ReadIdentifier(bool useCurrentChar = true)
            {
                var sb = new StringBuilder();
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
            private void PrintSection(StreamWriter writer, Section section)
            {
                if (!string.IsNullOrEmpty(section.Name))
                {
                    writer.Write("[{0}]\n", section.Name);
                }

                foreach (var setting in section)
                {
                    if (setting is Section)
                    {
                        PrintSection(writer, (Section)setting);
                    }
                    else
                    {
                        // Is a setting, write it
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
                            var nameBuilder = new StringBuilder();
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
                            var section = new Section(nameBuilder.ToString());
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

                                var setting = new Setting(name, value);

                                if (_currentSection != null)
                                {
                                    // Add the parsed setting to the current section
                                    _currentSection.Add(setting);
                                }
                                else
                                {
                                    // Not section exists, add it to root
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
                using (var writer = new StreamWriter(file))
                {
                    PrintSection(writer, root);

                    writer.Flush();
                }
                Reset();
            }
        }

        #endregion

        public delegate bool ValueParserExtentionHandler(string value, out object obj);

        private static readonly List<ValueParserExtentionHandler> ValueParsers = new List<ValueParserExtentionHandler>();

        private static INISettingsIO _mINI;
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

        public static void RegisterValueParser(ValueParserExtentionHandler parser)
        {
            ValueParsers.Add(parser);
        }
        public static void UnregisterValueParser(ValueParserExtentionHandler parser)
        {
            ValueParsers.Remove(parser);
        }


        private void Error(string message)
        {
            Console.WriteLine("> Error: " + message);
        }

        private void Warning(string message)
        {
            Console.WriteLine("> Warning: " + message);
        }

        private static bool TryParseValue(string value, out object obj)
        {
            if (string.IsNullOrEmpty(value))
            {
                obj = null;
                return true;
            }
            if (string.Compare(value, "true", true) == 0)
            {
                obj = true;
                return true;
            }
            if (string.Compare(value, "false", true) == 0)
            {
                obj = false;
                return true;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue))
            {
                obj = intValue;
                return true;
            }

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var floatValue))
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
    }
}
