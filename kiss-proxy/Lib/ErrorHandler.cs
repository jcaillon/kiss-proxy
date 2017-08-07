using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kissproxy.Lib {

    internal static class ErrorHandler {

        private const string ErrorFileName = "exceptions.log";

        /// <summary>
        /// Log a piece of information
        /// returns false if the error already occurred during the session, true otherwise
        /// </summary>
        public static void LogErrors(Exception e, string message = null) {
            if (e == null)
                return;
            
            try {
                var info = GetExceptionInfo(e);

                if (message != null)
                    info.Message = message + " : " + info.Message;

                // write in the log
                var toAppend = new StringBuilder();
                toAppend.AppendLine("============================================================");
                toAppend.AppendLine("WHAT : " + info.Message);
                toAppend.AppendLine("WHEN : " + DateTime.Now.ToString(CultureInfo.CurrentCulture));
                toAppend.AppendLine("WHERE : " + info.OriginMethod + ", line " + info.OriginLine);
                toAppend.AppendLine("DETAILS : ");
                foreach (var line in info.FullException.Split('\n')) {
                    toAppend.AppendLine("    " + line.Trim());
                }
                toAppend.AppendLine("");
                toAppend.AppendLine("");

                var errorPath = Path.Combine(Path.GetDirectoryName(AssemblyInfo.Location) ?? "", ErrorFileName);
                File.AppendAllText(errorPath, toAppend.ToString());

            } catch (Exception) {
                // ignored
            }
        }

        /// <summary>
        /// Returns info on an exception 
        /// </summary>
        private static ExceptionInfo GetExceptionInfo(Exception e) {
            ExceptionInfo output = null;
            var frame = new StackTrace(e, true).GetFrame(0);
            if (frame != null) {
                var method = frame.GetMethod();
                output = new ExceptionInfo {
                    OriginMethod = (method != null ? (method.DeclaringType != null ? method.DeclaringType.ToString() : "?") + "." + method.Name : "?") + "()",
                    OriginLine = frame.GetFileLineNumber(),
                    OriginVersion = AssemblyInfo.Version,
                    Message = e.Message,
                    FullException = e.ToString()
                };
            }
            if (output == null)
                output = new ExceptionInfo {
                    OriginMethod = "???",
                    OriginVersion = AssemblyInfo.Version,
                    Message = e.Message,
                    FullException = e.ToString()
                };
            return output;
        }

        #region global error handler callbacks

        public static void UnhandledErrorHandler(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
                LogErrors(ex, "Unhandled error");
        }

        public static void ThreadErrorHandler(object sender, ThreadExceptionEventArgs e) {
            LogErrors(e.Exception, "Thread error");
        }

        public static void UnobservedErrorHandler(object sender, UnobservedTaskExceptionEventArgs e) {
            LogErrors(e.Exception, "Unobserved error");
        }

        #endregion
    }

    #region ExceptionInfo

    /// <summary>
    /// Represents an exception
    /// </summary>
    internal class ExceptionInfo {
        public string OriginVersion { get; set; }
        public string OriginMethod { get; set; }
        public int OriginLine { get; set; }
        public string ReceptionTime { get; set; }
        public string Message { get; set; }
        public string FullException { get; set; }
    }

    #endregion
}
