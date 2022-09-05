namespace WorldExplorer;

public static class StringExtensions
{
    public static string TrimQuotes(this string value)
    {
        return value.Replace("\"", "");
    }
}