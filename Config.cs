using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FSEconomy.Properties;

namespace FSEconomy
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
            setFormItems();
        }

        private void setFormItems()
        {
            radioButton1.Checked = Settings.Default.localComputer;
            radioButton2.Checked = !Settings.Default.localComputer;
            setHostPortEnabled();
        }

        public bool isLocalComputer()
        {
            return radioButton1.Checked;
        }


        private void setHostPortEnabled()
        {
            hostname.Enabled = port.Enabled = radioButton2.Checked;
        }


        private void CheckedChanged(object sender, EventArgs e)
        {
            setHostPortEnabled();
        }

        private void Config_Activated(object sender, EventArgs e)
        {
            setFormItems();
        }

    }
}