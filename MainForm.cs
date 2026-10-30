using System;
using System.Drawing;
using System.Windows.Forms;
using NetFix.Core;

namespace NetFix
{
    public class MainForm : Form
    {
        private Button _btnDiag;
        private Button _btnRepair;
        private TextBox _output;
        private Label _status;

        public MainForm()
        {
            Text = "NetFix - Network Doctor";
            Width = 820;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            _btnDiag = new Button();
            _btnDiag.Text = "Run diagnosis";
            _btnDiag.Width = 140;
            _btnDiag.Height = 36;
            _btnDiag.Top = 12;
            _btnDiag.Left = 12;
            _btnDiag.Click += OnDiag;

            _btnRepair = new Button();
            _btnRepair.Text = "One-click repair";
            _btnRepair.Width = 140;
            _btnRepair.Height = 36;
            _btnRepair.Top = 12;
            _btnRepair.Left = 160;
            _btnRepair.BackColor = Color.LightSkyBlue;
            _btnRepair.Click += OnRepair;

            _status = new Label();
            _status.Text = "Ready. Run diagnosis first, then repair if needed.";
            _status.Top = 20;
            _status.Left = 320;
            _status.Width = 460;
            _status.ForeColor = Color.DimGray;

            _output = new TextBox();
            _output.Multiline = true;
            _output.ScrollBars = ScrollBars.Vertical;
            _output.ReadOnly = true;
            _output.Top = 56;
            _output.Left = 12;
            _output.Width = 780;
            _output.Height = 490;
            _output.Font = new Font("Consolas", 9.5f);
            _output.WordWrap = false;

            Controls.Add(_btnDiag);
            Controls.Add(_btnRepair);
            Controls.Add(_status);
            Controls.Add(_output);
        }

        private void Log(string s)
        {
            _output.AppendText(s + Environment.NewLine);
        }

        private void OnDiag(object sender, EventArgs e)
        {
            _output.Clear();
            _status.Text = "Diagnosing...";
            Log("=== NetFix diagnosis " + DateTime.Now.ToString() + " ===");
            Log("");
            var results = NetDiag.RunAll();
            int fails = 0, warns = 0;
            foreach (DiagResult r in results)
            {
                string tag;
                Color c;
                switch (r.Level)
                {
                    case DiagLevel.Ok: tag = "[ OK ]"; c = Color.Green; break;
                    case DiagLevel.Warn: tag = "[WARN]"; c = Color.DarkOrange; warns++; break;
                    case DiagLevel.Fail: tag = "[FAIL]"; c = Color.Firebrick; fails++; break;
                    default: tag = "[INFO]"; c = Color.DimGray; break;
                }
                Log(tag + " " + r.Check + " - " + r.Detail);
            }
            Log("");
            if (fails == 0 && warns == 0)
            {
                _status.Text = "All checks passed.";
                Log("All checks passed. No obvious network problem detected.");
            }
            else
            {
                _status.Text = fails + " fail(s), " + warns + " warning(s). Click 'One-click repair'.";
                Log(fails + " fail(s), " + warns + " warning(s). Click 'One-click repair' to try the common fixes.");
            }
        }

        private void OnRepair(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                "This will:\n" +
                "  - Flush DNS cache\n" +
                "  - Reset Winsock and TCP/IP stack (a reboot is required after)\n" +
                "  - Disable system proxy\n" +
                "  - Comment out custom hosts entries (a backup is saved)\n\n" +
                "Continue?",
                "Confirm repair", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _output.Clear();
            _status.Text = "Repairing...";
            Log("=== NetFix repair " + DateTime.Now.ToString() + " ===");
            Log("");
            var steps = NetRepair.RunAll();
            foreach (RepairStep s in steps)
            {
                Log((s.Ok ? "[ OK ]" : "[FAIL]") + " " + s.Name);
                if (!string.IsNullOrEmpty(s.Output)) Log("       " + s.Output);
            }
            Log("");
            Log("Repair done. A reboot is recommended for Winsock/TCP resets to take effect.");
            _status.Text = "Repair done. Reboot recommended.";
        }
    }
}
