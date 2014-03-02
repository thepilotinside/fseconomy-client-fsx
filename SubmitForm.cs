using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace FSEconomy
{
    public partial class SubmitForm : Form
    {
        public SubmitForm(string submitParameters)
        {
            InitializeComponent();
            backgroundWorker1.RunWorkerAsync(submitParameters);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            String submitResult = null;
            do
            {
                try
                {
                    submitResult = webRequest.getInstance().endFlight((string)e.Argument);
                    submitResult = submitResult.Replace('|', '\n');
                }
                catch (FSEconomyException)
                {
                    if (!backgroundWorker1.CancellationPending)
                        Thread.Sleep(5000);
                }
            } while (submitResult == null && !backgroundWorker1.CancellationPending);
            if (submitResult == null && backgroundWorker1.CancellationPending)
                e.Cancel = true;
            e.Result = submitResult;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && Properties.Settings.Default.showFinishedFlight)
            {
                button1.Text = "Ok";
                label1.Text = (String)e.Result;
            }
            else
            {
                this.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
                button1.Enabled = false;
            }
            else
                this.Close();
        }

    }
}