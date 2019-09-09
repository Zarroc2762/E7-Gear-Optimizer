using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
        List<(Item[], SStats)> combinations = new List<(Item[], SStats)>();
        List<int> filteredCombinations = new List<int>();
        int optimizePage = 1;
        int sortColumn = -1;
        Dictionary<Stats, (float,float)> forceStats = new Dictionary<Stats, (float,float)>();
        Dictionary<Stats, (float, float)> filterStats = new Dictionary<Stats, (float, float)>();
        CancellationTokenSource tokenSource;
        string[] args = Environment.GetCommandLineArgs();
        Hero optimizeHero = null;
        static bool limitResults = Properties.Settings.Default.LimitResults;
        static int limitResultsNum = Properties.Settings.Default.LimitResultsNum;
        static long resultsCurrent;//long is used to allow use Interlocked.Read() as that method is more clear than .CompareExchange()
        private bool useCache
        {
            get => Properties.Settings.Default.UseCache;
            set
            {
                if (value && !Directory.Exists(Properties.Settings.Default.CacheDirectory))
                {
                    Directory.CreateDirectory(Properties.Settings.Default.CacheDirectory);
                }
                Properties.Settings.Default.UseCache = value;
            }
        }
        private void setLastUsedFileName(string lastUsedFileName, bool web)
        {
            Properties.Settings.Default.LastUsedFileName = lastUsedFileName;
            Properties.Settings.Default.LastUsedFileNameWeb = web;
        }

        // Array of parent menu items for skill damage columns
        private ToolStripMenuItem[] contextMenuStrip1SkillMenuItems;

        private int enemyDef = 0;

        public Main()
        {
            InitializeComponent();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            Icon = Icon.FromHandle(Util.ResizeImage(Properties.Resources.bookmark, 19,18).GetHicon());
            if (args.Length == 1)
            {
                try
                {
                    Process.Start("E7 Optimizer Updater.exe");
                }
                catch
                {
                    MessageBox.Show("Could not find E7 optimizer Updater.exe");
                }
            }
            if (Properties.Settings.Default.UseCache)
            {
                Directory.CreateDirectory(Properties.Settings.Default.CacheDirectory);
            }
            //Read list of heroes from epicsevendb.com
            try
            {
                string json;
                string cacheFileName = Path.Combine(Properties.Settings.Default.CacheDirectory, "db.hero.json");
                if (useCache && File.Exists(cacheFileName))
                {
                    json = File.ReadAllText(cacheFileName);
                }
                else
                {
                    json = Util.client.DownloadString(Util.ApiUrl + "/hero/");
                    if (useCache)
                    {
                        File.WriteAllText(cacheFileName, json);
                    }
                }
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

            foreach (Stats stat in (Stats[])Enum.GetValues(typeof(Stats)))
            {
                forceStats[stat] = (0, float.MaxValue);
                filterStats[stat] = (0, float.MaxValue);
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
            for (int i = 1; i < tc_InventorySets.TabPages.Count; i++)
            {
                tc_InventorySets.TabPages[i].ImageIndex = i - 1;
            }
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
            cb_Set1.Items.Add("");
            cb_Set2.Items.Add("");
            cb_Set3.Items.Add("");
            cb_Eq.Items.Add("");
            dgv_OptimizeResults.RowCount = 0;
            dgv_Inventory.Columns[0].SortMode = DataGridViewColumnSortMode.Programmatic;
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
            richTextBox1.SelectedText = "Select the hero to optimize. Enter your stat requirements. Select which main stat to focus on for the right side of your equipment. It is recommended to keep the estimated results < 10,000,000 to not use too much RAM. 10,000,000 results equal about 4 GB RAM usage.\n\n";
            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 0;
            richTextBox1.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox1.SelectedText = "Export your Collection! It is NOT saved when closing the Program.";
            richTextBox2.SelectionFont = new Font(FontFamily.GenericSansSerif, 15, FontStyle.Bold);
            richTextBox2.SelectedText = "How to use: \n\n";
            richTextBox2.SelectionBullet = true;
            richTextBox2.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox2.SelectedText = "Start at the top and select the heroes of yor arena/GW team.\n";
            richTextBox2.SelectionBullet = false;
            richTextBox2.SelectedText = "\n";
            richTextBox2.SelectionBullet = true;
            richTextBox2.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox2.SelectedText = "Go from fastest (probably your CR pusher) in slot 1 to slowest in slot 3 (GW) / 4 (arena).\n";
            richTextBox2.SelectionBullet = false;
            richTextBox2.SelectedText = "\n";
            richTextBox2.SelectionBullet = true;
            richTextBox2.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox2.SelectedText = "Input SPD from imprints and/or exclusive equipment.\n";
            richTextBox2.SelectionBullet = false;
            richTextBox2.SelectedText = "\n";
            richTextBox2.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
            richTextBox2.SelectedText = "=> Red values indicate possible switches in the turn order of your team.";
            lb_Main.SelectedIndex = 0;
            lb_Sub1.SelectedIndex = 0;
            lb_Sub2.SelectedIndex = 0;
            lb_Sub3.SelectedIndex = 0;
            lb_Sub4.SelectedIndex = 0;
            // Initialize controls' values from Properties.Settings
            cb_LimitResults.Checked = Properties.Settings.Default.LimitResults;
            nud_LimitResults.Enabled = Properties.Settings.Default.LimitResults;
            nud_LimitResults.Value = Properties.Settings.Default.LimitResultsNum;
            cb_ImportOnLoad.Checked = Properties.Settings.Default.ImportOnLoad;
            cb_CacheWeb.Checked = useCache;
            b_ClearCache.Enabled = useCache;
            nud_EnemyDef.Value = enemyDef = Properties.Settings.Default.EnemyDefence;
            is_Weapon.Image = Properties.Resources.weapon;
            is_Helmet.Image = Properties.Resources.helmet;
            is_Armor.Image = Properties.Resources.armor;
            is_Necklace.Image = Properties.Resources.necklace;
            is_Ring.Image = Properties.Resources.ring;
            is_Boots.Image = Properties.Resources.boots;
            is_WeaponOptimize.Image = Properties.Resources.weapon;
            is_HelmetOptimize.Image = Properties.Resources.helmet;
            is_ArmorOptimize.Image = Properties.Resources.armor;
            is_NecklaceOptimize.Image = Properties.Resources.necklace;
            is_RingOptimize.Image = Properties.Resources.ring;
            is_BootsOptimize.Image = Properties.Resources.boots;
            // Initialize visibility of columns on Optimization tab based on saved Settings
            foreach (DataGridViewColumn col in dgv_CurrentGear.Columns)
            {
                var statName = col.Name.Substring(2, col.Name.LastIndexOf('_') - 2);//c_*_Current
                if (Properties.Settings.Default.OptimizationHiddenColumns.IndexOf(statName) >= 0)
                {
                    col.Visible = false;
                }
            }
            foreach (DataGridViewColumn col in dgv_OptimizeResults.Columns)
            {
                var statName = col.Name.Substring(2, col.Name.LastIndexOf('_') - 2);//c_*_Results
                if (Properties.Settings.Default.OptimizationHiddenColumns.IndexOf(statName) >= 0)
                {
                    col.Visible = false;
                }
            }
            // Initialize contextMenuStrip1
            foreach (var item in contextMenuStrip1.Items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem != null)
                {
                    var statName = menuItem.Name.Substring(5);//tsmi_*
                    if (Properties.Settings.Default.OptimizationHiddenColumns.IndexOf(statName) >= 0)
                    {
                        menuItem.Checked = false;
                    }
                }
            }
            // (Name suffix, Text suffix, ToolTipText)
            var tuples = new (string, string, string)[]
            {
                ("Normal", "Normal Damage", ""),
                ("Crit", "Critical Damage", ""),
                ("Avg", "Average Damage", "Crit*CritDmg+(1-Crit)*NormalDmg"),
                ("Avg/SPD", "Average Damage * SPD / 100", "AverageDmg*SPD/100")
            };
            contextMenuStrip1SkillMenuItems = new ToolStripMenuItem[]
            {
                tsmi_S1, tsmi_S2, tsmi_S3, tsmi_SB
            };
            // Add skill menu items and DataGridViews columns
            foreach (var tsmi in contextMenuStrip1SkillMenuItems)
            {
                tsmi.DropDownItemClicked += ContextMenuStrip1_ItemClicked;
                foreach (var tuple in tuples)
                {
                    string skillNameShort = tsmi.Name.Substring(5);//tsmi_*, S1, S2, etc.
                    string statName = skillNameShort + "_" + tuple.Item1;
                    bool visible = Properties.Settings.Default.OptimizationVisibleSkillColumns.Contains(statName);
                    ToolStripMenuItem item = new ToolStripMenuItem()
                    {
                        Name = tsmi.Name + "_" + tuple.Item1,
                        Text = tuple.Item2,
                        ToolTipText = tuple.Item3,
                        CheckOnClick = true,
                        Checked = visible
                    };
                    tsmi.DropDownItems.Add(item);
                    DataGridViewTextBoxColumn columnCurrent = new DataGridViewTextBoxColumn();
                    columnCurrent.Visible = visible;
                    columnCurrent.Name = $"c_{statName}_Current";
                    columnCurrent.HeaderText = skillNameShort + " " + tuple.Item1;
                    columnCurrent.ToolTipText = tsmi.Text + " " + tuple.Item2;
                    dgv_CurrentGear.Columns.Add(columnCurrent);
                    DataGridViewTextBoxColumn columnResults = new DataGridViewTextBoxColumn();
                    columnResults.Visible = visible;
                    columnResults.Name = $"c_{statName}_Results";
                    columnResults.HeaderText = skillNameShort + " " + tuple.Item1;
                    columnResults.ToolTipText = tsmi.Text + " " + tuple.Item2;
                    dgv_OptimizeResults.Columns.Add(columnResults);
                }
            }
            foreach (DataGridViewColumn col in dgv_CurrentGear.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            foreach (DataGridViewColumn col in dgv_OptimizeResults.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void B_import_Click(object sender, EventArgs e)
        {
            if (rb_import_this.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    import(ofd_import.FileName);
                }
            }
            else if (rb_import_web.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    import(ofd_import.FileName, true);
                }
            }
            else
            {
                MessageBox.Show("Please select the source to import from!");
            }
        }

        private void import(string fileName, bool web = false, bool append = false)
        {
            Import importForm = new Import(data, fileName, web, append);
            importForm.ShowDialog();
            if (!importForm.result)
            {
                string message = web
                    ? "Corrupted or wrong file format. Please select a JSON file exported by /u/HyrTheWinter's Equipment Optimizer!"
                    : "Corrupted or wrong file format. Please select a JSON file exported by this application!";
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                l_ImportResults.Text = $"Successfully imported {importForm.HeroesImported} heroes and {importForm.ItemsImported} items from {fileName}";
                l_ImportResults.ForeColor = Color.Green;
                updateHeroList();
                setLastUsedFileName(fileName, web);
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
                l_Results.Text = numberOfResults().ToString("#,0");
                updateCurrentGear();
            }
            else if (((TabControl)(sender)).SelectedIndex == 4)
            {
                cb1_SpeedTuner.Items.Clear();
                cb2_SpeedTuner.Items.Clear();
                cb3_SpeedTuner.Items.Clear();
                cb4_SpeedTuner.Items.Clear();
                foreach (Hero hero in data.Heroes)
                {
                    cb1_SpeedTuner.Items.Add(hero.Name + " " + hero.ID);
                    cb2_SpeedTuner.Items.Add(hero.Name + " " + hero.ID);
                    cb3_SpeedTuner.Items.Add(hero.Name + " " + hero.ID);
                    cb4_SpeedTuner.Items.Add(hero.Name + " " + hero.ID);
                }
                cb1_SpeedTuner_SelectedIndexChanged(null, null);
                Cb2_SpeedTuner_SelectedIndexChanged(null, null);
                Cb3_SpeedTuner_SelectedIndexChanged(null, null);
                Cb4_SpeedTuner_SelectedIndexChanged(null, null);
            }
        }


        private void updateItemList()
        {
            //clear currently displayed items
            Point cell = dgv_Inventory.CurrentCellAddress;
            DataGridViewColumn sortColumn = dgv_Inventory.SortedColumn;
            SortOrder order = dgv_Inventory.SortOrder;
            string ID = cell.Y >= 0 ? dgv_Inventory.Rows[cell.Y].Cells["c_ItemID"].Value.ToString() : null;
            dgv_Inventory.Rows.Clear();

            //calculate new list of items based on the selected type filter
            ItemType filter = tc_Inventory.SelectedIndex == 0 ? ItemType.All : (ItemType)(tc_Inventory.SelectedIndex - 1);
            var filteredList = (filter == ItemType.All) ? data.Items : data.Items.Where(x => x.Type == filter);
            Set? setFilter = tc_InventorySets.SelectedIndex == 0 ? null : (Set?)(tc_InventorySets.SelectedIndex - 1);
            filteredList = setFilter == null ? filteredList : filteredList.Where(x => x.Set == setFilter);
            foreach (Item item in filteredList)
            {
                object[] values = new object[dgv_Inventory.ColumnCount];
                values[0] = Sets.Images[(int)item.Set];
                values[20] = (int)item.Set;
                values[1] = Types.Images[(int)item.Type];
                values[21] = (int)item.Type;
                values[2] = item.Grade.ToString();
                values[3] = item.ILvl;
                values[4] = "+" + item.Enhance.ToString();
                values[5] = item.Main.Name.ToString().Replace("Percent","%");
                values[6] = Util.percentStats.Contains(item.Main.Name) ? item.Main.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.Main.Value.ToString();
                values[19] = item.Equipped == null ? "" : item.Equipped.Name;
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
                values[18] = item.WSS;
                values[22] = item.ID;
                values[23] = item.Locked.ToString();
                dgv_Inventory.Rows.Add(values);
            }
            l_ItemCount.Text = filteredList.Count().ToString();
            //restore previous sorting and select previously selected cell
            if (order != SortOrder.None) dgv_Inventory.Sort(sortColumn, (ListSortDirection)Enum.Parse(typeof(ListSortDirection), order.ToString()));
            //order of items can change due to change of sortColumn values, so restore selected row by item id
            if (cell.X > -1 && cell.Y > -1 && cell.X < dgv_Inventory.ColumnCount && cell.Y < dgv_Inventory.RowCount)
            {
                dgv_Inventory.CurrentCell = dgv_Inventory.Rows.Cast<DataGridViewRow>().Where(x => x.Cells["c_ItemID"].Value.ToString() == ID).First().Cells[cell.X];
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
                values[16] = (5 + (count * 4)) + "%";
                Dictionary<Stats, float> stats = hero.CurrentStats;
                values[7] = (int)stats[Stats.ATK];
                values[8] = (int)stats[Stats.SPD];
                values[9] = stats[Stats.Crit].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[10] = stats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[11] = (int)stats[Stats.HP];
                values[12] = (int)stats[Stats.HPpS];
                values[13] = (int)stats[Stats.DEF];
                values[14] = stats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[15] = stats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[17] = (int)stats[Stats.EHP];
                values[18] = (int)stats[Stats.EHPpS];
                values[19] = (int)stats[Stats.DMG];
                values[20] = (int)stats[Stats.DMGpS];
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
            ((RadioButton)p_Set.Controls.Find("rb_" + ((Set)row.Cells["c_SetID"].Value).ToString() + "Set", false)[0]).Checked = true;
            ((RadioButton)p_Type.Controls.Find("rb_" + ((ItemType)row.Cells["c_TypeID"].Value).ToString() + "Type", false)[0]).Checked = true;
            ((RadioButton)p_Grade.Controls.Find("rb_" + row.Cells["c_Grade"].Value + "Grade", false)[0]).Checked = true;
            nud_ILvl.Value = (int)row.Cells["c_ILvl"].Value;
            nud_Enhance.Value = (int)float.Parse(((string)row.Cells["c_Enhance"].Value).Substring(1));
            lb_Main.SelectedIndex = lb_Main.FindStringExact((string)row.Cells["c_Main"].Value);
            nud_Main.Value = (int)float.Parse(((string)row.Cells["c_Value"].Value).Replace("%",""));
            int subStat = 0;
            for (int i = 7; i < 18; i++)
            {
                if ((string)row.Cells[i].Value != "")
                {
                    ListBox lb = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + (subStat + 1) , false)[0];
                    lb.SelectedIndex = lb.FindStringExact(dgv_Inventory.Columns[i].HeaderText);
                    ((NumericUpDown)tb_Inventory.Controls.Find("nud_Sub" + (subStat+1), false)[0]).Value = (int)float.Parse(((string)row.Cells[i].Value).Replace("%", ""));
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
            string ID = (string)dgv_Inventory.Rows[e.RowIndex].Cells["c_ItemID"].Value;
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 5; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.ATK ? Stats.ATK.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.ATK;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 5; i++)
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 5; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.HP ? Stats.HP.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.HP;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length - 5; i++)
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
                {
                    lb_Main.Items[i] = (Stats)i == Stats.DEF ? Stats.DEF.ToString() : "";
                }
                lb_Main.SelectedIndex = (int)Stats.DEF;
                for (int sub = 1; sub < 5; sub++)
                {
                    ListBox current = (ListBox)tb_Inventory.Controls.Find("lb_Sub" + sub, false)[0];
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
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
                    for (int i = 0; i < Enum.GetNames(typeof(Stats)).Length-5; i++)
                    {
                        current.Items[i] = ((Stats)i).ToString().Replace("Percent", "%");
                    }
                }
            }
        }

        //Create a new item with the selected stats without equipping it to a hero
        private void B_NewItem_Click(object sender, EventArgs e)
        {
            createNewItem();
        }

        //Create a new item with the selected stats equipped on the selected hero
        private void B_NewItemEquipped_Click(object sender, EventArgs e)
        {
            
            Hero hero = null;
            if (cb_Eq.Text != "")
            {
                hero = data.Heroes.Find(x => x.Name == String.Join(" ", cb_Eq.Text.Split(' ').Reverse().Skip(1).Reverse()));
            }
            createNewItem(hero);
        }

        /// <summary>
        /// Creates new item based on UI controls values and equips it to <paramref name="hero"/>
        /// </summary>
        /// <param name="hero">The hero to equip new item to</param>
        private void createNewItem(Hero hero = null)
        {
            Set set = (Set)Enum.Parse(typeof(Set), p_Set.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Set", ""));
            ItemType type = (ItemType)Enum.Parse(typeof(ItemType), p_Type.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Type", ""));
            Grade grade = (Grade)Enum.Parse(typeof(Grade), p_Grade.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Grade", ""));

            Stat main;
            if (lb_Main.SelectedItem.ToString() != "")
            {
                main = new Stat((Stats)Enum.Parse(typeof(Stats), lb_Main.SelectedItem.ToString().Replace("%", "Percent")), (float)nud_Main.Value);
            }
            else
            {
                MessageBox.Show("Invalid Mainstat");
                return;
            }
            List<Stat> substats = new List<Stat>();
            string selection = lb_Sub1.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub1.Value));
            selection = lb_Sub2.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub2.Value));
            selection = lb_Sub3.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub3.Value));
            selection = lb_Sub4.SelectedItem.ToString();
            if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub4.Value));

            int ilvl = (int)nud_ILvl.Value;
            int enh = (int)nud_Enhance.Value;
            bool locked = false;
            if (Locked)
            {
                locked = true;
            }
            Item newItem = new Item(data.incrementItemID(), type, set, grade, ilvl, enh, main, substats.ToArray(), hero, locked);
            hero?.equip(newItem);
            data.Items.Add(newItem);
            updateItemList();
            //select the created item if it is displayed with the current filter
            if ((tc_Inventory.SelectedIndex == 0 || (ItemType)(tc_Inventory.SelectedIndex - 1) == type) && (tc_InventorySets.SelectedIndex == 0 || (Set)(tc_InventorySets.SelectedIndex - 1) == set))
            {
                dgv_Inventory.CurrentCell = dgv_Inventory.Rows.Cast<DataGridViewRow>().Where(x => x.Cells["c_ItemID"].Value.ToString() == newItem.ID).First().Cells[0];
            }
        }

        //Overwrite the currently selected item with the selected stats
        private void B_EditItem_Click(object sender, EventArgs e)
        {
            if (dgv_Inventory.SelectedRows.Count > 0)
            {
                string ID = (string)dgv_Inventory.SelectedRows[0].Cells["c_itemID"].Value;
                Item item = data.Items.Find(x => x.ID == ID);
                item.Set = (Set)Enum.Parse(typeof(Set), p_Set.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Set", ""));
                item.Type = (ItemType)Enum.Parse(typeof(ItemType), p_Type.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Type", ""));
                item.Grade = (Grade)Enum.Parse(typeof(Grade), p_Grade.Controls.OfType<RadioButton>().First(x => x.Checked).Name.Replace("rb_", "").Replace("Grade", ""));

                if (lb_Main.SelectedItem.ToString() != "")
                {
                    item.Main = new Stat((Stats)Enum.Parse(typeof(Stats), lb_Main.SelectedItem.ToString().Replace("%", "Percent")), (float)nud_Main.Value);
                }
                else
                {
                    MessageBox.Show("Invalid Mainstat");
                    return;
                }

                List<Stat> substats = new List<Stat>();
                string selection = lb_Sub1.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub1.Value));
                selection = lb_Sub2.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub2.Value));
                selection = lb_Sub3.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub3.Value));
                selection = lb_Sub4.SelectedItem.ToString();
                if (selection != "-----") substats.Add(new Stat((Stats)Enum.Parse(typeof(Stats), selection.Replace("%", "Percent")), (float)nud_Sub4.Value));
                item.SubStats = substats.ToArray();

                item.ILvl = (int)nud_ILvl.Value;
                item.Enhance = (int)nud_Enhance.Value;
                item.calcWSS();

                Hero newHero = data.Heroes.Find(x => x.Name == String.Join(" ", cb_Eq.Text.Split(' ').Reverse().Skip(1).Reverse()));
                if (newHero != item.Equipped)
                {
                    if (item.Equipped != null) item.Equipped.unequip(item);
                    if (newHero != null)
                    {
                        newHero.equip(item);
                    }
                }
                else if (newHero != null)
                {
                    if (item.Equipped != null) item.Equipped.unequip(item);
                    newHero.equip(item);
                }
                updateItemList();
                //select the edited item if it is displayed with the current filter
                if ((tc_Inventory.SelectedIndex == 0 || (ItemType)(tc_Inventory.SelectedIndex - 1) == item.Type) && (tc_InventorySets.SelectedIndex == 0 || (Set)(tc_InventorySets.SelectedIndex - 1) == item.Set))
                {
                    dgv_Inventory.CurrentCell = dgv_Inventory.Rows.Cast<DataGridViewRow>().Where(x => x.Cells["c_ItemID"].Value.ToString() == item.ID).First().Cells[0];
                }
            }
        }

        //Delete the currently selected item and unequip it
        private void B_RemoveItem_Click(object sender, EventArgs e)
        {
            if (dgv_Inventory.SelectedRows.Count > 0)
            {
                string ID = (string)dgv_Inventory.SelectedRows[0].Cells["c_itemID"].Value;
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
                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (float)nud_ArtifactAttack.Value), new Stat(Stats.HP, (float)nud_ArtifactHealth.Value) }, null, false);
                    Hero newHero = new Hero(data.incrementHeroID(), cb_Hero.Text, new List<Item>(), artifact, int.Parse(cb_HeroLvl.Text), int.Parse(cb_HeroAwakening.Text));
                    List<Item> eq = hero.getGear();
                    hero.unequipAll();
                    newHero.equip(eq);
                    data.Heroes.Remove(hero);
                    data.Heroes.Add(newHero);
                    newHero.Skills[0].Enhance = (int)nud_S1.Value;
                    newHero.Skills[1].Enhance = (int)nud_S2.Value;
                    newHero.Skills[2].Enhance = (int)nud_S3.Value;
                }
                else
                {
                    hero.Lvl = int.Parse(cb_HeroLvl.Text);
                    hero.Awakening = int.Parse(cb_HeroAwakening.Text);
                    if (hero.Awakening > hero.Lvl / 10)
                    {
                        hero.Awakening = hero.Lvl / 10;
                    }

                    Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (float)nud_ArtifactAttack.Value), new Stat(Stats.HP, (float)nud_ArtifactHealth.Value) }, null, false);
                    hero.Artifact = artifact;
                    hero.updateBaseStats();
                    hero.calcAwakeningStats();
                    hero.calcStats();
                    hero.Skills[0].Enhance = (int)nud_S1.Value;
                    hero.Skills[1].Enhance = (int)nud_S2.Value;
                    hero.Skills[2].Enhance = (int)nud_S3.Value;
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
            nud_S1.Value = hero.Skills[0].Enhance;
            nud_S2.Value = hero.Skills[1].Enhance;
            nud_S3.Value = hero.Skills[2].Enhance;
            tt_Skills.SetToolTip(nud_S1, $"+{hero.Skills[0].DamageIncrease}% damage dealt");
            tt_Skills.SetToolTip(nud_S2, $"+{hero.Skills[1].DamageIncrease}% damage dealt");
            tt_Skills.SetToolTip(nud_S3, $"+{hero.Skills[2].DamageIncrease}% damage dealt");

            //Check whether the selected Hero has an item equipped in the slot and set the controls for the slot accordingly
            is_Weapon.Item = hero.getItem(ItemType.Weapon);
            is_Helmet.Item = hero.getItem(ItemType.Helmet);
            is_Armor.Item = hero.getItem(ItemType.Armor);
            is_Necklace.Item = hero.getItem(ItemType.Necklace);
            is_Ring.Item = hero.getItem(ItemType.Ring);
            is_Boots.Item = hero.getItem(ItemType.Boots);
        }

        //Create a new Hero and add it to the list
        private void B_AddHero_Click(object sender, EventArgs e)
        {
            if (cb_Hero.Text != "" && cb_HeroLvl.Text != "" && cb_HeroAwakening.Text != "")
            {
                //Artifacts only consist of an ATK and a HP stat for the purpose of hero stat calculation. So the ID, type, set etc. is irrelevant
                Item artifact = new Item("", ItemType.Artifact, Set.Attack, Grade.Epic, 0, 0, new Stat(), new Stat[] { new Stat(Stats.ATK, (float)nud_ArtifactAttack.Value), new Stat(Stats.HP, (float)nud_ArtifactHealth.Value) }, null, false);
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

        /// <summary>
        /// Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        /// </summary>
        /// <param name="itemType">ItemType of the item</param>
        private void _equipItem(ItemType itemType)
        {
            List<Item> list = data.Items.Where(x => x.Type == itemType).ToList();
            SelectItemDialog dialog = new SelectItemDialog(list);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Item newItem = dialog.getSelectedItem();
                Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
                if (newItem != null)
                {
                    newItem.Equipped?.unequip(newItem);
                    Item oldItem = hero.getItem(itemType);
                    if (oldItem != null) hero.unequip(oldItem);
                    hero.equip(newItem);
                }
                else
                {
                    Item olditem = hero.getItem(itemType);
                    if (olditem != null)
                    {
                        hero.unequip(olditem);
                    }
                }
                updateHeroList();
            }
        }

        /// <summary>
        /// Switch to the Inventory tab and select the equipped item
        /// </summary>
        /// <param name="itemType">ItemType of the item</param>
        private void _editEquippedItem(ItemType itemType)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            Item item = hero.getItem(itemType);
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
            _equipItem(ItemType.Weapon);
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
            _equipItem(ItemType.Helmet);
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditHelmet_Click(object sender, EventArgs e)
        {
            _editEquippedItem(ItemType.Helmet);
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipArmor_Click(object sender, EventArgs e)
        {
            _equipItem(ItemType.Armor);
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipNecklace_Click(object sender, EventArgs e)
        {
            _equipItem(ItemType.Necklace);
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipRing_Click(object sender, EventArgs e)
        {
            _equipItem(ItemType.Ring);
        }

        //Show the "Select Item" dialog with a list of items which can be equipped in the selected slot
        private void B_EquipBoots_Click(object sender, EventArgs e)
        {
            _equipItem(ItemType.Boots);
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditArmor_Click(object sender, EventArgs e)
        {
            _editEquippedItem(ItemType.Armor);
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditNecklace_Click(object sender, EventArgs e)
        {
            _editEquippedItem(ItemType.Necklace);
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditRing_Click(object sender, EventArgs e)
        {
            _editEquippedItem(ItemType.Ring);
        }

        //Switch to the Inventory tab and select the equipped item
        private void B_EditBoots_Click(object sender, EventArgs e)
        {
            _editEquippedItem(ItemType.Boots);
        }


        private void Pb_ItemLocked_Click(object sender, EventArgs e)
        {
            /*if (Locked)
            {
                pb_ItemLocked.Image = Properties.Resources.unlocked;
                Locked = false;
            }
            else
            {
                pb_ItemLocked.Image = Properties.Resources.locked;
                Locked = true;
            }*/
            if (dgv_Inventory.SelectedRows.Count > 0)
            {
                string ID = (string)dgv_Inventory.SelectedRows[0].Cells["c_ItemID"].Value;
                Item item = data.Items.Find(x => x.ID == ID);
                item.Locked = !item.Locked;
                Locked = !Locked;
                updateItemList();
            }
        }

        //Generate JSON String with the current items and heroes
        

        //Calculate and display the current stats of the selected hero
        private void Cb_OptimizeHero_SelectedIndexChanged(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
            is_WeaponOptimize.Hero = hero;
            is_HelmetOptimize.Hero = hero;
            is_ArmorOptimize.Hero = hero;
            is_NecklaceOptimize.Hero = hero;
            is_RingOptimize.Hero = hero;
            is_BootsOptimize.Hero = hero;
            updateCurrentGear();
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        //Estimate the number of results with the currenty selected criteria. This method only takes account of focus options for right side gear 
        //and whether locked/equipped items are included because set and stat filters can only be applied after a combination of items has been calculated.
        private long numberOfResults()
        {
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
            List<Set> setFocus = new List<Set>();
            if (cb_Set1.SelectedIndex != -1 && cb_Set1.Items[cb_Set1.SelectedIndex].ToString() != "")
                setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set1.Items[cb_Set1.SelectedIndex].ToString()));
            if (cb_Set2.SelectedIndex != -1 && cb_Set2.Items[cb_Set2.SelectedIndex].ToString() != "")
                setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set2.Items[cb_Set2.SelectedIndex].ToString()));
            if (cb_Set3.SelectedIndex != -1 && cb_Set3.Items[cb_Set3.SelectedIndex].ToString() != "")
                setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set3.Items[cb_Set3.SelectedIndex].ToString()));
            List<Item> Base = data.Items.Where(x => x.Enhance >= nud_EnhanceFocus.Value).ToList();
            if (!chb_Equipped.Checked)
            {
                Base = Base.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
            }
            if (!chb_Locked.Checked)
            {
                Base = Base.Where(x => !x.Locked || x.Equipped == hero).ToList();
            }
            long necklaces = getRightSideGear(Base, ItemType.Necklace).Where(x => checkSets(x, setFocus)).Count();
            long rings = getRightSideGear(Base, ItemType.Ring).Where(x => checkSets(x, setFocus)).Count();
            long boots = getRightSideGear(Base, ItemType.Boots).Where(x => checkSets(x, setFocus)).Count();
            long weapons = Base.Where(x => x.Type == ItemType.Weapon).Where(x => checkForceStats(x)).Where(x => checkSets(x, setFocus)).Count();
            long helmets = Base.Where(x => x.Type == ItemType.Helmet).Where(x => checkForceStats(x)).Where(x => checkSets(x, setFocus)).Count();
            long armors = Base.Where(x => x.Type == ItemType.Armor).Where(x => checkForceStats(x)).Where(x => checkSets(x, setFocus)).Count();

            if (cb_keepEquip.Checked)
            {
                List<Item> gear = hero.getGear();
                foreach (Item item in gear)
                {
                    switch (item.Type)
                    {
                        case ItemType.Armor:
                            armors = 1;
                            break;
                        case ItemType.Boots:
                            boots = 1;
                            break;
                        case ItemType.Helmet:
                            helmets = 1;
                            break;
                        case ItemType.Necklace:
                            necklaces = 1;
                            break;
                        case ItemType.Ring:
                            rings = 1;
                            break;
                        case ItemType.Weapon:
                            weapons = 1;
                            break;
                        default:
                            break;
                    }
                }
            }

            return weapons * helmets * armors * necklaces * rings * boots;
        }

        /// <summary>
        /// This method returns filtered collection of "right-side" items (necklaces, rings or boots) 
        /// </summary>
        /// <param name="items">Items to choose from</param>
        /// <param name="itemType">ItemType of items to get</param>
        /// <returns></returns>
        private IEnumerable<Item> getRightSideGear(IEnumerable<Item> items, ItemType itemType)
        {
            string focus;
            if (itemType == ItemType.Necklace)
            {
                focus = tb_NecklaceFocus.Text;
            }
            else if (itemType == ItemType.Ring)
            {
                focus = tb_RingFocus.Text;
            }
            else if (itemType == ItemType.Boots)
            {
                focus = tb_BootsFocus.Text;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(itemType));
            }
            if (string.IsNullOrWhiteSpace(focus))
            {
                return items.Where(x => x.Type == itemType && x.Enhance >= nud_EnhanceFocus.Value).Where(x => checkForceStats(x));
            }
            else
            {
                var statsStrings = focus.Split(' ');
                List<Stats> stats = new List<Stats>(statsStrings.Length);
                foreach (var s in statsStrings)
                {
                    stats.Add((Stats)Enum.Parse(typeof(Stats), s.Replace("%", "Percent")));
                }
                return items.Where(x => x.Type == itemType && x.Enhance >= nud_EnhanceFocus.Value).Where(x => stats.Contains(x.Main.Name)).Where(x => checkForceStats(x));
            }
        }

        private bool checkForceStats(Item item)
        {
            bool pass = true;
            foreach (Stats stat in (Stats[])Enum.GetValues(typeof(Stats)))
            {
                if (Util.rollableStats[item.Type].Contains(stat))
                {
                    (float, float) filter = forceStats[stat];
                    if (stat == item.Main.Name)
                    {
                        pass = pass && filter.Item1 <= item.Main.Value && filter.Item2 >= item.Main.Value;
                    }
                    else
                    {
                        if (filter.Item1 > 0)
                        {
                            bool exists = false;
                            foreach (Stat sub in item.SubStats)
                            {
                                if (sub.Name == stat)
                                {
                                    exists = true;
                                    pass = pass && filter.Item1 <= sub.Value;
                                }
                            }
                            pass = pass && exists;
                        }
                        if (filter.Item1 < float.MaxValue)
                        {
                            foreach (Stat sub in item.SubStats)
                            {
                                if (sub.Name == stat)
                                {
                                    pass = pass && filter.Item1 <= sub.Value;
                                }
                            }
                        }
                    }
                }
            }
            return pass;
        }

        private bool checkSets(Item item, List<Set> setFocus)
        {
            bool pass = true;
            int slots = Util.setSlots(setFocus);
            if (slots == 6)
            {
                pass = setFocus.Contains(item.Set);
            }
            else if (slots > 6)
            {
                pass = false;
            }
            return pass;
        }

        private void Chb_Locked_CheckedChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void Chb_Equipped_CheckedChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        //Filter the items based on the selected focus options and whether equipped/locked gear is used. Then executes an asynchronous call to the calculate method
        //with the selected stat and set filters.
        private async void B_Optimize_Click(object sender, EventArgs e)
        {
            filteredCombinations.Clear();
            dgv_OptimizeResults.RowCount = 0;
            dgv_OptimizeResults.Rows.Clear();
            combinations.Clear();
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
            optimizeHero = hero;
            if (hero != null)
            {
                List<Item> weapons = data.Items.Where(x => x.Type == ItemType.Weapon && x.Enhance >= nud_EnhanceFocus.Value).ToList();
                List<Item> helmets = data.Items.Where(x => x.Type == ItemType.Helmet && x.Enhance >= nud_EnhanceFocus.Value).ToList();
                List<Item> armors = data.Items.Where(x => x.Type == ItemType.Armor && x.Enhance >= nud_EnhanceFocus.Value).ToList();
                List<Item> necklaces = getRightSideGear(data.Items, ItemType.Necklace).ToList();
                List<Item> rings = getRightSideGear(data.Items, ItemType.Ring).ToList();
                List<Item> boots = getRightSideGear(data.Items, ItemType.Boots).ToList();
                if (!chb_Equipped.Checked)
                {
                    weapons = weapons.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                    helmets = helmets.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                    armors = armors.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                    necklaces = necklaces.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                    rings = rings.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                    boots = boots.Where(x => x.Equipped == null || x.Equipped == hero).ToList();
                }
                if (!chb_Locked.Checked)
                {
                    weapons = weapons.Where(x => !x.Locked || x.Equipped == hero).ToList();
                    helmets = helmets.Where(x => !x.Locked || x.Equipped == hero).ToList();
                    armors = armors.Where(x => !x.Locked || x.Equipped == hero).ToList();
                    necklaces = necklaces.Where(x => !x.Locked || x.Equipped == hero).ToList();
                    rings = rings.Where(x => !x.Locked || x.Equipped == hero).ToList();
                    boots = boots.Where(x => !x.Locked || x.Equipped == hero).ToList();
                }

                List<Set> setFocus = new List<Set>();
                if (cb_Set1.SelectedIndex != -1 && cb_Set1.Items[cb_Set1.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set1.Items[cb_Set1.SelectedIndex].ToString()));
                if (cb_Set2.SelectedIndex != -1 && cb_Set2.Items[cb_Set2.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set2.Items[cb_Set2.SelectedIndex].ToString()));
                if (cb_Set3.SelectedIndex != -1 && cb_Set3.Items[cb_Set3.SelectedIndex].ToString() != "")
                    setFocus.Add((Set)Enum.Parse(typeof(Set), cb_Set3.Items[cb_Set3.SelectedIndex].ToString()));

                weapons = weapons.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();
                helmets = helmets.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();
                armors = armors.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();
                necklaces = necklaces.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();
                rings = rings.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();
                boots = boots.Where(x => checkSets(x, setFocus)).Where(x => checkForceStats(x)).ToList();

                if (cb_keepEquip.Checked)
                {
                    List<Item> gear = hero.getGear();
                    foreach (Item item in gear)
                    {
                        switch (item.Type)
                        {
                            case ItemType.Armor:
                                armors.Clear();
                                armors.Add(item);
                                break;
                            case ItemType.Boots:
                                boots.Clear();
                                boots.Add(item);
                                break;
                            case ItemType.Helmet:
                                helmets.Clear();
                                helmets.Add(item);
                                break;
                            case ItemType.Necklace:
                                necklaces.Clear();
                                necklaces.Add(item);
                                break;
                            case ItemType.Ring:
                                rings.Clear();
                                rings.Add(item);
                                break;
                            case ItemType.Weapon:
                                weapons.Clear();
                                weapons.Add(item);
                                break;
                            default:
                                break;
                        }
                    }
                }

                long numResults = weapons.Count * helmets.Count * armors.Count * necklaces.Count * rings.Count * boots.Count;
                if (numResults == 0)
                {
                    return;
                }
                float counter = 0;
                IProgress<int> progress = new Progress<int>(x =>
                {
                    counter += x;
                    var val = counter / numResults * 100;
                    val = val < 0 ? 0 : val;
                    pB_Optimize.Value = (int)(val);
                });
                pB_Optimize.Show();
                b_CancelOptimize.Show();
                List<Task<List<(Item[], SStats)>>> tasks = new List<Task<List<(Item[], SStats)>>>();
                tokenSource = new CancellationTokenSource();
                SStats sHeroStats = new SStats(hero.calcStatsWithoutGear((float)nud_CritBonus.Value / 100f));
                SStats sItemStats = new SStats();
                Dictionary<Stats, (float, float)> optimizedFilterStats = optimizeFilterStats();
                Interlocked.Exchange(ref resultsCurrent, 0);
                foreach (Item w in weapons)
                {
                    sItemStats.Add(w.AllStats);
                    foreach (Item h in helmets)
                    {
                        sItemStats.Add(h.AllStats);
                        foreach (Item a in armors)
                        {
                            sItemStats.Add(a.AllStats);

                            SStats sItemStatsTemp = new SStats(sItemStats);

                            tasks.Add(Task.Run(() => calculate(w, h, a, necklaces, rings, boots, hero, sHeroStats, optimizedFilterStats, setFocus, progress, sItemStatsTemp, cb_Broken.Checked, tokenSource.Token), tokenSource.Token));

                            sItemStats.Subtract(a.AllStats);
                        }
                        sItemStats.Subtract(h.AllStats);
                    }
                    sItemStats.Subtract(w.AllStats);
                }
                try
                {
                    if (tasks.Count > 0)
                    {
                        combinations = (await Task.WhenAll(tasks)).Aggregate((a, b) => { a.AddRange(b); return a; });
                        if (limitResults && combinations.Count > limitResultsNum)
                        {
                            combinations = combinations.Take(limitResultsNum).ToList();
                        }
                    }
                    b_CancelOptimize.Hide();
                    pB_Optimize.Hide();
                    pB_Optimize.Value = 0;
                    //Display the first page of results. Each page consists of 100 results
                    dgv_OptimizeResults.RowCount = Math.Min(100, combinations.Count);
                    optimizePage = 1;
                    l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);
                    if (limitResults && resultsCurrent >= limitResultsNum)
                    {
                        MessageBox.Show("Maximum number of combinations reached. Please try to narrow the filter.", "Limit break", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (OperationCanceledException)
                {
                    dgv_OptimizeResults.RowCount = Math.Min(100, combinations.Count);
                    optimizePage = 1;
                    l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);
                    b_CancelOptimize.Hide();
                    pB_Optimize.Hide();
                    pB_Optimize.Value = 0;
                }
            }
        }

        //Calculate all possible gear combinations and check whether they satisfy the given filters
        private static List<(Item[], SStats)> calculate(Item weapon, Item helmet,
                                                        Item armor, List<Item> necklaces,
                                                        List<Item> rings, List<Item> boots, Hero hero, 
                                                        SStats sStats,
                                                        Dictionary<Stats, (float, float)> filter, List<Set> setFocus,
                                                        IProgress<int> progress, SStats sItemStats,
                                                        bool brokenSets, CancellationToken ct)
        {
            List<(Item[], SStats)> combinations = new List<(Item[], SStats)>();
            if (limitResults && Interlocked.Read(ref resultsCurrent) >= limitResultsNum)
            {
                return combinations;
            }
            int[] setCounter = new int[Util.SETS_LENGTH];
            setCounter[(int)weapon.Set]++;
            setCounter[(int)helmet.Set]++;
            setCounter[(int)armor.Set]++;
            int count = 0;
            foreach (Item n in necklaces)
            {
                sItemStats.Add(n.AllStats);
                setCounter[(int)n.Set]++;
                foreach (Item r in rings)
                {
                    sItemStats.Add(r.AllStats);
                    setCounter[(int)r.Set]++;
                    foreach (Item b in boots)
                    {
                        ct.ThrowIfCancellationRequested();

                        sItemStats.Add(b.AllStats);
                        setCounter[(int)b.Set]++;

                        List<Set> activeSets = Util.activeSet(setCounter);

                        bool valid = true;
                        foreach (Set s in setFocus)
                        {
                            valid = valid && activeSets.Contains(s) && activeSets.Count(x => x == s) >= setFocus.Count(x => x == s);
                        }
                        if (!brokenSets)
                        {
                            valid = valid && (Util.setSlots(activeSets) == 6);
                        }
                        if (valid)
                        {
                            SStats setBonusStats = hero.setBonusStats(activeSets);
                            SStats calculatedStats = new SStats();
                            calculatedStats.ATK = (sStats.ATK * (1 + sItemStats.ATKPercent + setBonusStats.ATKPercent)) + sItemStats.ATK + hero.Artifact.SubStats[0].Value;
                            calculatedStats.HP = (sStats.HP * (1 + sItemStats.HPPercent + setBonusStats.HPPercent)) + sItemStats.HP + hero.Artifact.SubStats[1].Value;
                            calculatedStats.DEF = (sStats.DEF * (1 + sItemStats.DEFPercent + setBonusStats.DEFPercent)) + sItemStats.DEF;
                            calculatedStats.SPD = (sStats.SPD * (1 + setBonusStats.SPD)) + sItemStats.SPD;
                            calculatedStats.Crit = sStats.Crit + sItemStats.Crit + setBonusStats.Crit;
                            calculatedStats.CritDmg = sStats.CritDmg + sItemStats.CritDmg + setBonusStats.CritDmg;
                            calculatedStats.EFF = sStats.EFF + sItemStats.EFF + setBonusStats.EFF;
                            calculatedStats.RES = sStats.RES + sItemStats.RES + setBonusStats.RES;
                            valid = valid && checkFilter(calculatedStats, filter);
                            if (valid)
                            {
                                if (limitResults && Interlocked.Read(ref resultsCurrent) >= limitResultsNum)
                                {
                                    return combinations;
                                }
                                combinations.Add((new[] { weapon, helmet, armor, n, r, b }, calculatedStats));
                                Interlocked.Increment(ref resultsCurrent);
                            }
                        }
                        count++;
                        sItemStats.Subtract(b.AllStats);
                        setCounter[(int)b.Set]--;
                    }
                    sItemStats.Subtract(r.AllStats);
                    setCounter[(int)r.Set]--;
                }
                sItemStats.Subtract(n.AllStats);
                setCounter[(int)n.Set]--;
            }
            progress.Report(count);
            return combinations;
        }

        /// <summary>
        /// Optimizes global filterStats Dictionary by removing elements with empty values, so we don't have to poinessly check them while calculating
        /// </summary>
        /// <returns>Optimized filterStats</returns>
        private Dictionary<Stats, (float, float)> optimizeFilterStats()
        {
            Dictionary<Stats, (float, float)> stats = new Dictionary<Stats, (float, float)>();
            foreach (var pair in filterStats)
            {
                if (pair.Value.Item1 > 0 || pair.Value.Item2 < float.MaxValue)
                {
                    stats.Add(pair.Key, pair.Value);
                }
            }
            return stats;
        }

        //Get the value for the current cell depending on which page of results is displayed
        private void Dgv_OptimizeResults_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (dgv_OptimizeResults.RowCount == 0) return;
            if (e.RowIndex >= combinations.Count - ((optimizePage - 1) * 100)) return;
            if (filteredCombinations.Count > 0 && e.RowIndex >= filteredCombinations.Count - ((optimizePage - 1) * 100)) return;

            int iCombination = filteredCombinations.Count > 0 ? filteredCombinations[e.RowIndex + 100 * (optimizePage - 1)] : (e.RowIndex + 100 * (optimizePage - 1));
            List <Set> activeSets;
            switch (e.ColumnIndex)
            {
                case 0:
                    e.Value = (int)combinations[iCombination].Item2.ATK;
                    break;
                case 1:
                    e.Value = (int)combinations[iCombination].Item2.SPD;
                    break;
                case 2:
                    e.Value = combinations[iCombination].Item2.Crit.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 3:
                    e.Value = combinations[iCombination].Item2.CritDmg.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 4:
                    e.Value = (int)combinations[iCombination].Item2.HP;
                    break;
                case 5:
                    e.Value = (int)combinations[iCombination].Item2.HPpS;
                    break;
                case 6:
                    e.Value = (int)combinations[iCombination].Item2.DEF;
                    break;
                case 7:
                    e.Value = combinations[iCombination].Item2.EFF.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 8:
                    e.Value = combinations[iCombination].Item2.RES.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    break;
                case 9:
                    int count = 0;
                    activeSets = Util.activeSet(combinations[iCombination].Item1);
                    if (activeSets.Contains(Set.Unity))
                    {
                        foreach (Set set in activeSets)
                        {
                            count += set == Set.Unity ? 1 : 0;
                        }
                    }
                    e.Value = (5 + (count * 4)) + "%";
                    break;
                case 10:
                    activeSets = Util.activeSet(combinations[iCombination].Item1);
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
                case 11:
                    e.Value = (int)combinations[iCombination].Item2.EHP;
                    break;
                case 12:
                    e.Value = (int)combinations[iCombination].Item2.EHPpS;
                    break;
                case 13:
                    e.Value = (int)combinations[iCombination].Item2.DMG;
                    break;
                case 14:
                    e.Value = (int)combinations[iCombination].Item2.DMGpS;
                    break;
                default://Skill damage
                    // TODO refactor
                    var hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
                    SStats stats = combinations[iCombination].Item2;
                    int iSkill = (e.ColumnIndex - 15) / 4;
                    int iDmg = (e.ColumnIndex - 15) % 4;
                    bool soulburn = iSkill == 3;
                    var skill = soulburn ? hero.SkillWithSoulburn : hero.Skills[iSkill];
                    if (iDmg == 0)
                    {
                        e.Value = (int)skill.CalcDamage(stats, false, soulburn, enemyDef);
                        break;
                    }
                    if (iDmg == 1)
                    {
                        e.Value = (int)(skill.CalcDamage(stats, true, soulburn, enemyDef));
                        break;
                    }
                    float skillDmg = skill.CalcDamage(stats, false, soulburn, enemyDef);
                    float skillCritDmg = skill.CalcDamage(stats, true, soulburn, enemyDef);
                    float skillAvgDmg = stats.CritCapped * skillCritDmg + (1 - stats.Crit) * skillDmg;
                    if (iDmg == 2)
                    {
                        e.Value = (int)skillAvgDmg;
                        break;
                    }
                    e.Value = (int)(skillAvgDmg * stats.SPD / 100);
                    break;
            }
        }

        private void b_NextPage_Click(object sender, EventArgs e)
        {
            int count = filteredCombinations.Count > 0 ? filteredCombinations.Count : combinations.Count;
            if (optimizePage != (count + 99) / 100)
            {
                optimizePage++;
                onPageChange(count);
            }
        }

        private void B_PreviousPage_Click(object sender, EventArgs e)
        {
            int count = filteredCombinations.Count > 0 ? filteredCombinations.Count : combinations.Count;
            if (optimizePage > 1)
            {
                optimizePage--;
                onPageChange(count);
            }
        }

        private void onPageChange(int count)
        {
            dgv_OptimizeResults.RowCount = Math.Min(count - 100 * (optimizePage - 1), 100);
            dgv_OptimizeResults.Refresh();
            dgv_OptimizeResults.AutoResizeColumns();
            l_Pages.Text = optimizePage + " / " + ((count + 99) / 100);
        }

        private void L_Pages_SizeChanged(object sender, EventArgs e)
        {
            b_NextPage.Location = new Point(l_Pages.Location.X + l_Pages.Size.Width + 3, b_NextPage.Location.Y);
        }

        //Sort results across pages 
        private void Dgv_OptimizeResults_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            Func<(Item[], SStats), float> func = null;
            switch (e.ColumnIndex)
            {
                case 0:
                    func = x => x.Item2.ATK;
                    break;
                case 1:
                    func = x => x.Item2.SPD;
                    break;
                case 2:
                    func = x => x.Item2.Crit;
                    break;
                case 3:
                    func = x => x.Item2.CritDmg;
                    break;
                case 4:
                    func = x => x.Item2.HP;
                    break;
                case 5:
                    func = x => x.Item2.HPpS;
                    break;
                case 6:
                    func = x => x.Item2.DEF;
                    break;
                case 7:
                    func = x => x.Item2.EFF;
                    break;
                case 8:
                    func = x => x.Item2.RES;
                    break;
                case 9:
                    break;
                case 10:
                    break;
                case 11:
                    func = x => x.Item2.EHP;
                    break;
                case 12:
                    func = x => x.Item2.EHPpS;
                    break;
                case 13:
                    func = x => x.Item2.DMG;
                    break;
                case 14:
                    func = x => x.Item2.DMGpS;
                    break;
                default://Skill damage
                    // TODO refactor
                    var hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
                    int iSkill = (e.ColumnIndex - 15) / 4;
                    int iDmg = (e.ColumnIndex - 15) % 4;
                    bool soulburn = iSkill == 3;
                    var skill = soulburn ? hero.SkillWithSoulburn : hero.Skills[iSkill];
                    if (iDmg == 0)
                    {
                        func = x => skill.CalcDamage(x.Item2, false, soulburn, enemyDef);
                    }
                    else if (iDmg == 1)
                    {
                        func = x => skill.CalcDamage(x.Item2, true, soulburn, enemyDef);
                    }
                    else if (iDmg == 2)
                    {
                        func = x =>
                        {
                            float skillDmg = skill.CalcDamage(x.Item2, false, soulburn, enemyDef);
                            float skillCritDmg = skill.CalcDamage(x.Item2, true, soulburn, enemyDef);
                            return x.Item2.CritCapped * skillCritDmg + (1 - x.Item2.Crit) * skillDmg;
                        };
                    }
                    else if (iDmg == 3)
                    {
                        func = x =>
                        {
                            float skillDmg = skill.CalcDamage(x.Item2, false, soulburn, enemyDef);
                            float skillCritDmg = skill.CalcDamage(x.Item2, true, soulburn, enemyDef);
                            float skillAvgDmg = x.Item2.CritCapped * skillCritDmg + (1 - x.Item2.Crit) * skillDmg;
                            return skillAvgDmg * x.Item2.SPD / 100;
                        };
                    }
                    break;
            }
            if (func == null)
            {
                return;
            }
            if (sortColumn == e.ColumnIndex)
            {
                combinations = combinations.OrderBy(func).ToList();
                sortColumn = -1;
            }
            else
            {
                combinations = combinations.OrderByDescending(func).ToList();
                sortColumn = e.ColumnIndex;
            }
            if (filteredCombinations.Count > 0)
            {
                b_FilterResults.PerformClick();
            }
            optimizePage = 1;
            dgv_OptimizeResults.Refresh();
            dgv_OptimizeResults.AutoResizeColumns();
            dgv_OptimizeResults.CurrentCell = dgv_OptimizeResults.Rows[0].Cells[e.ColumnIndex];
            if (filteredCombinations.Count > 0)
            {
                l_Pages.Text = "1 / " + ((filteredCombinations.Count + 99) / 100);
            }
            else
            {
                l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);
            }
        }

        //Displays the items used in the selected gear combination. Similar to Dgv_Heroes_RowEnter
        private void Dgv_OptimizeResults_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (combinations.Count > 0)
            {
                List<Item> items;
                if (filteredCombinations.Count > 0)
                {
                    items = combinations[filteredCombinations[e.RowIndex + ((optimizePage - 1) * 100)]].Item1.ToList();
                }
                else
                {
                    items = combinations[e.RowIndex + ((optimizePage - 1) * 100)].Item1.ToList();
                }
                Item item;
                item = items.Find(x => x.Type == ItemType.Weapon);
                is_WeaponOptimize.Item = item;
                pb_OptimizeWeaponEquipped.Image = item?.Equipped?.Portrait;
                item = items.Find(x => x.Type == ItemType.Helmet);
                is_HelmetOptimize.Item = item;
                pb_OptimizeHelmetEquipped.Image = item?.Equipped?.Portrait;
                item = items.Find(x => x.Type == ItemType.Armor);
                is_ArmorOptimize.Item = item;
                pb_OptimizeArmorEquipped.Image = item?.Equipped?.Portrait;
                item = items.Find(x => x.Type == ItemType.Necklace);
                is_NecklaceOptimize.Item = item;
                pb_OptimizeNecklaceEquipped.Image = item?.Equipped?.Portrait;
                item = items.Find(x => x.Type == ItemType.Ring);
                is_RingOptimize.Item = item;
                pb_OptimizeRingEquipped.Image = item?.Equipped?.Portrait;
                item = items.Find(x => x.Type == ItemType.Boots);
                is_BootsOptimize.Item = item;
                pb_OptimizeBootsEquipped.Image = item?.Equipped?.Portrait;
            }
        }

        //Equip the currenty selected optimization result and update the current stats of the hero
        private void B_EquipOptimize_Click(object sender, EventArgs e)
        {
            List<Item> items;
            if (filteredCombinations.Count > 0)
            {
                items = combinations[filteredCombinations[dgv_OptimizeResults.SelectedCells[0].RowIndex + ((optimizePage - 1) * 100)]].Item1.ToList();
            }
            else
            {
                items = combinations[dgv_OptimizeResults.SelectedCells[0].RowIndex + ((optimizePage - 1) * 100)].Item1.ToList();
            }
            Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split(' ').Last());
            if (hero != optimizeHero)
            {
                return;
            }
            hero.unequipAll();
            foreach (Item item in items)
            {
                if (item.Equipped != null)
                {
                    item.Equipped.unequip(item);
                }
            }
            hero.equip(items);
            updateCurrentGear();
            Dgv_OptimizeResults_RowEnter(null, new DataGridViewCellEventArgs(0, dgv_OptimizeResults.SelectedCells[0].RowIndex));
        }

        private void B_Export_Click(object sender, EventArgs e)
        {
            if (sfd_export.ShowDialog() == DialogResult.OK)
            {
                JObject json = createJson();
                File.WriteAllText(sfd_export.FileName, json.ToString());
                setLastUsedFileName(sfd_export.FileName, false);
            }
        }
        
        //Create a backup of the current item and hero collection in the base directory of the application when the application closes
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (args.Length > 1)
            {
                JObject json = createJson();
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/Backup.json", json.ToString());
            }
            Properties.Settings.Default.OptimizationHiddenColumns.Clear();
            Properties.Settings.Default.OptimizationVisibleSkillColumns.Clear();
            foreach (var item in contextMenuStrip1.Items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem != null)
                {
                    if (contextMenuStrip1SkillMenuItems.Contains(menuItem))
                    {
                        foreach (ToolStripMenuItem childMenuItem in menuItem.DropDownItems)
                        {
                            if (childMenuItem.Checked)
                            {
                                var statName = childMenuItem.Name.Substring(5);//tsmi_*
                                Properties.Settings.Default.OptimizationVisibleSkillColumns.Add(statName);
                            }
                        }
                    }
                    else
                    {
                        if (!menuItem.Checked)
                        {
                            var statName = menuItem.Name.Substring(5);//tsmi_*
                            Properties.Settings.Default.OptimizationHiddenColumns.Add(statName);
                        }
                    }
                }
            }
            Properties.Settings.Default.Save();
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
                                       new JProperty("Awakening", h.Awakening),
                                       new JProperty("Skills", new JArray(from skill in h.Skills
                                                                          select new JObject(
                                                                              new JProperty("Enhance", skill.Enhance))))))),
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
                                       new JProperty("Locked", i.Locked))))
                    //new JProperty("currentItemID", data.CurrentItemID),
                    //new JProperty("currentHeroID", data.CurrentHeroID)
                    );
            return json;
        }

        //Update the current stats of the specified hero when the value of the critbonus changes
        private void Nud_CritBonus_ValueChanged(object sender, EventArgs e)
        {
            updateCurrentGear();
        }

        private void B_UnequipAll_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            hero.unequipAll();
            updateHeroList();
        }

        private void B_LockAll_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            List<Item> gear = hero.getGear();
            foreach (Item item in gear)
            {
                item.Locked = true;
            }
        }

        private void B_UnlockAll_Click(object sender, EventArgs e)
        {
            Hero hero = data.Heroes.Find(x => x.ID == (string)dgv_Heroes["c_HeroID", dgv_Heroes.SelectedCells[0].RowIndex].Value);
            List<Item> gear = hero.getGear();
            foreach (Item item in gear)
            {
                item.Locked = false;
            }
        }

        private void Dgv_Inventory_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (Util.percentageColumns.Contains(e.Column.Name))
            {
                int cell1 = 0;
                int cell2 = 0;
                int.TryParse(e.CellValue1.ToString().Replace("%","").Replace("+", ""), out cell1);
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
            else if (e.Column.Name == "c_Grade")
            {
                string cell1 = e.CellValue1.ToString();
                string cell2 = e.CellValue2.ToString();
                switch (cell1)
                {
                    case "Epic":
                        if (cell2 != "Epic") e.SortResult = -1;
                        else e.SortResult = 0;
                        break;
                    case "Heroic":
                        if (cell2 == "Epic") e.SortResult = 1;
                        else if (cell2 == "Heroic") e.SortResult = 0;
                        else e.SortResult = -1;
                        break;
                    case "Rare":
                        if (cell2 == "Epic" || cell2 == "Heroic") e.SortResult = 1;
                        else if (cell2 == "Rare") e.SortResult = 0;
                        else e.SortResult = -1;
                        break;
                    case "Good":
                        if (cell2 == "Normal") e.SortResult = -1;
                        else if (cell2 == "Good") e.SortResult = 0;
                        else e.SortResult = 1;
                        break;
                    case "Normal":
                        if (cell2 != "Normal") e.SortResult = 1;
                        else e.SortResult = 0;
                        break;
                }
                e.Handled = true;
            }
        }

        private void Tb_Force_TextChanged(object sender, EventArgs e)
        {
            float value = 0;
            TextBox tb = (TextBox)sender;
            if (tb.Text != "" && float.TryParse(tb.Text, out value))
            {
                if (tb.Name.Contains("Min"))
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Min", "").Replace("Force", ""));
                    if (Util.percentStats.Contains(stat))
                    {
                        value = value / 100f;
                    }
                    forceStats[stat] = (value, forceStats[stat].Item2);
                }
                else
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Max", "").Replace("Force", ""));
                    if (Util.percentStats.Contains(stat))
                    {
                        value = value / 100f;
                    }
                    forceStats[stat] = (forceStats[stat].Item1, value);
                }
            }
            else if (tb.Text == "")
            {
                if (tb.Name.Contains("Min"))
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Min", "").Replace("Force", ""));
                    forceStats[stat] = (0, forceStats[stat].Item2);
                }
                else
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Max", "").Replace("Force", ""));
                    forceStats[stat] = (forceStats[stat].Item1, float.MaxValue);
                }
            }
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void tc_InventorySets_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateItemList();
        }

        private void Dgv_Inventory_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                dgv_Inventory.Sort(dgv_Inventory.Columns[e.ColumnIndex], dgv_Inventory.SortOrder == SortOrder.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending);
                dgv_Inventory.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = dgv_Inventory.SortOrder;
            }
            else
            {
                dgv_Inventory.Columns[0].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }

        private void updateCurrentGear()
        {
            dgv_CurrentGear.Rows.Clear();
            if (cb_OptimizeHero.Text != "")
            {
                object[] values = new object[dgv_CurrentGear.ColumnCount];
                Hero hero = data.Heroes.Find(x => x.ID == cb_OptimizeHero.Text.Split().Last());
                SStats heroStats = new SStats(hero.CurrentStats);
                heroStats.Crit += (float)nud_CritBonus.Value / 100f;
                values[0] = (int)hero.CurrentStats[Stats.ATK];
                values[1] = (int)hero.CurrentStats[Stats.SPD];
                values[2] = heroStats.Crit.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[3] = hero.CurrentStats[Stats.CritDmg].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[4] = (int)hero.CurrentStats[Stats.HP];
                values[5] = (int)heroStats.HPpS;
                values[6] = (int)hero.CurrentStats[Stats.DEF];
                values[7] = hero.CurrentStats[Stats.EFF].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                values[8] = hero.CurrentStats[Stats.RES].ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                List<Set> activeSets = hero.activeSets();
                int count = 0;
                if (activeSets.Contains(Set.Unity))
                {
                    foreach (Set set in activeSets)
                    {
                        count += set == Set.Unity ? 1 : 0;
                    }
                }
                values[9] = (5 + (count * 4)) + "%";
                if (activeSets.Count > 0)
                {
                    Bitmap sets = new Bitmap(activeSets.Count * 25, 25, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(sets);
                    for (int i = 0; i < activeSets.Count; i++)
                    {
                        g.DrawImage(Util.ResizeImage((Image)Properties.Resources.ResourceManager.GetObject("set " + activeSets[i].ToString().ToLower().Replace("def", "defense")), 25, 25), i * 25, 0);
                    }
                    values[10] = sets;
                }
                else
                {
                    values[10] = null;
                }
                values[11] = (int)hero.CurrentStats[Stats.EHP];
                values[12] = (int)heroStats.EHPpS;
                values[13] = (int)heroStats.DMG;
                values[14] = (int)heroStats.DMGpS;
                int iCol = 15;
                for (int iSkill = 0; iSkill < 4; iSkill++)
                {
                    bool soulburn = iSkill == 3;
                    var skill = soulburn ? hero.SkillWithSoulburn : hero.Skills[iSkill];
                    float skillDmg = skill.CalcDamage(heroStats, false, soulburn, enemyDef);
                    values[iCol++] = (int)skillDmg;
                    float skillCritDmg = skill.CalcDamage(heroStats, true, soulburn, enemyDef);
                    values[iCol++] = (int)skillCritDmg;
                    float skillAvgDmg = heroStats.CritCapped * skillCritDmg + (1 - heroStats.Crit) * skillDmg;
                    values[iCol++] = (int)skillAvgDmg;
                    float skillAvgDmgSpd = skillAvgDmg * heroStats.SPD / 100;
                    values[iCol++] = (int)skillAvgDmgSpd;
                    //TODO dont calc all dmg if column is hidden (if necessary)
                }
                l_Results.Text = numberOfResults().ToString("#,0");
                dgv_CurrentGear.Rows.Add(values);
            }
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            if (args.Length == 1 && File.Exists("E7 Optimizer Updater.exe"))
            {
                Application.Exit();
            }
            else if (Application.ProductVersion != Util.ver)
            {
                updated updated = new updated();
                updated.ShowDialog();
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove("Version");
                config.AppSettings.Settings.Add("Version", Application.ProductVersion);
                config.Save(ConfigurationSaveMode.Full);
            }
        }

        private void Tb_Optimize_TextChanged(object sender, EventArgs e)
        {
            float value = 0;
            TextBox tb = (TextBox)sender;
            if (tb.Text != "" && float.TryParse(tb.Text, out value))
            {
                if (tb.Name.Contains("Min"))
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Min", ""));
                    if (Util.percentStats.Contains(stat))
                    {
                        value = value / 100f;
                    }
                    filterStats[stat] = (value, filterStats[stat].Item2);
                }
                else
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Max", ""));
                    if (Util.percentStats.Contains(stat))
                    {
                        value = value / 100f;
                    }
                    filterStats[stat] = (filterStats[stat].Item1, value);
                }
            }
            else if (tb.Text == "")
            {
                if (tb.Name.Contains("Min"))
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Min", ""));
                    filterStats[stat] = (0, filterStats[stat].Item2);
                }
                else
                {
                    Stats stat = (Stats)Enum.Parse(typeof(Stats), tb.Name.Replace("tb_Max", ""));
                    filterStats[stat] = (filterStats[stat].Item1, float.MaxValue);
                }
            }
            /*optimizePage = 1;
            dgv_OptimizeResults.Refresh();
            dgv_OptimizeResults.AutoResizeColumns();
            l_Pages.Text = "1 / " + ((combinations.Count + 99) / 100);*/
            
        }

        private static bool checkFilter (SStats stats, Dictionary<Stats, (float, float)> filterStats)
        {
            bool valid = true;
            foreach (KeyValuePair<Stats, (float, float)> stat in filterStats)
            {
                switch (stat.Key)
                {
                    case Stats.ATK:
                        valid = stat.Value.Item1 <= stats.ATK && stat.Value.Item2 >= stats.ATK;
                        break;
                    case Stats.HP:
                        valid = stat.Value.Item1 <= stats.HP && stat.Value.Item2 >= stats.HP;
                        break;
                    case Stats.DEF:
                        valid = stat.Value.Item1 <= stats.DEF && stat.Value.Item2 >= stats.DEF;
                        break;
                    case Stats.SPD:
                        valid = stat.Value.Item1 <= stats.SPD && stat.Value.Item2 >= stats.SPD;
                        break;
                    case Stats.Crit:
                        valid = stat.Value.Item1 <= stats.Crit && stat.Value.Item2 >= stats.Crit;
                        break;
                    case Stats.CritDmg:
                        valid = stat.Value.Item1 <= stats.CritDmg && stat.Value.Item2 >= stats.CritDmg;
                        break;
                    case Stats.EFF:
                        valid = stat.Value.Item1 <= stats.EFF && stat.Value.Item2 >= stats.EFF;
                        break;
                    case Stats.RES:
                        valid = stat.Value.Item1 <= stats.RES && stat.Value.Item2 >= stats.RES;
                        break;
                    case Stats.EHP:
                        valid = stat.Value.Item1 <= stats.EHP && stat.Value.Item2 >= stats.EHP;
                        break;
                    case Stats.HPpS:
                        valid = stat.Value.Item1 <= stats.HPpS && stat.Value.Item2 >= stats.HPpS;
                        break;
                    case Stats.EHPpS:
                        valid = stat.Value.Item1 <= stats.EHPpS && stat.Value.Item2 >= stats.EHPpS;
                        break;
                    case Stats.DMG:
                        valid = stat.Value.Item1 <= stats.DMG && stat.Value.Item2 >= stats.DMG;
                        break;
                    case Stats.DMGpS:
                        valid = stat.Value.Item1 <= stats.DMGpS && stat.Value.Item2 >= stats.DMGpS;
                        break;
                }
                if (!valid)
                {
                    return false;
                }
            }
            return valid;
        }

        private void B_FilterResults_Click(object sender, EventArgs e)
        {
            filteredCombinations.Clear();
            for (int i = 0; i < combinations.Count; i++)
            {
                if (checkFilter(combinations[i].Item2, filterStats))
                {
                    filteredCombinations.Add(i);
                }
            }
            dgv_OptimizeResults.RowCount = Math.Min(filteredCombinations.Count, 100);
            dgv_OptimizeResults.Refresh();
            dgv_OptimizeResults.AutoResizeColumns();
            l_Pages.Text = "1 / " + ((filteredCombinations.Count + 99) / 100);
        }

        private void B_ImportAppend_Click(object sender, EventArgs e)
        {
            if (rb_import_this.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    import(ofd_import.FileName, false, true);
                }
            }
            else if (rb_import_web.Checked)
            {
                if (ofd_import.ShowDialog() == DialogResult.OK)
                {
                    import(ofd_import.FileName, true, true);
                }
            }
            else
            {
                MessageBox.Show("Please select the source to import from!");
            }
        }

        private void Nud_EnhanceFocus_ValueChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void B_CancelOptimize_Click(object sender, EventArgs e)
        {
            tokenSource.Cancel();
        }

        private void cb1_SpeedTuner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb1_SpeedTuner.Text != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb1_SpeedTuner.Text.Split().Last());
                pb1_SpeedTuner.Image = hero.Portrait;
                tb1_SpeedTunerGear.Text = ((int)hero.CurrentStats[Stats.SPD]).ToString();
                int spd = int.Parse(tb1_SpeedTunerGear.Text) + (int)nud1_SpeedTunerImprint.Value;
                tb1_SpeedTuner_ResultMax.Text = (spd * 1.05).ToString();
                tb1_SpeedTuner_Result.Text = spd.ToString();
                tb1_SpeedTuner_ResultMin.Text = (spd * 0.95).ToString();
            }
            checkIfTuned();
        }

        private void checkIfTuned()
        {
            float spd1min = tb1_SpeedTuner_ResultMin.Text != "" ? float.Parse(tb1_SpeedTuner_ResultMin.Text) : 0;
            float spd2max = tb2_SpeedTuner_ResultMax.Text != "" ? float.Parse(tb2_SpeedTuner_ResultMax.Text) : 0;
            float spd2min = tb2_SpeedTuner_ResultMin.Text != "" ? float.Parse(tb2_SpeedTuner_ResultMin.Text) : 0;
            float spd3max = tb3_SpeedTuner_ResultMax.Text != "" ? float.Parse(tb3_SpeedTuner_ResultMax.Text) : 0;
            float spd3min = tb3_SpeedTuner_ResultMin.Text != "" ? float.Parse(tb3_SpeedTuner_ResultMin.Text) : 0;
            float spd4max = tb4_SpeedTuner_ResultMax.Text != "" ? float.Parse(tb4_SpeedTuner_ResultMax.Text) : 0;
            float spd4min = tb4_SpeedTuner_ResultMin.Text != "" ? float.Parse(tb4_SpeedTuner_ResultMin.Text) : 0;
            if (spd2max > spd1min)
            {
                tb2_SpeedTuner_ResultMax.BackColor = Color.Red;
            }
            else
            {
                tb2_SpeedTuner_ResultMax.BackColor = SystemColors.Control;
            }
            if (spd3max > spd2min)
            {
                tb3_SpeedTuner_ResultMax.BackColor = Color.Red;
            }
            else
            {
                tb3_SpeedTuner_ResultMax.BackColor = SystemColors.Control;
            }
            if (spd4max > spd3min)
            {
                tb4_SpeedTuner_ResultMax.BackColor = Color.Red;
            }
            else
            {
                tb4_SpeedTuner_ResultMax.BackColor = SystemColors.Control;
            }
        }

        private void Cb2_SpeedTuner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb2_SpeedTuner.Text != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb2_SpeedTuner.Text.Split().Last());
                pb2_SpeedTuner.Image = hero.Portrait;
                tb2_SpeedTunerGear.Text = ((int)hero.CurrentStats[Stats.SPD]).ToString();
                int spd = int.Parse(tb2_SpeedTunerGear.Text) + (int)nud2_SpeedTunerImprint.Value;
                tb2_SpeedTuner_ResultMax.Text = (spd * 1.05).ToString();
                tb2_SpeedTuner_Result.Text = spd.ToString();
                tb2_SpeedTuner_ResultMin.Text = (spd * 0.95).ToString();
            }
            checkIfTuned();
        }

        private void Cb3_SpeedTuner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb3_SpeedTuner.Text != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb3_SpeedTuner.Text.Split().Last());
                pb3_SpeedTuner.Image = hero.Portrait;
                tb3_SpeedTunerGear.Text = ((int)hero.CurrentStats[Stats.SPD]).ToString();
                int spd = int.Parse(tb3_SpeedTunerGear.Text) + (int)nud3_SpeedTunerImprint.Value;
                tb3_SpeedTuner_ResultMax.Text = (spd * 1.05).ToString();
                tb3_SpeedTuner_Result.Text = spd.ToString();
                tb3_SpeedTuner_ResultMin.Text = (spd * 0.95).ToString();
            }
            checkIfTuned();
        }

        private void Cb4_SpeedTuner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb4_SpeedTuner.Text != "")
            {
                Hero hero = data.Heroes.Find(x => x.ID == cb4_SpeedTuner.Text.Split().Last());
                pb4_SpeedTuner.Image = hero.Portrait;
                tb4_SpeedTunerGear.Text = ((int)hero.CurrentStats[Stats.SPD]).ToString();
                int spd = int.Parse(tb4_SpeedTunerGear.Text) + (int)nud4_SpeedTunerImprint.Value;
                tb4_SpeedTuner_ResultMax.Text = (spd * 1.05).ToString();
                tb4_SpeedTuner_Result.Text = spd.ToString();
                tb4_SpeedTuner_ResultMin.Text = (spd * 0.95).ToString();
            }
            checkIfTuned();
        }

        private void Nud1_SpeedTunerImprint_ValueChanged(object sender, EventArgs e)
        {
            cb1_SpeedTuner_SelectedIndexChanged(null, null);
        }

        private void Nud2_SpeedTunerImprint_ValueChanged(object sender, EventArgs e)
        {
            Cb2_SpeedTuner_SelectedIndexChanged(null, null);
        }

        private void Nud3_SpeedTunerImprint_ValueChanged(object sender, EventArgs e)
        {
            Cb3_SpeedTuner_SelectedIndexChanged(null, null);
        }

        private void Nud4_SpeedTunerImprint_ValueChanged(object sender, EventArgs e)
        {
            Cb4_SpeedTuner_SelectedIndexChanged(null, null);
        }

        private void Cb_LimitResults_CheckedChanged(object sender, EventArgs e)
        {
            nud_LimitResults.Enabled = cb_LimitResults.Checked;
            Properties.Settings.Default.LimitResults = limitResults = cb_LimitResults.Checked;
        }

        private void Nud_LimitResults_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LimitResultsNum = limitResultsNum = (int)nud_LimitResults.Value;
        }
        
        private void b_RightSideFocus_Click(TextBox tb, string[] stats)
        {
            MultiSelectForm multiSelect = new MultiSelectForm(stats);
            multiSelect.Location = tb.PointToScreen(new Point(0, 0 + tb.Height));
            var result = multiSelect.ShowDialog();
            if (result == DialogResult.OK)
            {
                List<string> selectedStats = new List<string>(multiSelect.SelectedItems.Count);
                foreach (var item in multiSelect.SelectedItems)
                {
                    selectedStats.Add(item.ToString());
                }
                tb.Text = string.Join(" ", selectedStats);
            }
        }

        private void B_NecklaceFocus(object sender, EventArgs e)
        {
            b_RightSideFocus_Click(tb_NecklaceFocus, new string[] { "ATK%", "ATK", "Crit", "CritDmg", "HP%", "HP", "DEF%", "DEF" });
        }

        private void B_RingFocus_Click(object sender, EventArgs e)
        {
            b_RightSideFocus_Click(tb_RingFocus, new string[] { "ATK%", "ATK", "HP%", "HP", "DEF%", "DEF", "EFF", "RES" });
        }

        private void B_BootsFocus_Click(object sender, EventArgs e)
        {
            b_RightSideFocus_Click(tb_BootsFocus, new string[] { "ATK%", "ATK", "SPD", "HP%", "HP", "DEF%", "DEF"});
        }

        private void Tb_NecklaceFocus_TextChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void Tb_RingFocus_TextChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void Tb_BootsFocus_TextChanged(object sender, EventArgs e)
        {
            l_Results.Text = numberOfResults().ToString("#,0");
        }

        private void Nud_CritBonus_Leave(object sender, EventArgs e)
        {
            nud_CritBonus.Text = nud_CritBonus.Value.ToString();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.ImportOnLoad && File.Exists(Properties.Settings.Default.LastUsedFileName))
            {
                import(Properties.Settings.Default.LastUsedFileName, Properties.Settings.Default.LastUsedFileNameWeb);
            }
        }

        private void Cb_ImportOnLoad_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ImportOnLoad = cb_ImportOnLoad.Checked;
        }

        private void Cb_CacheWeb_CheckedChanged(object sender, EventArgs e)
        {
            useCache = cb_CacheWeb.Checked;
            b_ClearCache.Enabled = useCache;
        }

        private void B_ClearCache(object sender, EventArgs e)
        {
            var files = Directory.GetFiles(Properties.Settings.Default.CacheDirectory, "db.*");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        
        private void ContextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var item = (ToolStripMenuItem)e.ClickedItem;
            if (item.Name.Length == 7 && item.Name.StartsWith("tsmi_S"))//Parent Skill X menu item
            {
                bool checkedNew = item.CheckState == CheckState.Unchecked;
                foreach (ToolStripMenuItem childItem in item.DropDownItems)
                {
                    var statName = childItem.Name.Substring(5);//tsmi_*
                    dgv_CurrentGear.Columns["c_" + statName + "_Current"].Visible = checkedNew;
                    dgv_OptimizeResults.Columns["c_" + statName + "_Results"].Visible = checkedNew;
                    childItem.Checked = checkedNew;
                }
                contextMenuStrip1.Hide();
            }
            else
            {
                var checkedNew = !item.Checked;
                var statName = item.Name.Substring(5);//tsmi_*
                dgv_CurrentGear.Columns["c_" + statName + "_Current"].Visible = checkedNew;
                dgv_OptimizeResults.Columns["c_" + statName + "_Results"].Visible = checkedNew;
            }
        }

        private void setCheckState(ToolStripMenuItem menuItem)
        {
            int itemsChecked = menuItem.DropDownItems.Cast<ToolStripMenuItem>().Count(i => i.Checked);
            if (itemsChecked == menuItem.DropDownItems.Count)
            {
                menuItem.CheckState = CheckState.Checked;
            }
            else if (itemsChecked == 0)
            {
                menuItem.CheckState = CheckState.Unchecked;
            }
            else
            {
                menuItem.CheckState = CheckState.Indeterminate;
            }
        }

        private void ContextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            foreach (var menuItem in contextMenuStrip1SkillMenuItems)
            {
                setCheckState(menuItem);
            }
        }

        private void Nud_EnemyDef_ValueChanged(object sender, EventArgs e)
        {
            enemyDef = Properties.Settings.Default.EnemyDefence = (int)nud_EnemyDef.Value;
            updateCurrentGear();
            dgv_OptimizeResults.Refresh();
        }
    }
}
