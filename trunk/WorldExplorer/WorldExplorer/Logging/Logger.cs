using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.Logging
{
    public interface ILogger
    {
        void LogLine(string line);
    }

    public class StringLogger : ILogger
    {
        private StringBuilder _sb = new StringBuilder();

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
