using System;
using System.Collections.Generic;
using System.Text;

namespace WorldExplorer
{
    public static class StringExtensions
    {
        public static string TrimQuotes(this string input)
        {
            if (input == null) return null;
            if (input.StartsWith('\"'))
                return input.Trim('\"');
            return input;
        }
    }
}
