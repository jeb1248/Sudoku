using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YAS
{
    public partial class FrmSelectCandidate : Form
    {
        public FrmSelectCandidate(List<int> CList)
        {
            InitializeComponent();

            LoadListView(CList);
        }

        private void LoadListView(List<int> CList)
        {
            int cnt = CList.Count;
            string str;

            //    Point newloc = new Point();

            int viewwidth = 135;
            this.Location = new Point(700, 300);
            lv2.Width = lv1.Width = viewwidth;
            this.Width = viewwidth + 5;

            int viewheight = cnt * 34;
            lv2.Height = lv1.Height = viewheight;
            this.Height = (viewheight *2)+ 5; ;

            lv1.Top = 3;
            lv2.Top = lv1.Bottom;
            lv2.Left = lv1.Left = 3;

            lv1.View = View.Details;
            lv1.Columns.Add("Column1Name");
            lv1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            
            lv2.View = View.Details;
            lv2.Columns.Add("Column1Name");
            lv2.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

            for (int i=0; i<cnt; i++) 
            {
                str = "Make " + (CList[i] + 1).ToString();
                lv1.Items.Add(new ListViewItem(str));
            }
            lv1.Columns[0].Width = -1;

            for (int i = 0; i < cnt; i++)
            {
                str = "Exclude " + (CList[i] + 1).ToString()+"'s";
                lv2.Items.Add(new ListViewItem(str));
            }
            lv2.Columns[0].Width = -1;
        }

        private void Lv1_Click(object sender, EventArgs e)
        {
            makeitem = GetMakeNum(lv1.SelectedItems[0].Text);
            // This gets you out
            this.DialogResult = DialogResult.OK;
        }

        private void Lv2_Click(object sender, EventArgs e)
        {
            excludeditem = GetExcludeNum(lv2.SelectedItems[0].Text);
            // This gets you out
            this.DialogResult = DialogResult.OK;
        }

        public int GetMakeNum(string s)
        {// ascii 0 = '48' -- 9 = '57' decimal
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            char ch = charArray[0];
            return (ch - '0');
        }

        public int GetExcludeNum(string s)
        {// ascii 0 = '48' -- 9 = '57' decimal
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            char ch = charArray[2];
            return (ch - '0');
        }

        private int makeitem=0;

        public int MakeItem
        {
            get
            { return makeitem; }
        }

        private int excludeditem=0;

        public int ExcludedItem
        {
            get
            { return excludeditem; }
        }

        private void Lv1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // This get you out - Escape key
            if(e.KeyChar == 27)
                this.DialogResult = DialogResult.OK;
        }

        private void Lv2_KeyPress(object sender, KeyPressEventArgs e)
        {
            // This get you out - Escape key
            if (e.KeyChar == 27)
                this.DialogResult = DialogResult.OK;
        }

    }   // End class FrmSelectCandidate : Form
}   // End namespace YAS
