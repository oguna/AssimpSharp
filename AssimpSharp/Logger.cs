using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public abstract class Logger
    {
        public enum LogSeverity
        {
            Normal,
            Verbose
        }

        public enum ErrorSeverity
        {
            Debugging = 1,
            Info = 2,
            Warn = 4,
            Err = 8,
        }

        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void SetLogSeverity(LogSeverity logSeverity)
        {
            this.Severity = logSeverity;
        }

        public LogSeverity GetLogSeverity()
        {
            return this.Severity;
        }

        public abstract bool AttachStream(LogStream stream, ErrorSeverity severity = ErrorSeverity.Debugging | ErrorSeverity.Err | ErrorSeverity.Warn | ErrorSeverity.Info);

        public abstract bool DetatchStream(LogStream stream, ErrorSeverity severity = ErrorSeverity.Debugging | ErrorSeverity.Err | ErrorSeverity.Warn | ErrorSeverity.Info);

        protected Logger()
        {
            SetLogSeverity(LogSeverity.Normal);
        }

        protected abstract void OnDebug(string message);

        protected abstract void OnInfo(string message);

        protected abstract void OnWarn(string message);

        protected abstract void OnError(string message);

        protected LogSeverity Severity;

    }
}
