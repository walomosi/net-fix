using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;

namespace NetFix.Core
{
    public enum DiagLevel { Ok, Warn, Fail, Info }

    public class DiagResult
    {
        public DiagLevel Level;
        public string Check;
        public string Detail;
    }

    internal static class NetDiag
    {
        public static List<DiagResult> RunAll()
        {
            var results = new List<DiagResult>();
            results.Add(CheckIpConfig());
            results.Add(CheckGateway());
            results.Add(CheckDnsResolution());
            results.Add(CheckInternet());
            results.Add(CheckHostsFile());
            results.Add(CheckProxy());
            return results;
        }

        private static DiagResult CheckIpConfig()
        {
            try
            {
                string firstIp = "n/a", firstDns = "n/a";
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    IPInterfaceProperties ip = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation u in ip.UnicastAddresses)
                    {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            firstIp = u.Address.ToString();
                            break;
                        }
                    }
                    foreach (IPAddress dns in ip.DnsAddresses)
                    {
                        if (dns.AddressFamily == AddressFamily.InterNetwork)
                        {
                            firstDns = dns.ToString();
                            break;
                        }
                    }
                    if (firstIp != "n/a") break;
                }
                return new DiagResult
                {
                    Level = DiagLevel.Info,
                    Check = "IP configuration",
                    Detail = "IPv4: " + firstIp + ", DNS: " + firstDns
                };
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "IP configuration", Detail = ex.Message };
            }
        }

        private static DiagResult CheckGateway()
        {
            try
            {
                string gw = null;
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    IPInterfaceProperties ip = nic.GetIPProperties();
                    foreach (GatewayIPAddressInformation g in ip.GatewayAddresses)
                    {
                        if (g.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gw = g.Address.ToString();
                            break;
                        }
                    }
                    if (gw != null) break;
                }
                if (gw == null) return new DiagResult { Level = DiagLevel.Warn, Check = "Default gateway", Detail = "No IPv4 gateway found" };

                using (Ping p = new Ping())
                {
                    PingReply r = p.Send(gw, 2000);
                    if (r.Status == IPStatus.Success)
                        return new DiagResult { Level = DiagLevel.Ok, Check = "Gateway reachable", Detail = gw + " in " + r.RoundtripTime + "ms" };
                    return new DiagResult { Level = DiagLevel.Fail, Check = "Gateway reachable", Detail = gw + " unreachable: " + r.Status };
                }
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "Gateway", Detail = ex.Message };
            }
        }

        private static DiagResult CheckDnsResolution()
        {
            try
            {
                IPAddress[] addrs = Dns.GetHostAddresses("www.bing.com");
                foreach (IPAddress a in addrs)
                {
                    if (a.AddressFamily == AddressFamily.InterNetwork)
                        return new DiagResult { Level = DiagLevel.Ok, Check = "DNS resolution", Detail = "www.bing.com -> " + a.ToString() };
                }
                return new DiagResult { Level = DiagLevel.Fail, Check = "DNS resolution", Detail = "No A record returned" };
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "DNS resolution", Detail = "Failed: " + ex.Message };
            }
        }

        private static DiagResult CheckInternet()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.msftconnecttest.com/connecttest.txt");
                req.Timeout = 5000;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader sr = new StreamReader(s))
                {
                    string body = sr.ReadToEnd().Trim();
                    if (body == "Microsoft Connect Test")
                        return new DiagResult { Level = DiagLevel.Ok, Check = "Internet reachable", Detail = "HTTPS to msftconnecttest.com OK" };
                    return new DiagResult { Level = DiagLevel.Warn, Check = "Internet reachable", Detail = "Unexpected response: " + body };
                }
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "Internet reachable", Detail = "HTTPS test failed: " + ex.Message };
            }
        }

        private static DiagResult CheckHostsFile()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                string[] lines = File.ReadAllLines(path);
                int custom = 0;
                string sample = null;
                foreach (string line in lines)
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    custom++;
                    if (sample == null) sample = t;
                }
                if (custom == 0)
                    return new DiagResult { Level = DiagLevel.Ok, Check = "Hosts file", Detail = "No custom entries" };
                return new DiagResult { Level = DiagLevel.Warn, Check = "Hosts file", Detail = custom + " custom entr" + (custom == 1 ? "y" : "ies") + " (e.g. " + sample + ") - may block or redirect domains" };
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "Hosts file", Detail = ex.Message };
            }
        }

        private static DiagResult CheckProxy()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (k == null) return new DiagResult { Level = DiagLevel.Info, Check = "System proxy", Detail = "No key" };
                    int enable = Convert.ToInt32(k.GetValue("ProxyEnable", 0));
                    string server = Convert.ToString(k.GetValue("ProxyServer", ""));
                    if (enable == 0)
                        return new DiagResult { Level = DiagLevel.Ok, Check = "System proxy", Detail = "Disabled" };
                    return new DiagResult { Level = DiagLevel.Warn, Check = "System proxy", Detail = "Enabled: " + server + " - may break connectivity if the proxy is down" };
                }
            }
            catch (Exception ex)
            {
                return new DiagResult { Level = DiagLevel.Fail, Check = "System proxy", Detail = ex.Message };
            }
        }
    }

}
