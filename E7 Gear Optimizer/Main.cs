using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace E7_Gear_Optimizer
{
    public partial class Main : Form
    {
        private Data data = new Data();
        private bool Locked = false;
        List<(List<Item>, Dictionary<Stats, decimal>)> combinations = new List<(List<Item>, Dictionary<Stats, decimal>)>();
        int optimizePage = 1;
        int sortColumn = -1;
        public Main()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Util.ResizeImage(Properties.Resources.bookmark, 19,18).GetHicon());

            //Read list of heroes from epicsevendb.com
            try
            {
                string json = Util.client.DownloadString(Util.ApiUrl + "/hero/");
                JToken info = JObject.Parse(json)["results"];
                int length = info.Count();
                for (int i = 0; i < length; i++)
                {
                    cb_Hero.Items.Add((string)info[i]["name"]);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show("Could not connect to epicsevendb.com. Please check your internet connection.");
            }

            //load assets
            Sets.Images.Add(Properties.Resources.set_speed);
            Sets.Images.Add(Properties.Resources.set_hit);
            Sets.Images.Add(Properties.Resources.set_crit);
            Sets.Images.Add(Properties.Resources.set_defense);
            Sets.Images.Add(Properties.Resources.set_health);
            Sets.Images.Add(Properties.Resources.set_attack);
            Sets.Images.Add(Properties.Resources.set_counter);
            Sets.Images.Add(Properties.Resources.set_lifesteal);
            Sets.Images.Add(Properties.Resources.set_destruction);
            Sets.Images.Add(Properties.Resources.set_resist);
            Sets.Images.Add(Properties.Resources.set_rage);
            Sets.Images.Add(Properties.Resources.set_immunity);
            Sets.Images.Add(Properties.Resources.set_unity);
            Elements.Images.Add(Properties.Resources.fire);
            Elements.Images.Add(Properties.Resources.earth);
            Elements.Images.Add(Properties.Resources.ice);
            Elements.Images.Add(Properties.Resources.light);
            Elements.Images.Add(Properties.Resources.dark);
            Classes.Images.Add(Properties.Resources.knight);
            Classes.Images.Add(Properties.Resources.warrior);
            Classes.Images.Add(Properties.Resources.thief);
            Classes.Images.Add(Properties.Resources.mage);
            Classes.Images.Add(Properties.Resources.soulweaver);
            Classes.Images.Add(Properties.Resources.ranger);
            cb_NecklaceFocus.Items.Add("");
            cb_RingFocus.Items.Add("");
            cb_BootsFocus.Items.Add("");
            cb_Set1.Items.Add("");
            cb_Set2.Items.Add("");
            cb_Set3.Items.Add("");
            cb_Eq.Items.Add("");
            dgv_OptimizeResults.RowCount = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 15, FontStyle.Bold); 
            richTextBox1.SelectedText = "How to use: \n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10 ,FontStyle.Bold);
            richTextBox1.SelectedText = "Start a new collection by entering your Heroes and Items on the Heroes and Inenventory Tabs or import an existing one.\n";
            richTextBox1.SelectionBullet = false;
            richTextBox1.SelectionIndent = 8;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
            richTextBox1.SelectedText = "You can import your collection from /u/HyrTheWinter's web optimizer if you used it.\n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox1.SelectedText = "Edit your items on the Inventory tab by selecting one from the list or adding a new one.\n";
            richTextBox1.SelectionBullet = false;
            richTextBox1.SelectionIndent = 8;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
            richTextBox1.SelectedText = "Choose the properties of the item with the controls below and click on the edit button to overwrite the selected item.\n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox1.SelectedText = "Edit your heroes on the Heroes tab by selecting one from the list or adding a new one.\n";
            richTextBox1.SelectionBullet = false;
            richTextBox1.SelectionIndent = 8;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
            richTextBox1.SelectedText = "Choose the properties of the hero with the controls below and click on the edit button to overwrite the selected hero. You can also equip your heroes with the items from your inventory.\n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox1.SelectedText = "Go to the Optimize tab and find the combination of items which meets your requirements.\n";
            richTextBox1.SelectionBullet = false;
            richTextBox1.SelectionIndent = 8;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
            richTextBox1.SelectedText = "Select the hero to optimize. Enter your stat requirements. Select which main stat to focus on for the right side of your equipment. It is recommended to keep the estimated results < 5,000,000 to not use too much RAM. 5,000,000 results equal about 3.5 GB RAM usage.\n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox1.SelectedText = "Export your Collection! It is NOT saved when closing the Program.";
            lb_Main.SelectedIndex = 0;
            lb_Sub1.SelectedIndex = 0;
            lb_Sub2.SelectedIndex = 0;
            lb_Sub3.SelectedIndex = 0;
            lb_Sub4.SelectedIndex = 0;
        }

        
        private void B_import_Click(object sender, EventArgs e)
        {
            if (rb_import_this.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    Import importForm = new Import(data, ofd_import.FileName, false);
                    importForm.ShowDialog();
                    if (!importForm.result)
                    {
                        MessageBox.Show("Corrupted or wrong file format. Please select a JSON file exported by this application!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        updateHeroList();
                    }
                }
            }
            else if (rb_import_web.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    Import importForm = new Import(data, ofd_import.FileName, true);
                    importForm.ShowDialog();
                    if (!importForm.result)
                    {
                        MessageBox.Show("Corrupted or wrong file format. Please select a JSON file exported by /u/HyrTheWinter's Equipment Optimizer!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        updateHeroList();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select the source to import from!");
            }
        }

        //Update the controls in the selected tab
        private void tc_Main_SelectedIndexChanged(object sender, EventArgs e)
        { 
            if (((TabControl)(sender)).SelectedIndex == 1)
            {
                updateItemList();
            }
            else if (((TabControl)(sender)).SelectedIndex == 2)
            {
                updateHeroList();
            }
            else if (((TabControl)(sender)).SelectedIndex == 3)
            {
                cb_OptimizeHero.Items.Clear();
                foreach (Hero hero in data.Heroes)
                {
                    cb_OptimizeHero.Items.Add(hero.Name + " " + hero.ID);
                }
            }
        }


        private void updateItemList()
        {
            //clear currently displayed items
            Point cell = dgv_Inventory.CurrentCellAddress;
            DataGridViewColumn sortColumn = dgv_Inventory.SortedColumn;
            SortOrder order = dgv_Inventory.SortOrder;
            dgv_Inventory.Rows.Clear();

            //calculate new list of items based on the selected type filter
            ItemType filter = tc_Inventory.SelectedIndex == 0 ? ItemType.All : (ItemType)(tc_Inventory.SelectedIndex - 1);
            var filteredList = (filter == ItemType.All) ? data.Items : data.Items.Where(x => x.Type == filter);
            foreach (Item item in filteredList)
            {
                object[] values = new object[dgv_Inventory.ColumnCount];
                values[0] = Sets.Images[(int)item.Set];
                values[19] = (int)item.Set;
                values[1] = Types.Images[(int)item.Type];
                values[20] = (int)item.Type;
                values[2] = item.Grade.ToString();
                values[3] = item.ILvl;
                values[4] = "+" + item.Enhance.ToString();
                values[5] = item.Main.Name.ToString().Replace("Percent","%");
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
                values[22] = item.Locked.ToString();
                dgv_Inventory.Rows.Add(values);
            }
            //restore previous sorting and select previously selected cell
            if (order != SortOrder.None) dgv_Inventory.Sort(sortColumn, (ListSortDirection)Enum.Parse(typeof(ListSortDirection), order.ToString()));
            if (cell.X > -1 && cell.Y > -1 && cell.X < dgv_Inventory.ColumnCount && cell.Y < dgv_Inventory.RowCount)
            {
                dgv_Inventory.CurrentCell = dgv_Inventory.Rows[cell.Y].Cells[cell.X];
            }
        }


        public void updateHeroList()
        {
            //clear currently displayed heroes
            Point cell = dgv_Heroes.CurrentCellAddress;
            DataGridViewColumn sortColumn = dgv_Heroes.SortedColumn;
            SortOrder order = dgv_Heroes.SortOrder;
            dgv_Heroes.Rows.Clear();

            //calculate new list of heroes based on the selected element/class filter
            Element elementFilter = tc_Heroes_Element.SelectedIndex == 0 ? Element.All : (Element)(tc_Heroes_Element.SelectedIndex - 1);
            HeroClass classFilter = tc_Heroes_Class.SelectedIndex == 0 ? HeroClass.All : (HeroClass)(tc_Heroes_Class.SelectedIndex - 1);
            var filteredList = (elementFilter == Element.All) ? data.Heroes : data.Heroes.Where(x => x.Element == elementFilter);
            filteredList = (classFilter == HeroClass.All) ? filteredList : filteredList.Where(x => x.Class == classFilter);
            foreach (Hero hero in filteredList)
            {
                object[] values = new object[dgv_Heroes.ColumnCount];
                values[dgv_Heroes.ColumnCount - 1] = hero.ID;
                values[0] = hero.PortraitSmall ?? Util.error;
                values[1] = hero.Name;
                values[2] = Elements.Images[(int)hero.Element];
                values[3] = Classes.Images[(int)hero.Class];
                values[4] = hero.Lvl;
                values[5] = hero.Stars ?? Util.error;
                List<Set> activeSets = hero.activeSets();
                if (activeSets.Count > 0)
                {
                    Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(sets);
                    for (int i = 0; i < activeSets.Count; i++)
                    {
                        g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                    }
                    values[6] = sets;
                }
                else
                {
                    values[6] = null;
                }
                int count = 0;
                if (activeSets.Contains(Set.Unity))
                {
                    foreach(Set set in activeSets)
                    {
                        count += set == Set.Unity ? 1 : 0;
                    }
                }
                values[15] = (5 + (count * 4)) + "%";
                Dictionary<Stats, decimal> stats = hero.CurrentStats;
                values[7] = (int)stats[Stats.ATK];
                values[8] = (int)stats[Stats.SPD];
                values[9] = stats[Stats.Crit].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[10] = stats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[11] = (int)stats[Stats.HP];
                values[12] = (int)stats[Stats.DEF];
                values[13] = stats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[14] = stats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[16] = (int)stats[Stats.EHP];
                values[17] = (int)stats[Stats.DMG];
                dgv_Heroes.Rows.Add(values);
            }

            //restore previous sorting and select previously selected cell
            if (order != SortOrder.None) dgv_Heroes.Sort(sortColumn, (ListSortDirection)Enum.Parse(typeof(ListSortDirection), order.ToString()));
            if (cell.X > -1 && cell.Y > -1 && cell.X < dgv_Heroes.ColumnCount && cell.Y < dgv_Heroes.RowCount)
            {
                dgv_Heroes.CurrentCell = dgv_Heroes.Rows[cell.Y].Cells[cell.X];
            }
            cb_Eq.Items.Clear();
            foreach (Hero hero in data.Heroes)
            {
                cb_Eq.Items.Add(hero.Name + " " + hero.ID);
            }
            cb_Eq.Items.Add("");
        }

        private void tc_Inventory_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateItemList();
        }

        private void dgv_Inventory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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

        //Change the controls on the inventory tab to reflect the data of the currenty selected item
        private void dgv_Inventory_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = dgv_Inventory.Rows[e.RowIndex];
            ((RadioButton)p_Set.Controls.Find("rb_" + ((Set)row.Cells[19].Value).ToString() + "Set", false)[0]).Checked = true;
            ((RadioButton)p_Type.Controls.Find("rb_" + ((ItemType)row.Cells[20].Value).ToString() + "Type", false)[0]).Checked = true;
            ((RadioButton)p_Grade.Controls.Find("rb_" + row.Cells[2].Value + "Grade", false)[0]).Checked = true;
            nud_ILvl.Value = (int)row.Cells[3].Value;
            nud_Enhance.Value = (int)decimal.Parse(((string)row.Cells[4].Value).Substring(1));
            lb_Main.SelectedIndex = lb_Main.FindStringExact((string)row.Cells[5].Value);
            nud_Main.Value = (int)decimal.Parse(((string)row.Cells[6].Value).Replace("%",""));
            int subStat = 0;
            for (int i = 7; i < 18; i++)
            {
                if ((string)row.Cells[i].Value != "")
                {
                    ListBox lb = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + (subStat + 1) , false)[0];
                    lb.SelectedIndex = lb.FindStringExact(dgv_Inventory.Columns[i].HeaderText);
                    ((NumericUpDown)tb_Inventory.Controls.Find("nud_Sub" + (subStat+1), false)[0]).Value = (int)decimal.Parse(((string)row.Cells[i].Value).Replace("%", ""));
                    subStat++;
                }
            }
            for (;subStat < 4; subStat++)
            {
                ((ListBox)tb_Inventory.Controls.Find("lb_Sub" + (subStat+1), false)[0]).SelectedIndex = 11;
                ((NumericUpDown)tb_Inventory.Controls.Find("nud_Sub" + (subStat + 1), false)[0]).Text = "";
            }
            if (row.Cells["c_Locked"].Value.ToString() == bool.TrueString)
            {
                pb_ItemLocked.Image = Properties.Resources.locked;
                Locked = true;
            }
            else
            {
                pb_ItemLocked.Image = Properties.Resources.unlocked;
                Locked = false;
            }
            string ID = (string)dgv_Inventory.Rows[e.RowIndex].Cells[21].Value;
            Item item = data.Items.Find(x => x.ID == ID);
            if (item.Equipped != null)
            {
                cb_Eq.SelectedIndex = cb_Eq.FindStringExact(item.Equipped.Name + " " + item.Equipped.ID);
                pb_Equipped.Image = item.Equipped.Portrait;
            }
            else
            {
                cb_Eq.SelectedIndex = 0;
                pb_Equipped.Image = null;
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_WeaponType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 2; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.ATK ? Stats.ATK.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.ATK;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 2; i++)
                    {
                        switch ((Stats)i)
                        {
                            case Stats.ATK:
                                current.Items[i] = "";
                                break;
                            case Stats.DEFPercent:
                                current.Items[i] = "";
                                break;
                            case Stats.DEF:
                                current.Items[i] = "";
                                break;
                            default:
                                current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                                break;
                        }
                    }
                }
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_HelmetType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 2; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.HP ? Stats.HP.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.HP;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 2; i++)
                    {
                        switch ((Stats)i)
                        {
                            case Stats.HP:
                                current.Items[i] = "";
                                break;
                            default:
                                current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                                break;
                        }
                    }
                }
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_ArmorType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.DEF ? Stats.DEF.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.DEF;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                    {
                        switch ((Stats)i)
                        {
                            case Stats.ATKPercent:
                                current.Items[i] = "";
                                break;
                            case Stats.ATK:
                                current.Items[i] = "";
                                break;
                            case Stats.DEF:
                                current.Items[i] = "";
                                break;
                            default:
                                current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                                break;
                        }
                    }
                }
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_NecklaceType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                {
                    switch ((Stats)i)
                    {
                        case Stats.SPD:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.EFF:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.RES:
                            lb_Main.Items[i] = "";
                            break;
                        default:
                            lb_Main.Items[i] = ((Stats)i).ToString().Replace("Percent","%");
                            break;
                    }
                }
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                    {
                        current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                    }
                }
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_RingType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                {
                    switch ((Stats)i)
                    {
                        case Stats.SPD:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.Crit:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.CritDmg:
                            lb_Main.Items[i] = "";
                            break;
                        default:
                            lb_Main.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                            break;
                    }
                }
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                    {
                        current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                    }
                }
            }
        }

        //Change selectable Mainstats based on the item type. Eg. weapons alwayys have ATK flat Mainstat
        private void Rb_BootsType_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                {
                    switch ((Stats)i)
                    {
                        case Stats.Crit:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.CritDmg:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.EFF:
                            lb_Main.Items[i] = "";
                            break;
                        case Stats.RES:
                            lb_Main.Items[i] = "";
                            break;
                        default:
                            lb_Main.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                            break;
                    }
                }
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-2; i++)
                    {
                        current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                    }
                }
            }
        }

        //Create a new item with the selected stats without equipping it to a hero
        private void B_NewItem_Click(object sender, EventArgs e)
        {
            Set set = (Set)Enum.Parse(typeof(Set), p_Set.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Set", ""));
            ItemType type = (ItemType)Enum.Parse(typeof(ItemType), p_Type.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Type", ""));
            Grade grade = (Grade)Enum.Parse(typeof(Grade), p_Grade.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Grade", ""));

            Stat main;
            if (lb_Main.SelectedItem.ToString() != "")
            {
                main = new Stat((Stats)Enum.Parse(typeof(Stats), lb_Main.SelectedItem.ToString().Replace("%", "Percent")), (decimal)nud_Main.Value);
            }
            else
            {
                MessageBox.Show("Invalid Mainstat");
                return;
            }

            List<Stat> substats = new List<Stat>();
            string selection = lb_Sub1.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub1.Value));
            selection = lb_Sub2.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub2.Value));
            selection = lb_Sub3.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub3.Value));
            selection = lb_Sub4.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub4.Value));

            int ilvl = (int)nud_ILvl.Value;
            int enh = (int)nud_Enhance.Value;
            bool locked = false;
            if (Locked)
            {
                locked = true;
            }

            Item newItem = new Item(data.incrementItemID(), type, set, grade, ilvl, enh, main, substats.ToArray(), null, locked);
            data.Items.Add(newItem);
            updateItemList();
            //select the created item if it is displayed with the current filter
            if (tc_Inventory.SelectedIndex == 0 || (ItemType)(tc_Inventory.SelectedIndex - 1) == type)
            {
                dgv_Inventory.CurrentCell = dgv_Inventory.Rows.Cast<DataGridViewRow>().Where(x => x.Cells["c_ItemID"].Value.ToString() == newItem.ID).First().Cells[0];
            }
        }

        //Create a new item with the selected stats equipped on the selected hero
        private void B_NewItemEquipped_Click(object sender, EventArgs e)
        {
            Set set = (Set)Enum.Parse(typeof(Set), p_Set.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Set", ""));
            ItemType type = (ItemType)Enum.Parse(typeof(ItemType), p_Type.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Type", ""));
            Grade grade = (Grade)Enum.Parse(typeof(Grade), p_Grade.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Grade", ""));

            Stat main;
            if (lb_Main.SelectedItem.ToString() != "")
            {
                main = new Stat((Stats)Enum.Parse(typeof(Stats), lb_Main.SelectedItem.ToString().Replace("%", "Percent")), (decimal)nud_Main.Value);
            }
            else
            {
                MessageBox.Show("Invalid Mainstat");
                return;
            }

            List<Stat> substats = new List<Stat>();
            string selection = lb_Sub1.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub1.Value));
            selection = lb_Sub2.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub2.Value));
            selection = lb_Sub3.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub3.Value));
            selection = lb_Sub4.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub4.Value));

            int ilvl = (int)nud_ILvl.Value;
            int enh = (int)nud_Enhance.Value;
            Hero hero = null;
            if (cb_Eq.Text != "")
            {
                hero = data.Heroes.Find(x => x.Name == String.Join(" ", cb_Eq.Text.Split(' ').Reverse().Skip(1).Reverse()));
            }
            bool locked = false;
            if (Locked)
            {
                locked = true;
            }

            Item newItem = new Item(data.incrementItemID(), type, set, grade, ilvl, enh, main, substats.ToArray(), hero, locked);
            if (hero != null)
            {
                hero.equip(newItem);
            }
            data.Items.Add(newItem);
            updateItemList();
            //select the created item if it is displayed with the current filter
            if (tc_Inventory.SelectedIndex == 0 || (ItemType)(tc_Inventory.SelectedIndex - 1) == type)
            {
                dgv_Inventory.CurrentCell = dgv_Inventory.Rows.Cast<DataGridViewRow>().Where(x => x.Cells["c_ItemID"].Value.ToString() == newItem.ID).First().Cells[0];
            }
        }

        //Overwrite the currently selected item with the selected stats
        private void B_EditItem_Click(object sender, EventArgs e)
        {
            if (dgv_Inventory.SelectedRows.Count > 0)
            {
                string ID = (string)dgv_Inventory.SelectedRows[0].Cells[21].Value;
                Item item = data.Items.Find(x => x.ID == ID);
                item.Set = (Set)Enum.Parse(typeof(Set), p_Set.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Set", ""));
                item.Type = (ItemType)Enum.Parse(typeof(ItemType), p_Type.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Type", ""));
                item.Grade = (Grade)Enum.Parse(typeof(Grade), p_Grade.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Grade", ""));

                if (lb_Main.SelectedItem.ToString() != "")
                {
                    item.Main = new Stat((Stats)Enum.Parse(typeof(Stats), lb_Main.SelectedItem.ToString().Replace("%", "Percent")), (decimal)nud_Main.Value);
                }
                else
                {
                    MessageBox.Show("Invalid Mainstat");
                    return;
                }

                List<Stat> substats = new List<Stat>();
                string selection = lb_Sub1.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub1.Value));
                selection = lb_Sub2.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub2.Value));
                selection = lb_Sub3.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub3.Value));
                selection = lb_Sub4.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (decimal)nud_Sub4.Value));
                item.SubStats = substats.ToArray();

                item.ILvl = (int)nud_ILvl.Value;
                item.Enhance = (int)nud_Enhance.Value;
                if (Locked)
                {
                    item.Locked = true;
                }
                else
                {
                    item.Locked = false;
                }

                Hero newHero = data.Heroes.Find(x => x.Name == String.Join(" ", cb_Eq.Text.Split(' ').Reverse().Skip(1).Reverse()));
                if (newHero != item.Equipped)
                {
                    item.Equipped.unequip(item);
                    if (newHero != null)
                    {
                        newHero.equip(item);
                    }
                }
                else if (newHero != null)
                {
                    newHero.calcStats();
                }
                updateItemList();
            }
        }

        //Delete the currently selected item and unequip it
        private void B_RemoveItem_Click(object sender, EventArgs e)
        {
            if (dgv_Inventory.SelectedRows.Count > 0)
            {
                string ID = (string)dgv_Inventory.SelectedRows[0].Cells[21].Value;
                Item item = data.Items.Find(x => x.ID == ID);
                Hero hero = item.Equipped;
                if (hero != null)
                {
                    hero.unequip(item);
                }
                data.Items.Remove(item);
                updateItemList();
            }
        }

        private void Tc_Heroes_Element_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateHeroList();
        }

        private void Tc_Heroes_Class_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateHeroList();
        }

        //Overwrite the currently selected hero with the selected stats
        private void B_EditHero_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            if (cb_Hero.Text != "" && cb_HeroLvl.Text != "" && cb_HeroAwakening.Text != "")
            {
                //create new hero object and equip gear if base hero changed
                if (cb_Hero.Text != hero.Name)
                {
                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (decimal)nud_ArtifactAttack.Value), new Stat(Stats.HP, (decimal)nud_ArtifactHealth.Value) }, null, false);
                    Hero newHero = new Hero(data.incrementHeroID(), cb_Hero.Text, new List<Item>(), artifact, int.Parse(cb_HeroLvl.Text), int.Parse(cb_HeroAwakening.Text));
                    List<Item> eq = hero.getGear();
                    hero.unequipAll();
                    newHero.equip(eq);
                    data.Heroes.Remove(hero);
                    data.Heroes.Add(newHero);
                }
                else
                {
                    hero.Lvl = int.Parse(cb_HeroLvl.Text);
                    hero.Awakening = int.Parse(cb_HeroAwakening.Text);
                    if (hero.Awakening > hero.Lvl / 10)
                    {
                        hero.Awakening = hero.Lvl / 10;
                    }

                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (decimal)nud_ArtifactAttack.Value), new Stat(Stats.HP, (decimal)nud_ArtifactHealth.Value) }, null, false);
                    hero.Artifact = artifact;
                    hero.calcAwakeningStats();
                    hero.calcStats();
                }
                updateHeroList();
            }
        }

        //Change the controls on the hero tab to reflect the data of the currenty selected hero
        private void Dgv_Heroes_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = dgv_Heroes.Rows[e.RowIndex];
            Hero hero = data.Heroes.Find(x => x.ID == (string)row.Cells["c_HeroID"].Value);
            cb_Hero.SelectedIndex = cb_Hero.FindStringExact(hero.Name);
            cb_HeroLvl.SelectedIndex = hero.Lvl == 50 ? 0 : 1;
            cb_HeroAwakening.SelectedIndex = hero.Awakening - 1;
            pb_Hero.Image = hero.Portrait ?? Util.error;
            nud_ArtifactAttack.Value = hero.Artifact != null ? (int)hero.Artifact.SubStats[0].Value : 0;
            nud_ArtifactHealth.Value = hero.Artifact != null ? (int)hero.Artifact.SubStats[1].Value : 0;

            //Check whether the selected Hero has an item equipped in the slot and set the controls for the slot accordingly
            Item item = hero.getItem(ItemType.Weapon);
            if (item != null)
            {
                l_WeaponGrade.Text = item.Grade.ToString() + " Weapon";
                l_WeaponGrade.ForeColor = Util.gradeColors[item.Grade];
                l_WeaponIlvl.Text = item.ILvl.ToString();
                l_WeaponEnhance.Text = "+" + item.Enhance.ToString();
                l_WeaponMain.Text = Util.statStrings[item.Main.Name];
                l_WeaponMainStat.Text = ((int)item.Main.Value).ToString();
                l_WeaponSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_WeaponSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_WeaponGrade.Text = "";
                l_WeaponIlvl.Text = "";
                l_WeaponEnhance.Text = "";
                l_WeaponMain.Text = "";
                l_WeaponMainStat.Text = "";
                l_WeaponSet.Text = "";
                pb_WeaponSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_WeaponSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_WeaponSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
            item = hero.getItem(ItemType.Helmet);
            if (item != null)
            {
                l_HelmetGrade.Text = item.Grade.ToString() + " Helmet";
                l_HelmetGrade.ForeColor = Util.gradeColors[item.Grade];
                l_HelmetIlvl.Text = item.ILvl.ToString();
                l_HelmetEnhance.Text = "+" + item.Enhance.ToString();
                l_HelmetMain.Text = Util.statStrings[item.Main.Name];
                l_HelmetMainStat.Text = ((int)item.Main.Value).ToString();
                l_HelmetSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_HelmetSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_HelmetGrade.Text = "";
                l_HelmetIlvl.Text = "";
                l_HelmetEnhance.Text = "";
                l_HelmetMain.Text = "";
                l_HelmetMainStat.Text = "";
                l_HelmetSet.Text = "";
                pb_HelmetSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_HelmetSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_HelmetSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
            item = hero.getItem(ItemType.Armor);
            if (item != null)
            {
                l_ArmorGrade.Text = item.Grade.ToString() + " Armor";
                l_ArmorGrade.ForeColor = Util.gradeColors[item.Grade];
                l_ArmorIlvl.Text = item.ILvl.ToString();
                l_ArmorEnhance.Text = "+" + item.Enhance.ToString();
                l_ArmorMain.Text = Util.statStrings[item.Main.Name];
                l_ArmorMainStat.Text = ((int)item.Main.Value).ToString();
                l_ArmorSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_ArmorSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_ArmorGrade.Text = "";
                l_ArmorIlvl.Text = "";
                l_ArmorEnhance.Text = "";
                l_ArmorMain.Text = "";
                l_ArmorMainStat.Text = "";
                l_ArmorSet.Text = "";
                pb_ArmorSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_ArmorSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_ArmorSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
            item = hero.getItem(ItemType.Necklace);
            if (item != null)
            {
                l_NecklaceGrade.Text = item.Grade.ToString() + " Necklace";
                l_NecklaceGrade.ForeColor = Util.gradeColors[item.Grade];
                l_NecklaceIlvl.Text = item.ILvl.ToString();
                l_NecklaceEnhance.Text = "+" + item.Enhance.ToString();
                l_NecklaceMain.Text = Util.statStrings[item.Main.Name];
                l_NecklaceMainStat.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                l_NecklaceSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_NecklaceSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_NecklaceGrade.Text = "";
                l_NecklaceIlvl.Text = "";
                l_NecklaceEnhance.Text = "";
                l_NecklaceMain.Text = "";
                l_NecklaceMainStat.Text = "";
                l_NecklaceSet.Text = "";
                pb_NecklaceSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_NecklaceSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_NecklaceSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
            item = hero.getItem(ItemType.Ring);
            if (item != null)
            {
                l_RingGrade.Text = item.Grade.ToString() + " Ring";
                l_RingGrade.ForeColor = Util.gradeColors[item.Grade];
                l_RingIlvl.Text = item.ILvl.ToString();
                l_RingEnhance.Text = "+" + item.Enhance.ToString();
                l_RingMain.Text = Util.statStrings[item.Main.Name];
                l_RingMainStat.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                l_RingSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_RingSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_RingGrade.Text = "";
                l_RingIlvl.Text = "";
                l_RingEnhance.Text = "";
                l_RingMain.Text = "";
                l_RingMainStat.Text = "";
                l_RingSet.Text = "";
                pb_RingSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_RingSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_RingSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
            item = hero.getItem(ItemType.Boots);
            if (item != null)
            {
                l_BootsGrade.Text = item.Grade.ToString() + " Boots";
                l_BootsGrade.ForeColor = Util.gradeColors[item.Grade];
                l_BootsIlvl.Text = item.ILvl.ToString();
                l_BootsEnhance.Text = "+" + item.Enhance.ToString();
                l_BootsMain.Text = Util.statStrings[item.Main.Name];
                l_BootsMainStat.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                l_BootsSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                pb_BootsSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                for (int i = 0; i < 4; i++)
                {
                    if (i < item.SubStats.Length)
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                    }
                    else
                    {
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1), true)[0]).Text = "";
                        ((Label)tb_Heroes.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
            else
            {
                l_BootsGrade.Text = "";
                l_BootsIlvl.Text = "";
                l_BootsEnhance.Text = "";
                l_BootsMain.Text = "";
                l_BootsMainStat.Text = "";
                l_BootsSet.Text = "";
                pb_BootsSet.Image = Util.error;
                for (int i = 0; i < 4; i++)
                {
                    ((Label)tb_Heroes.Controls.Find("l_BootsSub" + (i + 1), true)[0]).Text = "";
                    ((Label)tb_Heroes.Controls.Find("l_BootsSub" + (i + 1) + "Stat", true)[0]).Text = "";
                }
            }
        }

        //Create a new Hero and add it to the list
        private void B_AddHero_Click(object sender, EventArgs e)
        {
            if (cb_Hero.Text != "" && cb_HeroLvl.Text != "" && cb_HeroAwakening.Text != "")
            {
                //Artifacts only consist of an ATK and a HP stat for the purpose of hero stat calculation. So the ID, type, set etc. is irrelevant
                Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (decimal)nud_ArtifactAttack.Value), new Stat(Stats.HP, (decimal)nud_ArtifactHealth.Value) }, null, false);
                Hero newHero = new Hero(data.incrementHeroID(), cb_Hero.Text, new List<Item>(), artifact, int.Parse(cb_HeroLvl.Text), int.Parse(cb_HeroAwakening.Text));
                data.Heroes.Add(newHero);
                updateHeroList();
                dgv_Heroes.CurrentCell = dgv_Heroes.Rows[dgv_Heroes.Rows.Count - 1].Cells[0];
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_WeaponEdit_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Weapon);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipWeapon_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Weapon).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    hero.unequip(hero.getItem(ItemType.Weapon));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Weapon);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Delete the selected Hero and unequip his/her gear
        private void B_RemoveHero_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            hero.unequipAll();
            data.Heroes.Remove(hero);
            updateHeroList();
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipHelmet_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Helmet).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    hero.unequip(hero.getItem(ItemType.Helmet));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Helmet);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditHelmet_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Helmet);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipArmor_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Armor).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    hero.unequip(hero.getItem(ItemType.Armor));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Armor);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipNecklace_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Necklace).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {

                    hero.unequip(hero.getItem(ItemType.Necklace));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Armor);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipRing_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Ring).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    hero.unequip(hero.getItem(ItemType.Ring));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Ring);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipBoots_Click(object sender, EventArgs e)
        {
            List<Item> list = data.Items.Where(x => x.Type == ItemType.Boots).Where(x => x.Equipped == null).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    hero.unequip(hero.getItem(ItemType.Boots));
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(ItemType.Boots);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditArmor_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Armor);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditNecklace_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Necklace);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditRing_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Ring);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditBoots_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(ItemType.Boots);
            if (item != null)
            {
                tc_Main.SelectedIndex = 1;
                tc_Inventory.SelectedIndex = 0;
                foreach (DataGridViewRow row in dgv_Inventory.Rows)
                {
                    if ((string)row.Cells["c_ItemID"].Value == item.ID)
                    {
                        dgv_Inventory.CurrentCell = row.Cells["c_set"];
                        break;
                    }
                }
            }
        }


        private void Pb_ItemLocked_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                pb_ItemLocked.Image = Properties.Resources.unlocked;
                Locked = false;
            }
            else
            {
                pb_ItemLocked.Image = Properties.Resources.locked;
                Locked = true;
            }
        }

        //Generate JSON String with the current items and heroes
        

        //Calculate and display the current stats of the selected hero
        private void Cb_OptimizeHero_SelectedIndexChanged(object sender, EventArgs e)
        {
            dgv_CurrentGear.Rows.Clear();
            object[] values = new object[dgv_CurrentGear.ColumnCount];
            if (cb_OptimizeHero.Items[cb_OptimizeHero.SelectedIndex].ToString() != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Items[cb_OptimizeHero.SelectedIndex].ToString().Split().Last());
                values[0] = (int)hero.CurrentStats[Stats.ATK];
                values[1] = (int)hero.CurrentStats[Stats.SPD];
                decimal crit = hero.CurrentStats[Stats.Crit] + (nud_CritBonus.Value / 100m);
                crit = crit > 1 ? 1 : crit;
                values[2] = crit.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[3] = hero.CurrentStats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[4] = (int)hero.CurrentStats[Stats.HP];
                values[5] = (int)hero.CurrentStats[Stats.DEF];
                values[6] = hero.CurrentStats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[7] = hero.CurrentStats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                List<Set> activeSets = hero.activeSets();
                int count = 0;
                if (activeSets.Contains(Set.Unity))
                {
                    foreach (Set set in activeSets)
                    {
                        count += set == Set.Unity ? 1 : 0;
                    }
                }
                values[8] = (5 + (count * 4)) + "%";
                if (activeSets.Count > 0)
                {
                    Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(sets);
                    for (int i = 0; i < activeSets.Count; i++)
                    {
                        g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                    }
                    values[9] = sets;
                }
                else
                {
                    values[9] = null;
                }
                values[10] = (int)hero.CurrentStats[Stats.EHP];
                values[11] = (int)((hero.CurrentStats[Stats.ATK] * (1 - crit)) + (hero.CurrentStats[Stats.ATK] * crit * hero.CurrentStats[Stats.CritDmg]));
                l_Results.Text = numberOfResults().ToString();
            }
            else
            {
                for(int i = 0; i < values.Length; i++)
                {
                    values[i] = null;
                }
                l_Results.Text = "0";
            }
            dgv_CurrentGear.Rows.Add(values);
        }

        //Estimate the number of results with the currenty selected criteria. This method only takes account of focus options for right side gear 
        //and whether locked/equipped items are included because set and stat filters can only be applied after a combination of items has been calculated.
        private long numberOfResults()
        {
            List<Item> Base = data.Items;
            if (!chb_Equipped.Checked)
            {
                Base = Base.Where(x => x.Equipped == null).ToList();
            }
            if (!chb_Locked.Checked)
            {
                Base = Base.Where(x => !x.Locked).ToList();
            }
            string neckFocus = cb_NecklaceFocus.SelectedIndex > -1 ? cb_NecklaceFocus.Items[cb_NecklaceFocus.SelectedIndex].ToString() : "";
            long necklaces;
            if (neckFocus != "")
            {
                Stats stat = (Stats)Enum.Parse(typeof(Stats), neckFocus.Replace("%", "Percent"));
                necklaces = Base.Where(x => x.Type == ItemType.Necklace).Where(x => x.Main.Name == stat).Count();
            }
            else
            {
                necklaces = Base.Where(x => x.Type == ItemType.Necklace).Count();
            }
            string ringFocus = cb_RingFocus.SelectedIndex> -1 ? cb_RingFocus.Items[cb_RingFocus.SelectedIndex].ToString(): "";
            long rings;
            if (ringFocus != "")
            {
                Stats stat = (Stats)Enum.Parse(typeof(Stats), ringFocus.Replace("%", "Percent"));
                rings = Base.Where(x => x.Type == ItemType.Ring).Where(x => x.Main.Name == stat).Count();
            }
            else
            {
                rings = Base.Where(x => x.Type == ItemType.Ring).Count();
            }
            string bootsFocus = cb_BootsFocus.SelectedIndex > -1 ? cb_BootsFocus.Items[cb_BootsFocus.SelectedIndex].ToString() : "";
            long boots;
            if (bootsFocus != "")
            {
                Stats stat = (Stats)Enum.Parse(typeof(Stats), bootsFocus.Replace("%", "Percent"));
                boots = Base.Where(x => x.Type == ItemType.Boots).Where(x => x.Main.Name == stat).Count();
            }
            else
            {
                boots = Base.Where(x => x.Type == ItemType.Boots).Count();
            }
            long weapons = Base.Where(x => x.Type == ItemType.Weapon).Count();
            long helmets = Base.Where(x => x.Type == ItemType.Helmet).Count();
            long armors = Base.Where(x => x.Type == ItemType.Armor).Count();
            return weapons * helmets * armors * necklaces * rings * boots;
        }

        private void Chb_Locked_CheckedChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString();
        }

        private void Chb_Equipped_CheckedChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString();
        }

        private void Cb_NecklaceFocus_SelectedIndexChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString();
        }

        private void Cb_RingFocus_SelectedIndexChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString();
        }

        private void Cb_BootsFocus_SelectedIndexChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString();
        }

        //Filter the items based on the selected focus options and whether equipped/locked gear is used. Then executes an asynchronous call to the calculate method
        //with the selected stat and set filters.
        private async void B_Optimize_Click(object sender, EventArgs e)
        {
            dgv_OptimizeResults.RowCount = 0;
            dgv_OptimizeResults.Rows.Clear();
            combinations.Clear();
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
            if (hero != null)
            {
                List<Item> weapons = data.Items.Where(x => x.Type == ItemType.Weapon).ToList();
                List<Item> helmets = data.Items.Where(x => x.Type == ItemType.Helmet).ToList();
                List<Item> armors = data.Items.Where(x => x.Type == ItemType.Armor).ToList();
                List<Item> necklaces;
                List<Item> rings;
                List<Item> boots;
                string neckFocus = cb_NecklaceFocus.SelectedIndex > -1 ? cb_NecklaceFocus.Items[cb_NecklaceFocus.SelectedIndex].ToString() : "";
                if (neckFocus != "")
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), neckFocus.Replace("%", "Percent"));
                    necklaces = data.Items.Where(x => x.Type == ItemType.Necklace).Where(x => x.Main.Name == stat).ToList();
                }
                else
                {
                    necklaces = data.Items.Where(x => x.Type == ItemType.Necklace).ToList();
                }
                string ringFocus = cb_RingFocus.SelectedIndex > -1 ? cb_RingFocus.Items[cb_RingFocus.SelectedIndex].ToString() : "";
                if (ringFocus != "")
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), ringFocus.Replace("%", "Percent"));
                    rings = data.Items.Where(x => x.Type == ItemType.Ring).Where(x => x.Main.Name == stat).ToList();
                }
                else
                {
                    rings = data.Items.Where(x => x.Type == ItemType.Ring).ToList();
                }
                string bootsFocus = cb_BootsFocus.SelectedIndex > -1 ? cb_BootsFocus.Items[cb_BootsFocus.SelectedIndex].ToString() : "";
                if (bootsFocus != "")
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), bootsFocus.Replace("%", "Percent"));
                    boots = data.Items.Where(x => x.Type == ItemType.Boots).Where(x => x.Main.Name == stat).ToList();
                }
                else
                {
                    boots = data.Items.Where(x => x.Type == ItemType.Boots).ToList();
                }
                if (!chb_Equipped.Checked)
                {
                    weapons = weapons.Where(x => x.Equipped == null).ToList();
                    helmets = helmets.Where(x => x.Equipped == null).ToList();
                    armors = armors.Where(x => x.Equipped == null).ToList();
                    necklaces = necklaces.Where(x => x.Equipped == null).ToList();
                    rings = rings.Where(x => x.Equipped == null).ToList();
                    boots = boots.Where(x => x.Equipped == null).ToList();
                }
                if (!chb_Locked.Checked)
                {
                    weapons = weapons.Where(x => !x.Locked).ToList();
                    helmets = helmets.Where(x => !x.Locked).ToList();
                    armors = armors.Where(x => !x.Locked).ToList();
                    necklaces = necklaces.Where(x => !x.Locked).ToList();
                    rings = rings.Where(x => !x.Locked).ToList();
                    boots = boots.Where(x => !x.Locked).ToList();
                }

                List<Set> setFocus = new List<Set>();
                if (cb_Set1.SelectedIndex != -1 && cb_Set1.Items[cb_Set1.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set1.Items[cb_Set1.SelectedIndex].ToString()));
                if (cb_Set2.SelectedIndex != -1 && cb_Set2.Items[cb_Set2.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set2.Items[cb_Set2.SelectedIndex].ToString()));
                if (cb_Set3.SelectedIndex != -1 && cb_Set3.Items[cb_Set3.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set3.Items[cb_Set3.SelectedIndex].ToString()));
                (decimal, decimal)[] filter = new (decimal, decimal)[10];
                filter[0] = (tb_MinAttack.Text != "" ? decimal.Parse(tb_MinAttack.Text) : 0, tb_MaxAttack.Text != "" ? decimal.Parse(tb_MaxAttack.Text) : decimal.MaxValue);
                filter[1] = (tb_MinSpeed.Text != "" ? decimal.Parse(tb_MinSpeed.Text) : 0, tb_MaxSpeed.Text != "" ? decimal.Parse(tb_MaxSpeed.Text) : decimal.MaxValue);
                filter[2] = (tb_MinCrit.Text != "" ? decimal.Parse(tb_MinCrit.Text) / 100 : 0, tb_MaxCrit.Text != "" ? decimal.Parse(tb_MaxCrit.Text) / 100 : decimal.MaxValue);
                filter[3] = (tb_MinCritDmg.Text != "" ? decimal.Parse(tb_MinCritDmg.Text) / 100 : 0, tb_MaxCritDmg.Text != "" ? decimal.Parse(tb_MaxCritDmg.Text) / 100 : decimal.MaxValue);
                filter[4] = (tb_MinHealth.Text != "" ? decimal.Parse(tb_MinHealth.Text) : 0, tb_MaxHealth.Text != "" ? decimal.Parse(tb_MaxHealth.Text) : decimal.MaxValue);
                filter[5] = (tb_MinDefense.Text != "" ? decimal.Parse(tb_MinDefense.Text) : 0, tb_MaxDefense.Text != "" ? decimal.Parse(tb_MaxDefense.Text) : decimal.MaxValue);
                filter[6] = (tb_MinEff.Text != "" ? decimal.Parse(tb_MinEff.Text) / 100 : 0, tb_MaxEff.Text != "" ? decimal.Parse(tb_MaxEff.Text) / 100 : decimal.MaxValue);
                filter[7] = (tb_MinRes.Text != "" ? decimal.Parse(tb_MinRes.Text) / 100 : 0, tb_MaxRes.Text != "" ? decimal.Parse(tb_MaxRes.Text) / 100 : decimal.MaxValue);
                filter[8] = (tb_MinEhp.Text != "" ? decimal.Parse(tb_MinEhp.Text) : 0, tb_MaxEhp.Text != "" ? decimal.Parse(tb_MaxEhp.Text) : decimal.MaxValue);
                filter[9] = (tb_MinDmg.Text != "" ? decimal.Parse(tb_MinDmg.Text) : 0, tb_MaxDmg.Text != "" ? decimal.Parse(tb_MaxDmg.Text) : decimal.MaxValue);

                IProgress<int> progress = new Progress<int>(x => pB_Optimize.Value = x);
                pB_Optimize.Show();
                combinations = await Task.Run(() => calculate(weapons, helmets, armors, necklaces, rings, boots, hero, filter, setFocus, progress));
                pB_Optimize.Hide();
                pB_Optimize.Value = 0;
                //Display the first page of results. Each page consists of 100 results
                dgv_OptimizeResults.RowCount = 100;
                optimizePage = 1;
                l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);
            }
        }

        //Calculate all possible gear combinations and check whether they satisfy the given filters
        private List<(List<Item>, Dictionary<Stats, decimal>)> calculate(List<Item> weapons, List<Item> helmets,
                                                                        List<Item> armors, List<Item> necklaces,
                                                                        List<Item> rings, List<Item> boots, Hero hero,
                                                                        (decimal, decimal)[] filter, List<Set> setFocus,
                                                                        IProgress<int> progress)
        {
            List<(List<Item>, Dictionary<Stats, decimal>)> combinations = new List<(List<Item>, Dictionary<Stats, decimal>)>();
            decimal counter = 0;
            long numResults = weapons.Count * helmets.Count * armors.Count * necklaces.Count * rings.Count * boots.Count;
            foreach (Item w in weapons)
            {
                foreach (Item h in helmets)
                {
                    foreach (Item a in armors)
                    {
                        foreach (Item n in necklaces)
                        {
                            foreach (Item r in rings)
                            {
                                foreach (Item b in boots)
                                {
                                    List<Set> activeSets;
                                    Dictionary<Stats, decimal> stats = hero.calcStatsWithGear(new List<Item> { w, h, a, n, r, b }, out activeSets, nud_CritBonus.Value / 100m);
                                    bool valid = true;
                                    valid = valid && stats[Stats.ATK] >= filter[0].Item1 && stats[Stats.ATK] <= filter[0].Item2;
                                    valid = valid && stats[Stats.SPD] >= filter[1].Item1 && stats[Stats.SPD] <= filter[1].Item2;
                                    valid = valid && stats[Stats.Crit] >= filter[2].Item1 && stats[Stats.Crit] <= filter[2].Item2;
                                    valid = valid && stats[Stats.CritDmg] >= filter[3].Item1 && stats[Stats.CritDmg] <= filter[3].Item2;
                                    valid = valid && stats[Stats.HP] >= filter[4].Item1 && stats[Stats.HP] <= filter[4].Item2;
                                    valid = valid && stats[Stats.DEF] >= filter[5].Item1 && stats[Stats.DEF] <= filter[5].Item2;
                                    valid = valid && stats[Stats.EFF] >= filter[6].Item1 && stats[Stats.EFF] <= filter[6].Item2;
                                    valid = valid && stats[Stats.RES] >= filter[7].Item1 && stats[Stats.RES] <= filter[7].Item2;
                                    valid = valid && stats[Stats.EHP] >= filter[8].Item1 && stats[Stats.EHP] <= filter[8].Item2;
                                    valid = valid && stats[Stats.DMG] >= filter[9].Item1 && stats[Stats.DMG] <= filter[9].Item2;
                                    foreach (Set s in setFocus)
                                    {
                                        valid = valid && activeSets.Contains(s);
                                    }
                                    if (valid)
                                    {
                                        combinations.Add((new List<Item> { w, h, a, n, r, b }, stats));
                                    }
                                    counter += 1;
                                    progress.Report((int)(counter / numResults * 100));
                                }
                            }
                        }
                    }
                }
            }
            return combinations;
        }

        //Get the value for the current cell depending on which page of results is displayed
        private void Dgv_OptimizeResults_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (dgv_OptimizeResults.RowCount == 0) return;
            if (e.RowIndex >= combinations.Count - ((optimizePage - 1) * 100)) return;
            List<Set> activeSets;
            switch (e.ColumnIndex)
            {
                case 0:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage -1)].Item2[Stats.ATK];
                    break;
                case 1:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.SPD];
                    break;
                case 2:
                    e.Value = combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.Crit].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 3:
                    e.Value = combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 4:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.HP];
                    break;
                case 5:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.DEF];
                    break;
                case 6:
                    e.Value = combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 7:
                    e.Value = combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 8:
                    int count = 0;
                    activeSets = Util.activeSet(combinations[e.RowIndex + 100 * (optimizePage - 1)].Item1);
                    if (activeSets.Contains(Set.Unity))
                    {
                        foreach (Set set in activeSets)
                        {
                            count += set == Set.Unity ? 1 : 0;
                        }
                    }
                    e.Value = (5 + (count * 4)) + "%";
                    break;
                case 9:
                    activeSets = Util.activeSet(combinations[e.RowIndex + 100 * (optimizePage - 1)].Item1);
                    if (activeSets.Count > 0)
                    {
                        Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                        Graphics g = Graphics.FromImage(sets);
                        for (int i = 0; i < activeSets.Count; i++)
                        {
                            g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                        }
                        e.Value = sets;
                    }
                    else
                    {
                        e.Value = null;
                    }
                    break;
                case 10:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.EHP];
                    break;
                case 11:
                    e.Value = (int)combinations[e.RowIndex + 100 * (optimizePage - 1)].Item2[Stats.DMG];
                    break;

            }
        }


        private void b_NextPage_Click(object sender, EventArgs e)
        {
            if (optimizePage != ((combinations.Count + 99) / 100))
            {
                optimizePage++;
                dgv_OptimizeResults.Refresh();
                dgv_OptimizeResults.AutoResizeColumns();
                l_Pages.Text = optimizePage + " / " + ((combinations.Count + 99) / 100);
            }

        }

        private void B_PreviousPage_Click(object sender, EventArgs e)
        {
            if (optimizePage > 1)
            {
                optimizePage--;
                dgv_OptimizeResults.Refresh();
                dgv_OptimizeResults.AutoResizeColumns();
                l_Pages.Text = optimizePage + " / " + ((combinations.Count + 99) / 100);
            }
        }

        private void L_Pages_SizeChanged(object sender, EventArgs e)
        {
            b_NextPage.Location = new Point(l_Pages.Location.X + l_Pages.Size.Width + 3, b_NextPage.Location.Y);
        }

        //Sort results across pages 
        private void Dgv_OptimizeResults_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            switch (e.ColumnIndex)
            {
                case 0:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.ATK]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.ATK]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 1:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.SPD]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.SPD]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 2:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.Crit]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.Crit]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 3:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.CritDmg]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.CritDmg]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 4:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.HP]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.HP]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 5:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.DEF]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.DEF]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 6:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.EFF]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.EFF]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 7:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.RES]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.RES]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 8:
                    break;
                case 9:
                    break;
                case 10:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.EHP]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.EHP]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
                case 11:
                    if (sortColumn == e.ColumnIndex)
                    {
                        combinations = combinations.OrderBy(x => x.Item2[Stats.DMG]).ToList();
                        sortColumn = -1;
                    }
                    else
                    {
                        combinations = combinations.OrderByDescending(x => x.Item2[Stats.DMG]).ToList();
                        sortColumn = e.ColumnIndex;
                    }
                    break;
            }
            optimizePage = 1;
            dgv_OptimizeResults.Refresh();
            dgv_OptimizeResults.AutoResizeColumns();
            l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);
        }

        //Displays the items used in the selected gear combination. Similar to Dgv_Heroes_RowEnter
        private void Dgv_OptimizeResults_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (combinations.Count > 0)
            {
                List<Item> items = combinations[e.RowIndex + ((optimizePage - 1) * 100)].Item1;
                Item item = items.Find(x => x.Type == ItemType.Weapon);
                if (item != null)
                {
                    l_WeaponGradeOptimize.Text = item.Grade.ToString() + " Weapon";
                    l_WeaponGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_WeaponIlvlOptimize.Text = item.ILvl.ToString();
                    l_WeaponEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_WeaponMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_WeaponMainStatOptimize.Text = ((int)item.Main.Value).ToString();
                    l_WeaponSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_WeaponSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_WeaponGradeOptimize.Text = "";
                    l_WeaponIlvlOptimize.Text = "";
                    l_WeaponEnhanceOptimize.Text = "";
                    l_WeaponMainOptimize.Text = "";
                    l_WeaponMainStatOptimize.Text = "";
                    l_WeaponSetOptimize.Text = "";
                    pb_WeaponSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_WeaponSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_WeaponSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
                item = items.Find(x => x.Type == ItemType.Helmet);
                if (item != null)
                {
                    l_HelmetGradeOptimize.Text = item.Grade.ToString() + " Helmet";
                    l_HelmetGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_HelmetIlvlOptimize.Text = item.ILvl.ToString();
                    l_HelmetEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_HelmetMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_HelmetMainStatOptimize.Text = ((int)item.Main.Value).ToString();
                    l_HelmetSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_HelmetSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_HelmetGradeOptimize.Text = "";
                    l_HelmetIlvlOptimize.Text = "";
                    l_HelmetEnhanceOptimize.Text = "";
                    l_HelmetMainOptimize.Text = "";
                    l_HelmetMainStatOptimize.Text = "";
                    l_HelmetSetOptimize.Text = "";
                    pb_HelmetSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_HelmetSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_HelmetSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
                item = items.Find(x => x.Type == ItemType.Armor);
                if (item != null)
                {
                    l_ArmorGradeOptimize.Text = item.Grade.ToString() + " Armor";
                    l_ArmorGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_ArmorIlvlOptimize.Text = item.ILvl.ToString();
                    l_ArmorEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_ArmorMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_ArmorMainStatOptimize.Text = ((int)item.Main.Value).ToString();
                    l_ArmorSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_ArmorSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_ArmorGradeOptimize.Text = "";
                    l_ArmorIlvlOptimize.Text = "";
                    l_ArmorEnhanceOptimize.Text = "";
                    l_ArmorMainOptimize.Text = "";
                    l_ArmorMainStatOptimize.Text = "";
                    l_ArmorSetOptimize.Text = "";
                    pb_ArmorSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_ArmorSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_ArmorSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
                item = items.Find(x => x.Type == ItemType.Necklace);
                if (item != null)
                {
                    l_NecklaceGradeOptimize.Text = item.Grade.ToString() + " Necklace";
                    l_NecklaceGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_NecklaceIlvlOptimize.Text = item.ILvl.ToString();
                    l_NecklaceEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_NecklaceMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_NecklaceMainStatOptimize.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                    l_NecklaceSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_NecklaceSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_NecklaceGradeOptimize.Text = "";
                    l_NecklaceIlvlOptimize.Text = "";
                    l_NecklaceEnhanceOptimize.Text = "";
                    l_NecklaceMainOptimize.Text = "";
                    l_NecklaceMainStatOptimize.Text = "";
                    l_NecklaceSetOptimize.Text = "";
                    pb_NecklaceSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_NecklaceSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_NecklaceSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
                item = items.Find(x => x.Type == ItemType.Ring);
                if (item != null)
                {
                    l_RingGradeOptimize.Text = item.Grade.ToString() + " Ring";
                    l_RingGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_RingIlvlOptimize.Text = item.ILvl.ToString();
                    l_RingEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_RingMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_RingMainStatOptimize.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                    l_RingSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_RingSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_RingGradeOptimize.Text = "";
                    l_RingIlvlOptimize.Text = "";
                    l_RingEnhanceOptimize.Text = "";
                    l_RingMainOptimize.Text = "";
                    l_RingMainStatOptimize.Text = "";
                    l_RingSetOptimize.Text = "";
                    pb_RingSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_RingSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_RingSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
                item = items.Find(x => x.Type == ItemType.Boots);
                if (item != null)
                {
                    l_BootsGradeOptimize.Text = item.Grade.ToString() + " Boots";
                    l_BootsGradeOptimize.ForeColor = Util.gradeColors[item.Grade];
                    l_BootsIlvlOptimize.Text = item.ILvl.ToString();
                    l_BootsEnhanceOptimize.Text = "+" + item.Enhance.ToString();
                    l_BootsMainOptimize.Text = Util.statStrings[item.Main.Name];
                    l_BootsMainStatOptimize.Text = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                    l_BootsSetOptimize.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_BootsSetOptimize.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Optimize", true)[0]).Text = "";
                            ((Label)tb_Optimize.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                        }
                    }
                }
                else
                {
                    l_BootsGradeOptimize.Text = "";
                    l_BootsIlvlOptimize.Text = "";
                    l_BootsEnhanceOptimize.Text = "";
                    l_BootsMainOptimize.Text = "";
                    l_BootsMainStatOptimize.Text = "";
                    l_BootsSetOptimize.Text = "";
                    pb_BootsSetOptimize.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        ((Label)tb_Optimize.Controls.Find("l_BootsSub" + (i + 1) + "Optimize", true)[0]).Text = "";
                        ((Label)tb_Optimize.Controls.Find("l_BootsSub" + (i + 1) + "StatOptimize", true)[0]).Text = "";
                    }
                }
            }
        }

        //Equip the currenty selected optimization result and update the current stats of the hero
        private void B_EquipOptimize_Click(object sender, EventArgs e)
        {
            List<Item> items = combinations[dgv_OptimizeResults.SelectedCells[0].RowIndex + ((optimizePage - 1) * 100)].Item1;
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split(' ').Last());
            hero.equip(items);
            dgv_CurrentGear.Rows.Clear();
            object[] values = new object[dgv_CurrentGear.ColumnCount];
            if (cb_OptimizeHero.Text != "")
            {
                values[0] = (int)hero.CurrentStats[Stats.ATK];
                values[1] = (int)hero.CurrentStats[Stats.SPD];
                values[2] = hero.CurrentStats[Stats.Crit].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[3] = hero.CurrentStats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[4] = (int)hero.CurrentStats[Stats.HP];
                values[5] = (int)hero.CurrentStats[Stats.DEF];
                values[6] = hero.CurrentStats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[7] = hero.CurrentStats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                List<Set> activeSets = hero.activeSets();
                int count = 0;
                if (activeSets.Contains(Set.Unity))
                {
                    foreach (Set set in activeSets)
                    {
                        count += set == Set.Unity ? 1 : 0;
                    }
                }
                values[8] = (5 + (count * 4)) + "%";
                if (activeSets.Count > 0)
                {
                    Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(sets);
                    for (int i = 0; i < activeSets.Count; i++)
                    {
                        g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                    }
                    values[9] = sets;
                }
                else
                {
                    values[9] = null;
                }
                values[10] = (int)hero.CurrentStats[Stats.EHP];
                values[11] = (int)hero.CurrentStats[Stats.DMG];
                l_Results.Text = numberOfResults().ToString();
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = null;
                }
                l_Results.Text = "0";
            }
            dgv_CurrentGear.Rows.Add(values);
        }

        private void B_Export_Click(object sender, EventArgs e)
        {
            if (sfd_export.ShowDialog() == DialogResult.OK)
            {
                JObject json = createJson();
                File.WriteAllText(sfd_export.FileName, json.ToString());
            }
        }
        
        //Create a backup of the current item and hero collection in the base directory of the application when the application closes
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            JObject json = createJson();
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/Backup.json", json.ToString());
        }

        private JObject createJson()
        {
            JObject json = new JObject(
                    new JProperty("heroes",
                        new JArray(from h in data.Heroes
                                   select new JObject(
                                       new JProperty("ID", h.ID),
                                       new JProperty("Name", h.Name),
                                       new JProperty("Gear",
                                            new JArray(from g in h.getGear()
                                                       select new JObject(
                                                             new JProperty("ID", g.ID)))),
                                       new JProperty("Artifact",
                                            new JObject(
                                                new JProperty("ATK", h.Artifact.SubStats[0].Value),
                                                new JProperty("HP", h.Artifact.SubStats[1].Value))),
                                       new JProperty("Lvl", h.Lvl),
                                       new JProperty("Awakening", h.Awakening)))),
                    new JProperty("items",
                        new JArray(from i in data.Items
                                   select new JObject(
                                       new JProperty("ID", i.ID),
                                       new JProperty("Type", i.Type),
                                       new JProperty("Set", i.Set),
                                       new JProperty("Grade", i.Grade),
                                       new JProperty("Ilvl", i.ILvl),
                                       new JProperty("Enhance", i.Enhance),
                                       new JProperty("Main",
                                            new JObject(
                                                new JProperty("Name", i.Main.Name),
                                                new JProperty("Value", i.Main.Value))),
                                       new JProperty("SubStats",
                                            new JArray(from s in i.SubStats
                                                       select new JObject(
                                                            new JProperty("Name", s.Name),
                                                            new JProperty("Value", s.Value)))),
                                       new JProperty("Locked", i.Locked)))),
                    new JProperty("currentItemID", data.CurrentItemID),
                    new JProperty("currentHeroID", data.CurrentHeroID));
            return json;
        }

        //Update the current stats of the specified hero when the value of the critbonus changes
        private void Nud_CritBonus_ValueChanged(object sender, EventArgs e)
        {

            dgv_CurrentGear.Rows.Clear();
            object[] values = new object[dgv_CurrentGear.ColumnCount];
            if (cb_OptimizeHero.Text != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Items[cb_OptimizeHero.SelectedIndex].ToString().Split().Last());
                values[0] = (int)hero.CurrentStats[Stats.ATK];
                values[1] = (int)hero.CurrentStats[Stats.SPD];
                decimal crit = hero.CurrentStats[Stats.Crit] + (nud_CritBonus.Value / 100m);
                crit = crit > 1 ? 1 : crit;
                values[2] = crit.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[3] = hero.CurrentStats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[4] = (int)hero.CurrentStats[Stats.HP];
                values[5] = (int)hero.CurrentStats[Stats.DEF];
                values[6] = hero.CurrentStats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[7] = hero.CurrentStats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                List<Set> activeSets = hero.activeSets();
                int count = 0;
                if (activeSets.Contains(Set.Unity))
                {
                    foreach (Set set in activeSets)
                    {
                        count += set == Set.Unity ? 1 : 0;
                    }
                }
                values[8] = (5 + (count * 4)) + "%";
                if (activeSets.Count > 0)
                {
                    Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(sets);
                    for (int i = 0; i < activeSets.Count; i++)
                    {
                        g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                    }
                    values[9] = sets;
                }
                else
                {
                    values[9] = null;
                }
                values[10] = (int)hero.CurrentStats[Stats.EHP];
                values[11] = (int)((hero.CurrentStats[Stats.ATK] * (1 - crit)) + (hero.CurrentStats[Stats.ATK] * crit * hero.CurrentStats[Stats.CritDmg]));
                l_Results.Text = numberOfResults().ToString();
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = null;
                }
                l_Results.Text = "0";
            }
            dgv_CurrentGear.Rows.Add(values);
        }

        private void B_UnequipAll_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            hero.unequipAll();
            updateHeroList();
        }

        
    }
}
