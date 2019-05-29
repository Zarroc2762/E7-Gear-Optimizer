using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E7_Gear_Optimizer
{
    public partial class SelectItemDialog : Form
    {
        List<Item> items;

        public SelectItemDialog()
        {
            InitializeComponent();

        }

        public SelectItemDialog(List<Item> items)
        {
            InitializeComponent();
            this.items = items;
        }

        private void SelectItemDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                button1.PerformClick();
            }
            e.Handled = true;
        }

        public Item getSelectedItem()
        {
            return items.Find(x => x.ID == (string)dgv_Inventory.Rows[dgv_Inventory.SelectedCells[0].RowIndex].Cells["C_ItemID"].Value);
        }

        private void SelectItemDialog_Shown(object sender, EventArgs e)
        {
            foreach (Item item in items)
            {
                object[] values = new object[dgv_Inventory.ColumnCount];
                values[0] = Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense")), 25, 25);
                values[19] = (int)item.Set;
                values[1] = Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject(item.Type.ToString().ToLower()), 25, 25);
                values[20] = (int)item.Type;
                values[2] = item.Grade.ToString();
                values[3] = item.ILvl;
                values[4] = "+" + item.Enhance.ToString();
                values[5] = item.Main.Name.ToString().Replace("Percent", "%");
                values[6] = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                values[18] = item.Equipped == null ? "" : item.Equipped.Name;
                for (int i = 0; i < item.SubStats.Length; i++)
                {
                    values[(int)item.SubStats[i].Name + 7] = item.SubStats[i].Value < 1 ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                }
                for (int i = 7; i < 18; i++)
                {
                    if (values[i] == null)
                    {
                        values[i] = "";
                    }
                }
                values[21] = item.ID;
                dgv_Inventory.Rows.Add(values);
            }
        }

        private void Dgv_Inventory_DoubleClick(object sender, EventArgs e)
        {
            button1.PerformClick();
        }
    }
}
