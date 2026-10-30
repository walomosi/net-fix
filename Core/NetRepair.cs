using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace NetFix.Core
{
    public class RepairStep
    {
        public string Name;
        public bool Ok;
        public string Output;
    }

    internal static class NetRepair
    {
        // Order matters: flush DNS first (cheap, non-disruptive), then reset Winsock/TCP
        // which drops all connections, then clear proxy/hosts which are user-level overrides.
        public static System.Collections.Generic.List<RepairStep> RunAll()
        {
            var steps = new System.Collections.Generic.List<RepairStep>();
            steps.Add(Run("Flush DNS cache", "ipconfig", "/flushdns"));
            steps.Add(Run("Reset Winsock", "netsh", "winsock reset"));
            steps.Add(Run("Reset TCP/IP stack", "netsh", "int ip reset"));
            steps.Add(DisableProxy());
            steps.Add(ClearHostsCustom());
            return steps;
        }

        private static RepairStep Run(string name, string exe, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    string errp = p.StandardError.ReadToEnd();
                    p.WaitForExit(15000);
                    string combined = (outp + " " + errp).Trim();
                    if (combined.Length > 200) combined = combined.Substring(0, 200) + "...";
                    return new RepairStep { Name = name, Ok = p.ExitCode == 0, Output = combined };
                }
            }
            catch (Exception ex)
            {
                return new RepairStep { Name = name, Ok = false, Output = ex.Message };
            }
        }

        private static RepairStep DisableProxy()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    if (k == null) return new RepairStep { Name = "Disable system proxy", Ok = false, Output = "Registry key not found" };
                    k.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    return new RepairStep { Name = "Disable system proxy", Ok = true, Output = "ProxyEnable set to 0" };
                }
            }
            catch (Exception ex)
            {
                return new RepairStep { Name = "Disable system proxy", Ok = false, Output = ex.Message };
            }
        }

        private static RepairStep ClearHostsCustom()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                string backup = path + ".netfix-bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(path, backup, true);
                string[] lines = File.ReadAllLines(path);
                using (StreamWriter sw = new StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
                {
                    foreach (string line in lines)
                    {
                        string t = line.Trim();
                        if (t.Length == 0 || t.StartsWith("#"))
                        {
                            sw.WriteLine(line);
                        }
                        else
                        {
                            sw.WriteLine("# [netfix removed] " + line);
                        }
                    }
                }
                return new RepairStep { Name = "Clear custom hosts entries", Ok = true, Output = "Backed up to " + Path.GetFileName(backup) + ", custom entries commented out" };
            }
            catch (Exception ex)
            {
                return new RepairStep { Name = "Clear custom hosts entries", Ok = false, Output = ex.Message };
            }
        }
    }
}
