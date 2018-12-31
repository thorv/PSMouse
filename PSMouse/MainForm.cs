using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSMouse
{
    public partial class MainForm : Form
    {
        readonly private int HeightMin;
        PSMouse psm;
        bool portOpen = false;
        public MainForm(PSMouse psMouse)
        {
            psm = psMouse;
            HeightMin = Size.Height;
            Text = "PSMouse " + Program.VERSION;

            InitializeComponent();
        }
        private void DrawForm()
        {
            int btWidth = btAdd.Width;
            int btHeight = btAdd.Height;
            int csw = this.ClientSize.Width;
            int csh = this.ClientSize.Height;
            const int btInterval = 10;
            dgv.Size = new Size(csw - btWidth-btInterval*2, csh);
            Point pt= new Point(csw - btWidth - btInterval, menuStrip1.Height + btInterval);
            btAdd.Location = pt;
            pt = new Point(pt.X, pt.Y + btInterval + btHeight);
            btEdit.Location = pt;
            pt = new Point(pt.X, pt.Y + btInterval + btHeight);
            btRemove.Location = pt;
            pt = new Point(pt.X, this.ClientSize.Height - btInterval - btHeight);
            cbBaudrate.Location = pt;
            pt = new Point(pt.X, pt.Y - btInterval - btHeight);
            cbComPort.Location = pt;
            pt = new Point(pt.X, pt.Y - btInterval - btHeight);
            bt_Test.Location = pt;
            pt = new Point(pt.X, pt.Y - btInterval - btStart.Height);
            btStart.Location = pt;

            cbBaudrate.SelectedText=Properties.Settings.Default.baudrate.ToString();

            psm.RcvdEvent += onRcvd;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (Size.Height < HeightMin)
            {
                Size=new Size(Size.Width, HeightMin);
            }
            DrawForm();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DrawForm();
            dgv.DataSource = psm.cmds;
            dgv.Columns[0].HeaderText = "Charactor";
            dgv.Columns[1].HeaderText = "Hex";
            dgv.Columns[2].HeaderText = "Scripts";
            dgv.Columns[0].Width = 70;
            dgv.Columns[1].Width = 70;
            dgv.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            SortedDictionary<String, String> serList = GetComPorts(true);
            foreach(var comp in serList)
            {
                if (comp.Key == Properties.Settings.Default.port)
                {
                    cbComPort.Text = comp.Key;
                    break;
                }
            }
        }

        bool isShowHelp = false;
        internal async void helpToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (!isShowHelp)
            {
                isShowHelp = true;
                await Task.Run(() =>
                {
                    MessageBox.Show(
                        "wt sec : wait time in sec(float)\r\n" +
                        "mm x,y : mouse move to x,y\r\n" +
                        "md x,y : mouse moves x,y from current position\r\n" +
                        "mw val : mouse wheel rotate\r\n" +
                        "ml     : mouse left button click\r\n" +
                        "mc     : mouse center button click\r\n" +
                        "mr     : mouse right button click\r\n" +
                        "bp n   : Beep system sound. n=1..5\n" +
                        ";      : command delimiter\r\n"
                        , "Script help", MessageBoxButtons.OK);
                    isShowHelp = false;
                });

            }

        }
        internal CmdPair dlgCmd;
        private void btAdd_Click(object sender, EventArgs e)
        {
            editCmds( new CmdPair('\0', string.Empty));
        }
        private void editCmds(CmdPair cp, int idx=-1)
        {
            dlgCmd = new CmdPair();
            CmdEditForm dlg = new CmdEditForm(this, cp, psm.cmds);
            DialogResult result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (idx == -1)
                {
                    ((SortBindingList<CmdPair>)dgv.DataSource).Add(dlgCmd);
                }
                else
                {
                    ((SortBindingList<CmdPair>)dgv.DataSource)[idx]=dlgCmd;
                }
                dgv.Sort(dgv.Columns[0], ListSortDirection.Ascending);
                Properties.Settings.Default.CmdPairs = psm.cmds;
                Properties.Settings.Default.Save();
            }
        }

        private void btRemove_Click(object sender, EventArgs e)
        {
            int row = dgv.CurrentCell.RowIndex;
            var ret = MessageBox.Show("Delete. Sure? ", "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            if (ret == DialogResult.OK)
            {
                dgv.Rows.RemoveAt(row);
            }
            Properties.Settings.Default.CmdPairs = psm.cmds;
            Properties.Settings.Default.Save();
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (portOpen)
            {
                psm.Close();
                portOpen = false;
                btStart.Text = "START";
                cbBaudrate.Enabled = true;
                cbComPort.Enabled = true;
            }
            else
            {
                string com = (string)cbComPort.Text;
                int baud = Int32.Parse(cbBaudrate.Text);
                portOpen = psm.Open(cbComPort.Text, baud);
                if (portOpen)
                {
                    btStart.Text = "STOP";
                    cbBaudrate.Enabled = false;
                    cbComPort.Enabled = false;
                    Properties.Settings.Default.port = com;
                    Properties.Settings.Default.baudrate = baud;
                    Properties.Settings.Default.Save();
                }
            }
        }

        // シリアルポートリスト デバイス名付き
        static private SortedDictionary<String, String> PortsTable;
        public static SortedDictionary<string, string> GetComPorts(bool refresh = false)
        {
            if (PortsTable == null || refresh)
            {
                PortsTable = new SortedDictionary<string, string>(new SortRule());
                var mcW32Devs = new ManagementClass("Win32_PnPEntity");
                var ports = SerialPort.GetPortNames().ToList<String>();
                var portlist = ports.Distinct();//portsに重複がある場合があるので取り除く
                var dev = mcW32Devs.GetInstances().Cast<ManagementObject>().ToList();
                var caps = dev.Where(x => ((string)(x.GetPropertyValue("PNPClass")) ?? string.Empty).Contains("Ports"));
                try
                {
                    foreach (var port in portlist)
                    {
                        var cap = caps.Where(x => ((string)(x.GetPropertyValue("Caption"))).Contains(port));
                        PortsTable.Add(port, ((string)(cap.First().GetPropertyValue("Caption"))));
                    }
                }
                catch (System.InvalidOperationException ex)
                {
                    PortsTable = new SortedDictionary<string, string>();
                }
            }
            return PortsTable;
        }
        public sealed class SortRule : IComparer<String>
        {
            public int Compare(String x, String y)
            {
                try
                {
                    int xx;
                    int yy;
                    Int32.TryParse(x.Substring(3), out xx);
                    Int32.TryParse(y.Substring(3), out yy);
                    return xx - yy;
                }
                catch
                {
                    return 0;
                }
            }
        }
        private void cbComPort_DropDown(object sender, EventArgs e)
        {
            SortedDictionary<String, String> serList = GetComPorts(true);
            cbComPort.Items.Clear();
            foreach (var port in serList)
            {
                cbComPort.Items.Add(port.Key);

            }

        }

        private void cbComPort_DrawItem(object sender, DrawItemEventArgs e)
        {
            SortedDictionary<String, String> serList = GetComPorts();
            int index = e.Index;
            string txt = e.Index > -1 ? cbComPort.Items[e.Index].ToString() : cbComPort.Text;
            e.DrawBackground();
            if (index >= 0)
            {
                e.Graphics.DrawString(txt,
                                       e.Font,
                                       System.Drawing.Brushes.Black,
                                       e.Bounds.X, e.Bounds.Y);
                {
                    toolTip1.Show(serList[(string)cbComPort.Items[index]],
                        cbComPort, cbComPort.PointToClient(Cursor.Position).X + 20,
                        cbComPort.PointToClient(Cursor.Position).Y);
                }
            }
        }

        private void cbComPort_DropDownClosed(object sender, EventArgs e)
        {
            toolTip1.Hide(cbComPort);
        }

        private void cbComPort_SerectedIndexChanged(object sender, EventArgs e)
        {
            SortedDictionary<String, String> serList = GetComPorts();
            if (cbComPort.SelectedIndex < 0)
            {
                this.toolTip1.Hide(cbComPort);
            }
            else
            {
                this.toolTip1.SetToolTip(cbComPort, serList[(string)cbComPort.SelectedItem]);
            }

        }

        private void bt_Test_Click(object sender, EventArgs e)
        {
            int raw;
            if (dgv.SelectedCells.Count == 0)
            {
                raw = 0;
            }
            else
            {
                raw = dgv.SelectedCells[0].RowIndex;
            }
            Scripts sc= new Scripts(psm.cmds[raw].scripts);
            sc.doScripts();
        }

        private void btEdit_Click(object sender, EventArgs e)
        {
            int raw;
            if (dgv.SelectedCells.Count == 0)
            {
                raw = 0;
            }
            else
            {
                raw = dgv.SelectedCells[0].RowIndex;
            }
            editCmds(psm.cmds[raw],raw);
        }

        private void cbBaudrate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int baud = Convert.ToInt32(cbBaudrate.SelectedItem);
            psm.baudrate(baud);
        }
        void onRcvd(Object e, Int32 i)
        {
            dgv.Rows[i].Selected = true;
        }
    }

}
