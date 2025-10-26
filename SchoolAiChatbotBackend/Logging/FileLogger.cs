using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace SchoolAiChatbotBackend.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _name;
        private readonly Func<string, LogLevel, bool>? _filter;
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogger(string name, Func<string, LogLevel, bool>? filter, string filePath)
        {
            _name = name;
            _filter = filter;
            _filePath = filePath;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (_filter == null) return true;
            return _filter(_name, logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var logRecord = $"{DateTime.UtcNow:O} [{logLevel}] {_name}: {message}";
            if (exception != null)
            {
                logRecord += "\n" + exception.ToString();
            }
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? "");
                    File.AppendAllText(_filePath, logRecord + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // suppress logging errors
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool>? _filter;
        private readonly string _filePath;

        public FileLoggerProvider(string filePath, Func<string, LogLevel, bool>? filter = null)
        {
            _filePath = filePath;
            _filter = filter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _filter, _filePath);
        }

        public void Dispose()
        {
        }
    }
}
