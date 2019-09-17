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
            dgv_Inventory.Columns[0].SortMode = DataGridViewColumnSortMode.Programmatic;

        }

        public SelectItemDialog(List<Item> items)
        {
            InitializeComponent();
            this.items = items;
            dgv_Inventory.Columns[0].SortMode = DataGridViewColumnSortMode.Programmatic;
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
            _redrawDgv();
        }

        private void _redrawDgv()
        {
            // Store previous sorting options
            var sortedColumn = dgv_Inventory.SortedColumn;
            var sortOrder = dgv_Inventory.SortOrder;
            dgv_Inventory.Rows.Clear();
            foreach (Item item in items)
            {
                if (item.Equipped != null && !cb_ShowEquippedItems.Checked)
                {
                    continue;
                }
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
            if (sortedColumn != null)
            {
                dgv_Inventory.Sort(sortedColumn, sortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
        }

        private void Dgv_Inventory_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                dgv_Inventory.Sort(dgv_Inventory.Columns[e.ColumnIndex], dgv_Inventory.SortOrder == SortOrder.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending);
                dgv_Inventory.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = dgv_Inventory.SortOrder;
            }
        }

        private void Dgv_Inventory_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (Util.percentageColumns.Contains(e.Column.Name))
            {
                int cell1 = 0;
                int cell2 = 0;
                int.TryParse(e.CellValue1.ToString().Replace("%", "").Replace("+", ""), out cell1);
                int.TryParse(e.CellValue2.ToString().Replace("%", "").Replace("+", ""), out cell2);
                e.SortResult = cell1.CompareTo(cell2);
                e.Handled = true;
            }
            else if (e.Column.Index == 0)
            {
                Set cell1 = (Set)dgv_Inventory["c_SetID", e.RowIndex1].Value;
                Set cell2 = (Set)dgv_Inventory["c_SetID", e.RowIndex2].Value;
                e.SortResult = cell1.CompareTo(cell2);
                e.Handled = true;
            }
        }

        private void Dgv_Inventory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv_Inventory.Columns[e.ColumnIndex].HeaderText == "Grade")
            {
                switch (e.Value.ToString())
                {
                    case "Epic":
                        e.CellStyle.ForeColor = Color.Red;
                        break;
                    case "Heroic":
                        e.CellStyle.ForeColor = Color.Purple;
                        break;
                    case "Rare":
                        e.CellStyle.ForeColor = Color.Blue;
                        break;
                    case "Good":
                        e.CellStyle.ForeColor = Color.Green;
                        break;
                    default:
                        break;
                }
            }
        }

        private void Dgv_Inventory_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                button1.PerformClick();
            }
        }

        private void Cb_ShowEquippedItems_CheckedChanged(object sender, EventArgs e)
        {
            _redrawDgv();
        }
    }
}
