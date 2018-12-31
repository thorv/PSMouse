using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSMouse
{
    [Serializable()]
    public struct CmdPair
    {
        public char cmd { get; set; }
        public string hex { get; set; }
        public string scripts { get; set; }

        public CmdPair(char c, string s)
        {
            cmd = c;
            hex = String.Format("0x{0:X02}", (int)c);
            scripts = s;
        }
    }
    public class PSMouse
    {
        public SortBindingList<CmdPair> cmds { get; set; }
        private SerialPort port;
        public PSMouse()
        {
            cmds = Properties.Settings.Default.CmdPairs;
            if (cmds == null)
            {
                cmds = new SortBindingList<CmdPair>();
            }
            cmds.AllowNew=true;
            cmds.AllowEdit=true;
            cmds.AllowRemove=true;
            port = new SerialPort();
            port.DataReceived += onReceivedData;
        }
        public void Add(char c, String s)
        {
            cmds.Add(new CmdPair(c, s));
        }
        public bool Open(string portName,int baudrate)
        {
            try
            {
                port.PortName = portName;
                port.BaudRate = baudrate;
                port.Open();
                Thread.Sleep(100);
            }
            catch
            {
                MessageBox.Show("Port Open ERROR!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                port.Close();
            }
            return port.IsOpen;
        }
        public void Close()
        {
            port.Close();
        }
        public void baudrate(int baud)
        {
            port.BaudRate = baud;
        }
        public event EventHandler<int> RcvdEvent;
        private void onReceivedData(Object e, SerialDataReceivedEventArgs arg)
        {
            String rcvd= port.ReadExisting();
            foreach(var rr in rcvd)
            {
                foreach (var icmd in cmds.Select((str, i) => new { str, i }))
                {
                    if (icmd.str.cmd == rr)
                    {
                        Task.Run(() =>
                        {
                            Scripts sc;
                            if (RcvdEvent != null)
                            {
                                RcvdEvent(this, icmd.i);

                            }
                            sc = new Scripts(icmd.str.scripts);
                            sc.doScripts();
                        });
                        break;
                    }
                }

            }
        }
    }
    public class Comparer<T> : IComparer<T>
    {
        private ListSortDirection _dir;
        private PropertyDescriptor _prop;
        public Comparer(PropertyDescriptor prop, ListSortDirection dir)
        {
            _prop = prop;
            _dir = dir;
        }

        public int Compare(T obj1, T obj2)
        {
            object valX = GetPropValue(obj1, this._prop.Name);
            object valY = GetPropValue(obj2, this._prop.Name);

            if(_dir == ListSortDirection.Ascending)
            {
                return Convert.ToInt16(valX)-Convert.ToInt16(valY);
            }
            else
            {
                return Convert.ToInt16(valY) - Convert.ToInt16(valX);
            }
        }
        private int CompareAsc(object valX, object valY)
        {
            return valX.ToString().CompareTo(valY.ToString());
        }
        private int CompareDesc(object valX, object valY)
        {
            return -CompareAsc(valX, valY);
        }
        private object GetPropValue(T val, string prop)
        {
            PropertyInfo propInfo = val.GetType().GetProperty(prop);
            return propInfo.GetValue(val, null);
        }
    }
    public class SortBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private PropertyDescriptor _sortProp;
        private ListSortDirection _sortDir;
        public SortBindingList( T[] items)
        {
            ((List<T>)base.Items).AddRange(items);
        }
        public SortBindingList()
        {
        }
        protected override bool SupportsSortingCore
        {
            get
            {
                return true;
            }
        }
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            //ソートするためバインドされているリストを取得します。
            List<T> items = base.Items as List<T>;

            //リストがあった場合、
            //Comparerを生成してソート項目と項目名を渡します。
            if (items != null)
            {
                Comparer<T> sc = new Comparer<T>(prop, direction);
                items.Sort(sc);
                //ソート済みに設定します。
                this._isSorted = true;
            }
            else
            {
                this._isSorted = false;
            }

            //ソート結果、方向を保持しておきます。
            this._sortProp = prop;
            this._sortDir = direction;

            //リストが変更（ソート）されたことをイベント通知します。
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, prop));
        }
    }
}
