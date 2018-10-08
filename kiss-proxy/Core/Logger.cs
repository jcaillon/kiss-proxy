using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using kissproxy.Lib;

namespace kissproxy.Core {
    internal class Logger {
        #region Fields

        private const string LogDirectory = "logs";
        private const string DumpDirectory = "dump";
        private const string LogFileName = "access.log";

        private static object _lock = new object();

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the logs folder, create it if needed
        /// </summary>
        public string LogsFolder {
            get {
                var path = Path.Combine(Path.GetDirectoryName(AssemblyInfo.Location) ?? "", LogDirectory, DateTime.Today.ToString("dd-MM-yy"));
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>
        /// Returns the dump folder, create it if needed
        /// </summary>
        public string DumpFolder {
            get {
                var path = Path.Combine(LogsFolder, DumpDirectory);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>
        /// Get access log path
        /// </summary>
        public string AccessLogPath => Path.Combine(LogsFolder, LogFileName);

        #endregion

        #region Singleton

        private static Logger _instance;

        /// <summary>
        /// Get singleton instance of the logger
        /// </summary>
        /// <returns></returns>
        public static Logger Instance => _instance ?? (_instance = new Logger());

        #endregion

        #region Public

        /// <summary>
        /// Log a new line of access
        /// </summary>
        public static void Log(ProxyType type, IPEndPoint clientEndPoint, IPEndPoint proxyEndPoint, string request) {
            try {
                // ReSharper disable once FormatStringProblem
                var filePath = Instance.AccessLogPath;
                var content = $"[{DateTime.Now:HH:mm:ss}]\t{type}\t{clientEndPoint.Address}:{clientEndPoint.Port}\t{proxyEndPoint.Address}:{proxyEndPoint.Port}\t{request}\r\n";
                DoInLock(() => {
                    if (!File.Exists(filePath))
                        content = "[time]\tType\tClient\tProxy\tRequest\r\n" + content;
                    File.AppendAllText(filePath, content, Encoding.Default);
                });
            } catch (Exception e) {
                ErrorHandler.LogErrors(e);
            }
        }

        /// <summary>
        /// Dump a request/response
        /// </summary>
        public static void Dump(IPEndPoint clientEndPoint, Titanium.Web.Proxy.Http.Request request, Titanium.Web.Proxy.Http.Response response, string body) {
            try {
                foreach (var logRule in Program.Config.LogRules) {
                    if (!string.IsNullOrEmpty(logRule.UrlMatch) && new Regex(logRule.UrlMatch, RegexOptions.IgnoreCase).Match(request.RequestUri.AbsoluteUri).Success || !string.IsNullOrEmpty(logRule.ClientIp) && clientEndPoint.Address.ToString().Equals(logRule.ClientIp)) {
                        var filePath = DateTime.Now.ToString("HH.mm.ss.fff") + "_" + request.OriginalUrl.ToValidFileName() + "." + (response == null ? "req" : "res");
                        filePath = Path.Combine(Instance.DumpFolder, filePath);
                        if (filePath.Length >= 258)
                            filePath = filePath.Substring(0, 258);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        DoInLock(() => {
                            File.AppendAllText(filePath, (response == null ? request.HeaderText : response.HeaderText) + body ?? "", Encoding.Default);
                        });
                    }
                }
            } catch (Exception e) {
                ErrorHandler.LogErrors(e);
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Execute the action behind the lock
        /// </summary>
        private static void DoInLock(Action toDo) {
            if (Monitor.TryEnter(_lock)) {
                try {
                    toDo();
                } finally {
                    Monitor.Exit(_lock);
                }
            }
        }

        #endregion
    }

    #region Enums

    internal enum ProxyType {
        HttpProxy,
        TcpForwarder,
        UdpForwarder
    }

    #endregion
}