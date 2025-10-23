using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Util
{
    public static class GameLogger
    {
        public enum LogLevel
        {
            None,
            Error,
            Warning,
            Info,
            Debug
        }

        private static LogLevel _currentLogLevel = LogLevel.Debug;

        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

#if !DISABLE_LOGGING
        [Conditional("ENABLE_LOGS")]
        public static void LogInfo(string message, Object context = null)
        {
            if (_currentLogLevel < LogLevel.Info) return;
            var stackTrace = new StackTrace();
            var methodName = stackTrace.GetFrame(1)?.GetMethod()?.Name;
            var objectName = context != null ? context.name : "Unknown";
            Debug.Log($"[{objectName}][{methodName}] {message}", context);
        }

        [Conditional("ENABLE_LOGS")]
        public static void LogWarning(string message, Object context = null)
        {
            if (_currentLogLevel >= LogLevel.Warning)
            {
                var stackTrace = new StackTrace();
                var methodName = stackTrace.GetFrame(1)?.GetMethod()?.Name;
                var objectName = context != null ? context.name : "Unknown";
                Debug.LogWarning($"[{objectName}][{methodName}] {message}", context);
            }
        }

        [Conditional("ENABLE_LOGS")]
        public static void LogError(string message, Object context = null)
        {
            if (_currentLogLevel >= LogLevel.Error)
            {
                var stackTrace = new StackTrace();
                var methodName = stackTrace.GetFrame(1)?.GetMethod()?.Name;
                var objectName = context != null ? context.name : "Unknown";
                Debug.LogError($"[{objectName}][{methodName}] {message}", context);
            }
        }
#endif
    }
}