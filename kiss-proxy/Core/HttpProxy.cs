using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using kissproxy.Lib;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace kissproxy.Core {

    public class HttpProxy {

        private readonly ProxyServer _proxyServer;

        private readonly Proxy _proxyConfig;

        private IPEndPoint _thisEndPoint;

        private Dictionary<string, ExternalProxy> _externalProxies = new Dictionary<string, ExternalProxy>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="proxy"></param>
        public HttpProxy(Proxy proxy) {
            _proxyServer = new ProxyServer {
                ExceptionFunc = exception => ErrorHandler.LogErrors(exception),
                TrustRootCertificate = true,
                ForwardToUpstreamGateway = true,
            };
            _proxyConfig = proxy;
        }

        /// <summary>
        /// Start server
        /// </summary>
        public void Start() {
            _proxyServer.BeforeRequest += OnRequest;
            _proxyServer.BeforeResponse += OnResponse;
            _proxyServer.GetCustomUpStreamHttpProxyFunc = GetCustomUpStreamProxy;
            _proxyServer.GetCustomUpStreamHttpsProxyFunc = GetCustomUpStreamProxy;

            // adding endpoints
            _thisEndPoint = new IPEndPoint(!string.IsNullOrEmpty(_proxyConfig.LocalAddress) ? IPAddress.Parse(_proxyConfig.LocalAddress) : Utils.LocalMachineIpAddress, _proxyConfig.LocalPort);
            _proxyServer.AddEndPoint(new ExplicitProxyEndPoint(_thisEndPoint.Address, _thisEndPoint.Port, true));
            
            // starting the server
            _proxyServer.Start();

            Logger.Log(ProxyType.HttpProxy, _thisEndPoint, _thisEndPoint, "Starting proxy server...");

            // registering external proxies
            foreach (var externalProxyRule in _proxyConfig.ExternalProxyRules) {
                var newProxy = new ExternalProxy {
                    HostName = externalProxyRule.ProxyHost,
                    Port = externalProxyRule.ProxyPort
                };
                if (!string.IsNullOrEmpty(externalProxyRule.ProxyUsername)) {
                    newProxy.UserName = externalProxyRule.ProxyUsername;
                    newProxy.Password = externalProxyRule.ProxyPassword;
                }
                _externalProxies.Add(externalProxyRule.ProxyHost + ":" + externalProxyRule.ProxyPort, newProxy);
            }
        }

        /// <summary>
        /// Stop server
        /// </summary>
        public void Stop() {
            _proxyServer.BeforeRequest -= OnRequest;
            _proxyServer.BeforeResponse -= OnResponse;
            _proxyServer.Stop();

            Logger.Log(ProxyType.HttpProxy, _thisEndPoint, _thisEndPoint, "Stopping proxy server...");
        }

        /// <summary>
        /// Gets the system up stream proxy
        /// </summary>
        private Task<ExternalProxy> GetCustomUpStreamProxy(SessionEventArgs e) {
            try {
                // use a custom external proxy?
                foreach (var proxyRule in _proxyConfig.ExternalProxyRules) {
                    var reg = new Regex(proxyRule.UrlMatch, RegexOptions.IgnoreCase);
                    if (reg.Match(e.WebSession.Request.RequestUri.AbsoluteUri).Success) {

                        ExternalProxy proxy = null;

                        // try to use the system proxy?
                        if (proxyRule.ProxyHost.Equals("SystemWebProxy")) {
                            var webProxy = WebRequest.GetSystemWebProxy().GetProxy(e.WebSession.Request.RequestUri);
                            proxy = new ExternalProxy {
                                HostName = webProxy.Host,
                                Port = webProxy.Port
                            };
                        }

                        // use a configured external proxy
                        if (_externalProxies.ContainsKey(proxyRule.ProxyHost + ":" + proxyRule.ProxyPort)) {
                            proxy = _externalProxies[proxyRule.ProxyHost + ":" + proxyRule.ProxyPort];
                        }

                        if (proxy != null) {
                            Logger.Log(ProxyType.HttpProxy, e.ClientEndPoint, _thisEndPoint, $"NEW SERVER CONNEXION ({_proxyServer.ServerConnectionCount}) TO {e.WebSession.Request.RequestUri.Host}:{e.WebSession.Request.RequestUri.Port} USING EXTERNAL PROXY {proxy.HostName}:{proxy.Port}");
                            return Task.FromResult(proxy);
                        }
                    }
                }
            } catch (Exception ex) {
                ErrorHandler.LogErrors(ex, "GetCustomUpStreamProxy");
            }

            return Task.FromResult(new ExternalProxy {
                HostName = e.WebSession.Request.RequestUri.Host,
                Port = e.WebSession.Request.RequestUri.Port
            });
        }

        /// <summary>
        /// Intercept / cancel / redirect or update requests
        /// </summary>
        private async Task OnRequest(object sender, SessionEventArgs e) {
            try {
                Logger.Log(ProxyType.HttpProxy, e.ClientEndPoint, _thisEndPoint, e.WebSession.Request.Method + " " + e.WebSession.Request.RequestUri.AbsoluteUri);

                // dump request
                if (Program.LogActivated) {
                    string body = null;
                    if (e.WebSession.Request.HasBody) {
                        body = await e.GetRequestBodyAsString();
                    }
                    Logger.Dump(e.ClientEndPoint, e.WebSession.Request, null, body);
                }

                // await e.Redirect("http://xx/");
                // await e.Ok("<!DOCTYPE html><html><body><h1>Website Blocked</h1><p>Blocked by titanium web proxy.</p></body></html>", null);
            } catch (Exception exception) {
                ErrorHandler.LogErrors(exception, "OnRequest");
            }
        }

        /// <summary>
        /// Intercept response
        /// </summary>
        private async Task OnResponse(object sender, SessionEventArgs e) {
            try {
                // dump response
                if (Program.LogActivated) {
                    string body = null;
                    if (e.WebSession.Response.HasBody) {
                        body = await e.GetResponseBodyAsString();
                    }
                    Logger.Dump(e.ClientEndPoint, e.WebSession.Request, e.WebSession.Response, body);
                }
            } catch (Exception exception) {
                ErrorHandler.LogErrors(exception, "OnResponse");
            }
        }
    }
}
