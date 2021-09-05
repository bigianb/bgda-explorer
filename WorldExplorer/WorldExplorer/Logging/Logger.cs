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
}