using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace WorldExplorer.Logging;

/// <summary>
/// This doesn't really do anything
/// </summary>
internal sealed class ScopeDisposable : IDisposable
{
    public void Dispose()
    {
    }
}

public class StringLogger : ILogger
{
    private readonly StringBuilder _sb = new();
    private readonly LogLevel _minLevel = LogLevel.Trace;
        
    public StringLogger()
    {
    }

    public StringLogger(LogLevel minLevel)
    {
        _minLevel = minLevel;
    }

    public void LogLine(string line)
    {
        _sb.AppendLine(line);
    }

    public override string ToString()
    {
        return _sb.ToString();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        _sb.Append($"{msg}\n");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new ScopeDisposable();
    }
}

public class NullLogger : ILogger
{
    private static readonly Lazy<NullLogger> InstanceLazy = new(() => new NullLogger());

    public static NullLogger Instance => InstanceLazy.Value;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new ScopeDisposable();
    }
}