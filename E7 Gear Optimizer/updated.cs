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
    public partial class updated : Form
    {
        public updated()
        {
            InitializeComponent();
        }

        private void Updated_Shown(object sender, EventArgs e)
        {
            label1.Text = "Application was updated from " + Util.ver.Substring(0, Util.ver.Length - 2) + "  to " + Application.ProductVersion.Substring(0, Application.ProductVersion.Length - 2) + "!";
            linkLabel1.Text = Util.GitHubUrl;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(Util.GitHubUrl);
        }
    }
}
