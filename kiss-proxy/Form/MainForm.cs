using System;
using System.Windows.Forms;

namespace kissproxy.Form {
    public partial class MainForm : System.Windows.Forms.Form {

        public MainForm() {
            InitializeComponent();
        }

        protected override void SetVisibleCore(bool value) {
            base.SetVisibleCore(false);
        }

        public void ShowBalloon(string text, string title, ToolTipIcon icon, int timeInSec) {
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.ShowBalloonTip(timeInSec);
        }

        private void openconfigToolStripMenuItem_Click(object sender, System.EventArgs e) {
            Program.OpenConfig();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Program.Exit();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e) {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null) {
                menuItem.Checked = !menuItem.Checked;
                Program.LogActivated = menuItem.Checked;
            }
        }

        private void restartServerToolStripMenuItem_Click(object sender, EventArgs e) {
            Program.RestartServers();
        }

        private void reloadtoolStripMenuItem2_Click(object sender, EventArgs e) {
            Program.LoadConfig();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e) {
            Program.ShowStartedServers();
        }
    }
}
