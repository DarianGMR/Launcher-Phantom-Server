using System;
using Microsoft.Extensions.Logging;

namespace LauncherPhantomServer.Middleware
{
    /// <summary>
    /// Provider de logging personalizado que formatea los logs de manera simple y limpia
    /// Muestra solo: [NIVEL] mensaje
    /// Ejemplo: [INFO] [API] POST /api/auth/login - Usuario: admin
    /// Funciona en Debug, Release y ejecutables
    /// </summary>
    public class SimpleLoggerProvider : ILoggerProvider
    {
        private readonly Dictionary<string, SimpleLogger> _loggers = new();

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.ContainsKey(categoryName))
            {
                _loggers[categoryName] = new SimpleLogger(categoryName);
            }
            return _loggers[categoryName];
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class SimpleLogger : ILogger
    {
        private readonly string _categoryName;
        private static readonly object _lock = new object();

        public SimpleLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Mostrar todos los niveles de log para la aplicación
            if (_categoryName.StartsWith("LauncherPhantomServer"))
            {
                return true;
            }

            // Para Microsoft, solo mostrar Error y Critical
            if (_categoryName.StartsWith("Microsoft"))
            {
                return logLevel >= LogLevel.Error;
            }

            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // Sincronizar acceso para evitar entrelazamiento de logs
            lock (_lock)
            {
                // Colorear según el nivel
                var color = logLevel switch
                {
                    LogLevel.Critical => ConsoleColor.Magenta,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Information => ConsoleColor.Green,
                    LogLevel.Debug => ConsoleColor.Cyan,
                    LogLevel.Trace => ConsoleColor.Gray,
                    _ => ConsoleColor.White
                };

                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine($"[{logLevel.ToString().ToUpper()}] {message}");
                    Console.ResetColor();
                }
                catch
                {
                    // Si hay error con colores, mostrar sin color
                    Console.WriteLine($"[{logLevel.ToString().ToUpper()}] {message}");
                }

                // Mostrar excepción si existe
                if (exception != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    try
                    {
                        Console.WriteLine(exception.ToString());
                        Console.ResetColor();
                    }
                    catch
                    {
                        Console.WriteLine(exception.ToString());
                    }
                }
            }
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}