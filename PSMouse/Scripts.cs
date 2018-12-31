using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSMouse
{
    public class Scripts
    {
        private static readonly object syncObject = new object();
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x1;
        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;
        private const int MOUSEEVENTF_WHEEL = 0x800;

        public String myScript { get; set; }
        Boolean isBreak;
        class ActionItem
        {
            public String cmd { get; set; }
            public delegate void actionDelegate(String data);
            public actionDelegate func { get; set; }
            public ActionItem(String cmd, actionDelegate func)
            {
                this.cmd = cmd;
                this.func = func;
            }
        }

        ActionItem[] ActionItems;

        public Scripts(String script)
        {
            myScript = script;
            ActionItems = new ActionItem[] {
            new ActionItem("wt", doWait),
            new ActionItem("ml", doMouseClickL),
            new ActionItem("mc", doMouseClickCT),
            new ActionItem("mr", doMouseClickR),
            new ActionItem("mm", doMouseMoveAbsolute),
            new ActionItem("md", doMouseMoveDifferential),
            new ActionItem("mw", doMouseWheel),
            new ActionItem("bp", doBeep)
            };
        }
        delegate void showCommandDelegate(String str);
        public void doScripts()
        {
            System.IO.StringReader rs = new System.IO.StringReader(myScript);
            isBreak = false;
            while (rs.Peek() > -1)
            {
                String[] cmds = rs.ReadLine().Split(';');
                for (int i = 0; i < cmds.Length; i++)
                {
                    if (isBreak)
                    {
                        break;
                    }
                    cmds[i] = cmds[i].Trim();
                    String data = "";
                    if (cmds[i].Length > 2)
                    {
                        data = cmds[i].Substring(2).Trim();
                    }
                    String cmd = "";
                    if (cmds[i].Length >= 2)
                    {
                        cmd = cmds[i].Substring(0, 2).ToLower();
                    }
                    if (String.Equals("//", cmd))
                    {
                        break; //行末まで読み飛ばし
                    }
                    if (String.IsNullOrEmpty(cmd))
                    {
                        continue; //空のコマンドは次の;まで読み飛ばし
                    }
                    bool found = false;
                    foreach (ActionItem item in ActionItems)
                    {
                        if (String.Equals(item.cmd, cmd))
                        {
                            item.func(data);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        MessageBox.Show("Command Error (" + cmd + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            rs.Close();
        }
        public void doWait(String data)
        {
            try
            {
                int t = 0;
                while (t < Double.Parse(data) * 1000)
                {
                    if (isBreak)
                    {
                        break;
                    }
                    t += 100;
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch
            {
                MessageBox.Show("Parameter Error(wait:" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        public void doMouseClickL(string data)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }
        public void doMouseClickCT(string data)
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }
        public void doMouseClickR(string data)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベントを発生させる

        }

        public void doMouseMoveAbsolute(string data)
        {
            try
            {
                String[] pos = data.Split(',');
                int mouseX = int.Parse(pos[0]);
                int mouseY = int.Parse(pos[1]);
                SetCursorPos(mouseX, mouseY);
            }
            catch
            {
                MessageBox.Show("Parameter Error(move mouse :" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void doMouseMoveDifferential(string data)
        {
            try
            {
                String[] pos = data.Split(',');
                int mouseX = int.Parse(pos[0]);
                int mouseY = int.Parse(pos[1]);
                mouse_event(MOUSEEVENTF_MOVE, mouseX, mouseY, 0, 0);
            }
            catch
            {
                MessageBox.Show("Parameter Error(move mouse differetial :" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void doMouseWheel(string data)
        {
            try
            {
                int dw = int.Parse(data);
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, dw, 0);
            }
            catch
            {
                MessageBox.Show("Parameter Error(move mouse differetial :" + data + ")", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void doBeep(string data)
        {
            System.Media.SystemSound[] beep = {
                System.Media.SystemSounds.Asterisk,
                System.Media.SystemSounds.Beep,
                System.Media.SystemSounds.Exclamation,
                System.Media.SystemSounds.Hand,
                System.Media.SystemSounds.Question
            };
            /*
             * [コントロールパネル]-[サウンド]の設定との対応
             Asterisk:メッセージ（情報）
             Beep:一般の警告音
             Exclamation:メッセージ（警告）
             Hand:システムエラー
             Question:メッセージ（問い合わせ）
            */

            int n=0;
            Int32.TryParse(data, out n);
            if (n < 0 || n >= beep.Length) { n = 0; }
            beep[n].Play();
        }

        public void setBreak()
        {
            isBreak = true;
        }
    }
}
