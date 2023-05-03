using System;

namespace PlayHouseConnector

{
    public interface IPlayHouseLogger
    {
        void Debug(string message, string className);
        void Info(string message, string className);
        void Warn(string message, string className);
        void Error(string? message, string className, Exception? ex = null);
        void Trace(string message, string className);
        void Fatal(string message, string className);
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    public class ConsoleLogger : IPlayHouseLogger
    {
        private string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public void Trace(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} TRACE: ({className}) - {message}");
        }

        public void Debug(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} DEBUG: ({className}) - {message}");
        }

        public void Info(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} INFO: ({className}) - {message}");
        }

        public void Warn(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} WARN: ({className}) - {message}");
        }

        public void Error(string? message, string className, Exception? ex = null)
        {
            if (ex != null)
            {
                Console.WriteLine($"{GetTimeStamp()} ERROR: ({className}) - {message} [{ex}]");
            }
            else
            {
                Console.WriteLine($"{GetTimeStamp()} ERROR: ({className}) - {message}");
            }
        }

        public void Fatal(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} FATAL: ({className}) - {message}");
        }
    }

    public static class LOG
    {
        private static IPlayHouseLogger _logger = new ConsoleLogger();
        private static LogLevel _logLevel = LogLevel.Trace;

        public static void SetLogger(IPlayHouseLogger logger, LogLevel logLevel = LogLevel.Trace)
        {
            _logger = logger;
            _logLevel = logLevel;
        }

        public static void Trace(string message, Type clazz)
        {
            if (LogLevel.Trace >= _logLevel)
            {
                _logger.Trace(message, clazz.Name);
            }
        }

        public static void Debug(string message, Type clazz)
        {
            if (LogLevel.Debug >= _logLevel)
            {
                _logger.Debug(message, clazz.Name);
            }
        }

        public static void Info(string message, Type clazz)
        {
            if (LogLevel.Info >= _logLevel)
            {
                _logger.Info(message, clazz.Name);
            }
        }

        public static void Warn(string message, Type clazz)
        {
            if (LogLevel.Warning >= _logLevel)
            {
                _logger.Warn(message, clazz.Name);
            }
        }

        public static void Error(string? message, Type clazz)
        {
            if (LogLevel.Error >= _logLevel)
            {
                _logger.Error(message, clazz.Name);
            }
        }

        public static void Error(string? message, Type clazz, Exception ex)
        {
            if (LogLevel.Error >= _logLevel)
            {
                _logger.Error(message, clazz.Name, ex);
            }
        }

        public static void Fatal(string message, Type clazz)
        {
            if (LogLevel.Fatal >= _logLevel)
            {
                _logger.Fatal(message, clazz.Name);
            }
        }
    }
}
