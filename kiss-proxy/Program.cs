﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using kissproxy.Core;
using kissproxy.Form;
using kissproxy.Lib;

namespace kissproxy {
    static class Program {
        internal static Config Config;

        internal static bool LogActivated;

        private static List<HttpProxy> _proxies = new List<HttpProxy>();

        private static List<TcpFwd> _tcpForwarders = new List<TcpFwd>();
        
        private static List<UdpFwd> _udpForwarders = new List<UdpFwd>();

        private static MainForm _mainForm;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // catch unhandled errors to log them
            AppDomain.CurrentDomain.UnhandledException += ErrorHandler.UnhandledErrorHandler;
            Application.ThreadException += ErrorHandler.ThreadErrorHandler;
            TaskScheduler.UnobservedTaskException += ErrorHandler.UnobservedErrorHandler;

            if (!LoadConfig())
                return;

            StartServers();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _mainForm = new MainForm();
            Application.Run(_mainForm);
        }

        /// <summary>
        /// Exit the application
        /// </summary>
        internal static void Exit() {
            StopServers();
            Application.Exit();
        }

        /// <summary>
        /// Load the config file
        /// </summary>
        /// <returns></returns>
        internal static bool LoadConfig() {
            Config = Config.Load();
            if (Config == null) {
                Config.ExportSample();
                MessageBox.Show("Could not find the config.xml necessary next to this .exe!\r\nA sample file has been exported.\r\nThis application will now shutdown", "Config.xml not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
                return false;
            }

            if (_mainForm != null)
                _mainForm.ShowBalloon("All done!", "Config file reloaded", ToolTipIcon.Info, 3);
            return true;
        }

        /// <summary>
        /// Start all the servers
        /// </summary>
        internal static void StartServers() {
            try {
                // start HTTP PROXIES
                foreach (var proxy in Config.Proxies) {
                    var prox = new HttpProxy(proxy);
                    _proxies.Add(prox);
                    prox.Start();
                }

                // start TCP FORWARDER
                foreach (var forwarder in Config.TcpForwarders) {
                    var fwd = new TcpFwd(forwarder);
                    _tcpForwarders.Add(fwd);
                    fwd.Start();
                }

                // start UDP FORWARDER
                foreach (var forwarder in Config.UdpForwarders) {
                    var fwd = new UdpFwd(forwarder);
                    _udpForwarders.Add(fwd);
                    fwd.Start();
                }
            } catch (Exception e) {
                ErrorHandler.LogErrors(e);
                MessageBox.Show("Error starting the servers :\r\n\r\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Stop all the servers
        /// </summary>
        internal static void StopServers() {
            try {
                // stop HTTP PROXIES
                foreach (var proxy in _proxies) {
                    proxy.Stop();
                }

                _proxies.Clear();
                
                // stop TCP FORWARDER
                foreach (var fwd in _tcpForwarders) {
                    fwd.Stop();
                }
                
                // stop UDP FORWARDER
                foreach (var fwd in _udpForwarders) {
                    fwd.Stop();
                }

                _tcpForwarders.Clear();
                _udpForwarders.Clear();
            } catch (Exception e) {
                ErrorHandler.LogErrors(e);
            }
        }

        /// <summary>
        /// restart all servers and reload the config file
        /// </summary>
        internal static void RestartServers() {
            _mainForm.ShowBalloon("Please wait...", "Restarting servers", ToolTipIcon.Info, 3);
            StopServers();
            if (LoadConfig()) {
                StartServers();
            }
            _mainForm.ShowBalloon("All done!", "Servers restarted!", ToolTipIcon.Info, 3);
        }

        internal static void OpenConfig() {
            Config.Open();
        }

        public static void ShowStartedServers() {
            var sb = new StringBuilder();
            sb.AppendLine("List of the started servers : ");
            sb.AppendLine();
            foreach (var httpProxy in _proxies) {
                sb.AppendLine("HTTP PROXY @ " + httpProxy.ServerInfo);
            }

            foreach (var fwd in _tcpForwarders) {
                sb.AppendLine("TCP FOWARDER @ " + fwd.ServerInfo);
            }

            foreach (var fwd in _udpForwarders) {
                sb.AppendLine("UDP FOWARDER @ " + fwd.ServerInfo);
            }

            MessageBox.Show(sb.ToString(), "Started servers", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}