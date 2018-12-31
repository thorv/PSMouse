using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSMouse
{
    public partial class CmdEditForm : Form
    {
        MainForm mainf;
        SortBindingList<CmdPair> cmdPairs;
        public CmdEditForm(MainForm mf, CmdPair cp, SortBindingList<CmdPair> cpl)
        {
            InitializeComponent();
            mainf = mf;
            if (cp.cmd != 0)
            {
                tb_Char.Text = cp.cmd.ToString();
                tb_Char.ReadOnly = true;
                tbScripts.Select();
            }
            else
            {
                tb_Char.Text = String.Empty;
            }
            tbScripts.Text = cp.scripts;
            cmdPairs = cpl;
            lbAlreadyMsg.Text = "";
            if (tb_Char.Text.Equals(String.Empty))
            {
                bt_Ok.Enabled = false;
            }
        }

        private void bt_Ok_Click(object sender, EventArgs e)
        {
            CmdPair dlgCmd = mainf.dlgCmd;
            dlgCmd.cmd = tb_Char.Text.ToCharArray()[0];
            dlgCmd.hex = String.Format("0x{0:X02}", (int)dlgCmd.cmd);
            dlgCmd.scripts = tbScripts.Text;
            mainf.dlgCmd = dlgCmd;
        }

        private void tb_Char_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            try
            {
                if (!tb.ReadOnly)
                {
                    bt_Ok.Enabled = false;
                    lbAlreadyMsg.Text = String.Format(String.Empty, tb.Text);
                    char c = e.KeyChar;
                    tb.Text = String.Format("{0} (0x{1:X2})", c, (int)c);
                    foreach (var cmdPair in cmdPairs)
                    {
                        if (cmdPair.cmd == c)
                        {
                            lbAlreadyMsg.Text = String.Format("'{0}' is already used.", tb.Text);
                            tb.Text = "";
                            break;
                        }
                    }
                }
            }
            catch { }
            finally
            {
                e.Handled = true;
                bt_Ok.Enabled = !tb.Text.Equals(String.Empty);
            }

        }

        private void tbScripts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                bt_Ok_Click(null,null);
                this.DialogResult = DialogResult.OK;
                this.Close();
                this.Dispose();
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainf.helpToolStripMenuItem_ClickAsync(null, null);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ts_Mouse.Text = "Mouse: " + Control.MousePosition.ToString();
        }

        private void CmdEditForm_Load(object sender, EventArgs e)
        {
            timer1.Interval = 100;
            timer1.Enabled = true;
            timer1.Start();
        }
    }
}
