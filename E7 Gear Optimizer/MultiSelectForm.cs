using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E7_Gear_Optimizer
{
    public partial class MultiSelectForm : Form
    {
        public MultiSelectForm(string[] items)
        {
            InitializeComponent();

            listBox1.Items.Clear();
            listBox1.Items.AddRange(items);
            listBox1.Height = listBox1.PreferredHeight;

            this.Height = listBox1.ItemHeight * items.Length + b_OK.Height + 4;
        }

        public ListBox.SelectedObjectCollection SelectedItems => listBox1.SelectedItems;

        private void Btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Btn_Clear_Click(object sender, EventArgs e)
        {
            listBox1.SelectedItems.Clear();
            this.Close();
        }

        private void MultiSelectForm_KeyDown(object sender, KeyEventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
