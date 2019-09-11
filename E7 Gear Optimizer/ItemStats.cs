using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace E7_Gear_Optimizer
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public partial class ItemStats : UserControl
    {
        public ItemStats()
        {
            InitializeComponent();
            substatsLabels = new[]
            {
                (l_ItemSub1, l_ItemSub1Stat),
                (l_ItemSub2, l_ItemSub2Stat),
                (l_ItemSub3, l_ItemSub3Stat),
                (l_ItemSub4, l_ItemSub4Stat)
            };
        }

        private Image image = null;
        private Item item = null;
        /// <summary>
        /// Array of Labels for substats names and values, to use instead of Controls.Find()
        /// </summary>
        private (Label, Label)[] substatsLabels;

        /// <summary>
        /// Gets or sets the Hero against whom flat stats' percent values will be calculated
        /// </summary>
        public Hero Hero { get; set; } = null;

        /// <summary>
        /// Gets or sets the Item which stats will be displayed by this control
        /// </summary>
        public Item Item
        {
            get => item;
            set
            {
                item = value;
                if (item != null)
                {
                    l_ItemGrade.Text = item.Grade.ToString() + " " + item.Type.ToString();
                    l_ItemGrade.ForeColor = Util.gradeColors[item.Grade];
                    l_ItemIlvl.Text = item.ILvl.ToString();
                    l_ItemEnhance.Text = "+" + item.Enhance.ToString();
                    l_ItemMain.Text = Util.statStrings[item.Main.Name];
                    l_ItemMainStat.Text = getStatValueString(item.Main);
                    l_ItemSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            substatsLabels[i].Item1.Text = Util.statStrings[item.SubStats[i].Name];
                            substatsLabels[i].Item2.Text = getStatValueString(item.SubStats[i]);
                        }
                        else
                        {
                            substatsLabels[i].Item1.Text = "";
                            substatsLabels[i].Item2.Text = "";
                        }
                    }
                    if (image == null)
                    {
                        pb_Image.Image = (Image)Properties.Resources.ResourceManager.GetObject(item.Type.ToString().ToLower());
                    }
                    pb_ItemSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                }
                else
                {
                    l_ItemGrade.Text = "";
                    l_ItemIlvl.Text = "";
                    l_ItemEnhance.Text = "";
                    l_ItemMain.Text = "";
                    l_ItemMainStat.Text = "";
                    l_ItemSet.Text = "";
                    l_ItemSub1.Text = l_ItemSub1Stat.Text = "";
                    l_ItemSub2.Text = l_ItemSub2Stat.Text = "";
                    l_ItemSub3.Text = l_ItemSub3Stat.Text = "";
                    l_ItemSub4.Text = l_ItemSub4Stat.Text = "";
                    if (image == null)
                    {
                        pb_Image.Image = null;
                    }
                    pb_ItemSet.Image = null;
                }
            }
        }

        private string getStatValueString(Stat stat)
        {
            string s;
            if (Util.percentStats.Contains(stat.Name))
            {
                s = stat.Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
            }
            else if ((item.Equipped == null && Hero == null) || stat.Name == Stats.SPD)
            {
                s = stat.Value.ToString();
            }
            else
            {
                s = stat.Value.ToString() + " (" + (stat.Value / (Hero ?? item.Equipped).BaseStats[stat.Name]).ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) + ')';
            }
            return s;
        }

        /// <summary>
        /// Gets or sets the Image that is used instead of default item type image
        /// </summary>
        public Image Image
        {
            get => image;
            set
            {
                pb_Image.Image = image = value;
            }
        }
    }
}
