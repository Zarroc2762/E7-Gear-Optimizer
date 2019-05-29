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
    public partial class Import : Form
    {

        public bool result;
        public Data data;
        public string fileName;
        public bool web;

        public Import()
        {
            InitializeComponent();
        }

        public Import(Data data, string fileName, bool web)
        {
            InitializeComponent();
            this.data = data;
            this.fileName = fileName;
            this.web = web;
        }

        private async void Import_Shown(object sender, EventArgs e)
        {
            Progress<int> progress = new Progress<int>(x => progressBar1.Value = x);
            (bool, int, int) results;
            if (web)
            {
                results = await Task.Run(() => data.importFromWeb(fileName, progress));
            }
            else
            {
                results = await Task.Run(() => data.importFromThis(fileName, progress));
            }
            result = results.Item1;
            label4.Text = results.Item2.ToString();
            label5.Text = results.Item3.ToString();
            button1.Enabled = true;
        }
    }
}
