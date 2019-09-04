using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;

namespace E7_Gear_Optimizer
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public partial class ItemStats : UserControl
    {
        public ItemStats()
        {
            InitializeComponent();
        }

        private Item item;

        public Item Item
        {
            get => item;
            set
            {
                item = value;
                if (item != null)
                {
                    l_ItemGrade.Text = item.Grade.ToString() + " Weapon";
                    l_ItemGrade.ForeColor = Util.gradeColors[item.Grade];
                    l_ItemIlvl.Text = item.ILvl.ToString();
                    l_ItemEnhance.Text = "+" + item.Enhance.ToString();
                    l_ItemMain.Text = Util.statStrings[item.Main.Name];
                    l_ItemMainStat.Text = ((int)item.Main.Value).ToString();
                    l_ItemSet.Text = item.Set.ToString().Replace("Crit", "Critical").Replace("Def", "Defense") + " Set";
                    pb_ItemSet.Image = (Image)Properties.Resources.ResourceManager.GetObject("set " + item.Set.ToString().ToLower().Replace("def", "defense"));
                    for (int i = 0; i < 4; i++)
                    {
                        //if (i < item.SubStats.Length)
                        //{
                        //    ((Label)tb_.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "", true)[0]).Text = Util.statStrings[item.SubStats[i].Name];
                        //    ((Label)tb_.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = Util.percentStats.Contains(item.SubStats[i].Name) ? item.SubStats[i].Value.ToString("P0", CultureInfo.CreateSpecificCulture("en-US")) : item.SubStats[i].Value.ToString();
                        //}
                        //else
                        //{
                        //    ((Label)tb_.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "", true)[0]).Text = "";
                        //    ((Label)tb_.Controls.Find("l_" + item.Type.ToString() + "Sub" + (i + 1) + "Stat", true)[0]).Text = "";
                        //}
                    }
                    //l_ItemEquipped.Text = item.Equipped != null ? item.Equipped.Name + " " + item.Equipped.ID : "";
                    pb_Image.Image = item.Equipped?.Portrait;
                }
                else
                {
                    l_ItemGrade.Text = "";
                    l_ItemIlvl.Text = "";
                    l_ItemEnhance.Text = "";
                    l_ItemMain.Text = "";
                    l_ItemMainStat.Text = "";
                    l_ItemSet.Text = "";
                    //l_ItemEquipped.Text = "";
                    pb_Image.Image = Util.error;
                    pb_ItemSet.Image = Util.error;
                    for (int i = 0; i < 4; i++)
                    {
                        //((Label)tb_.Controls.Find("l_ItemSub" + (i + 1) + "", true)[0]).Text = "";
                        //((Label)tb_.Controls.Find("l_ItemSub" + (i + 1) + "Stat", true)[0]).Text = "";
                    }
                }
            }
        }
    }
}
