using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace kissproxy.Core {
    [XmlRoot(ElementName = "Config")]
    public class Config {
        private const string ConfigFileName = "config.xml";

        private static string ConfigFilePath => Path.Combine(Path.GetDirectoryName(Lib.AssemblyInfo.Location) ?? "", ConfigFileName);

        [XmlArray(ElementName = "Proxies")]
        public List<Proxy> Proxies { get; set; }

        [XmlArray(ElementName = "TcpForwarders")]
        public List<TcpForwarder> TcpForwarders { get; set; }

        [XmlArray(ElementName = "LogRules")]
        public List<LogRule> LogRules { get; set; }

        /// <summary>
        /// Load the config.xml file
        /// </summary>
        /// <returns></returns>
        public static Config Load() {
            var configPath = ConfigFilePath;
            if (File.Exists(configPath)) {
                var conf = new Config();
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                using (FileStream stream = File.OpenRead(configPath)) {
                    conf = (Config) serializer.Deserialize(stream);
                }

                return conf;
            }

            return null;
        }

        /// <summary>
        /// Export an .xml sample for the config file
        /// </summary>
        public static void ExportSample() {
            var conf = new Config {
                Proxies = new List<Proxy> {
                    new Proxy {
                        LocalAddress = "",
                        LocalPort = 666,
                        ExternalProxyRules = new List<ExternalProxyRule> {
                            new ExternalProxyRule {
                                UrlMatch = ".*cnaf.*",
                                ProxyHost = "192.168.213.137",
                                ProxyPort = 3128,
                                ProxyUsername = "",
                                ProxyPassword = ""
                            },
                            new ExternalProxyRule {
                                UrlMatch = ".*",
                                ProxyHost = "172.27.25.3",
                                ProxyPort = 8080,
                                ProxyUsername = "",
                                ProxyPassword = ""
                            }
                        }
                    }
                },
                TcpForwarders = new List<TcpForwarder> {
                    new TcpForwarder {
                        LocalAddress = "",
                        LocalPort = 667,
                        DistantAddress = "172.27.50.55",
                        DistantPort = 80
                    }
                },
                LogRules = new List<LogRule> {
                    new LogRule {
                        UrlMatch = ".*cnaf.*",
                        ClientIp = "192\\..*"
                    }
                }
            };
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            using (FileStream stream = File.OpenWrite(ConfigFilePath)) {
                serializer.Serialize(stream, conf);
            }
        }

        public void Open() {
            var process = new ProcessStartInfo(ConfigFilePath) {
                UseShellExecute = true
            };
            Process.Start(process);
        }
    }

    [XmlRoot(ElementName = "HttpProxy")]
    public class Proxy {
        [XmlElement(ElementName = "LocalAddress")]
        public string LocalAddress { get; set; }

        [XmlElement(ElementName = "LocalPort")]
        public int LocalPort { get; set; }

        [XmlArray(ElementName = "ExternalProxyRules")]
        public List<ExternalProxyRule> ExternalProxyRules { get; set; }
    }

    [XmlRoot(ElementName = "ExternalProxyRule")]
    public class ExternalProxyRule {
        [XmlElement(ElementName = "RegexUrlMatch")]
        public string UrlMatch { get; set; }

        [XmlElement(ElementName = "ProxyHost")]
        public string ProxyHost { get; set; }

        [XmlElement(ElementName = "ProxyPort")]
        public int ProxyPort { get; set; }

        [XmlElement(ElementName = "ProxyUsername")]
        public string ProxyUsername { get; set; }

        [XmlElement(ElementName = "ProxyPassword")]
        public string ProxyPassword { get; set; }
    }

    [XmlRoot(ElementName = "TcpForwarder")]
    public class TcpForwarder {
        [XmlElement(ElementName = "LocalAddress")]
        public string LocalAddress { get; set; }

        [XmlElement(ElementName = "LocalPort")]
        public int LocalPort { get; set; }

        [XmlElement(ElementName = "DistantAddress")]
        public string DistantAddress { get; set; }

        [XmlElement(ElementName = "DistantPort")]
        public int DistantPort { get; set; }
    }

    [XmlRoot(ElementName = "LogRule")]
    public class LogRule {
        [XmlElement(ElementName = "RegexUrlMatch")]
        public string UrlMatch { get; set; }

        [XmlElement(ElementName = "ClientIp")]
        public string ClientIp { get; set; }
    }
}