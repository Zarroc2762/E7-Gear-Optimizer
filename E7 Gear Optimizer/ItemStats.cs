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
        }

        private Image image = null;
        private Item item = null;

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
                    l_ItemMainStat.Text = ((int)item.Main.Value).ToString();
                    l_ItemSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < item.SubStats.Length)
                        {
                            ((Label)Controls.Find("l_ItemSub" + (i + 1) + "", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                            ((Label)Controls.Find("l_ItemSub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        }
                        else
                        {
                            ((Label)Controls.Find("l_ItemSub" + (i + 1) + "", true)[0]).Text = "";
                            ((Label)Controls.Find("l_ItemSub" + (i + 1) + "Stat", true)[0]).Text = "";
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
                        pb_Image.Image = Util.error;
                    }
                    pb_ItemSet.Image = Util.error;
                }
            }
        }

        /// <summary>
        /// Get or set image that is used instead of default item type image
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
