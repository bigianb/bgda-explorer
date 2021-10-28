using System.Text;

namespace WorldExplorer.Logging
{
    public interface ILogger
    {
        void LogLine(string line);
    }

    public class StringLogger : ILogger
    {
        private readonly StringBuilder _sb = new();

        public void LogLine(string line)
        {
            _sb.AppendLine(line);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }

    public class NullLogger : ILogger
    {
        private static NullLogger? _instance;

        public static NullLogger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NullLogger();
                return _instance;
            }
        }
        public void LogLine(string line)
        {
            // Do nothing
        }
    }
}