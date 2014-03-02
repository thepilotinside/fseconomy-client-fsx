using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using FSEconomy.Properties;


namespace FSEconomy
{
    public partial class Main : Form
    {
        SimConnect connection = null;
        const int WM_USER_SIMCONNECT = 0x0402;
        StateManager stateManager;

        public Main()
        {
            InitializeComponent();
            stateManager = new StateManager(new updateScreen(updateAircraftPane), new updateScreen(updateProbeData), this.dataGridView1);
            kickWebbrowser();
        }

        private void kickWebbrowser()
        {
            String path = Settings.Default.server +  "/autologon?user=" +
                Settings.Default.username + "&password=" +
                Settings.Default.password + "&offset=" +
                TimeZone.CurrentTimeZone.GetUtcOffset(new DateTime()).Hours;
            Uri logon = new Uri(path);
            webBrowser1.Url = logon;
            webRequest.getInstance().accountCheck();
            updateStatusbar();
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg != WM_USER_SIMCONNECT)
                base.DefWndProc(ref m);
            else if (connection != null)
                connection.ReceiveMessage();
        }

        void updateStatusbar()
        {
            webRequest webRequest = webRequest.getInstance();
            this.toolStripInternetOK.Image = webRequest.isAlive ? Properties.Resources.world : Properties.Resources.world_gs;
            this.toolStripInternetOK.Text = webRequest.isAlive ? "Connected" : "Not Connected";

        }

        void updateProbeData()
        {
            Aircraft currentAircraft = stateManager.getAircraft();

            String airport = "this airport";
            if (currentAircraft != null && currentAircraft.latestProbeData != null &&
                currentAircraft.latestProbeData.currentAirport != null)
                airport = currentAircraft.latestProbeData.currentAirport.name;

            this.aircraftValue.Text = currentAircraft.aircraftDisplayName;
            this.aircraftPicture.Image = currentAircraft.aircraftInDatabase ?
                FSEconomy.Properties.Resources.accept : FSEconomy.Properties.Resources.exclamation;
            this.availablePicture.Image = (currentAircraft.latestProbeData != null && currentAircraft.latestProbeData.availableAtCurrentAirport) ?
                FSEconomy.Properties.Resources.accept : FSEconomy.Properties.Resources.exclamation;
            this.availableLable.Text = ((currentAircraft.latestProbeData != null && currentAircraft.latestProbeData.availableAtCurrentAirport) ?
                "The aircraft is available at " :
                "The aircraft is not available at ") + airport;

            this.alternativeAircraftGrid.Rows.Clear();
            this.altAirportGrid.Rows.Clear();

            if (currentAircraft != null && currentAircraft.latestProbeData != null)
            {
                foreach (webRequest.altAircraft altAircraft in currentAircraft.latestProbeData.altAircraft)
                    this.alternativeAircraftGrid.Rows.Add(altAircraft.model, altAircraft.registration);

                foreach (webRequest.closeAirport ap in currentAircraft.latestProbeData.airports)
                    this.altAirportGrid.Rows.Add(ap.icao, String.Format("{0:0.00} NM", ap.distance), (int)ap.bearing);
            }
        }

        void updateAircraftPane()
        {
            Aircraft currentAircraft = stateManager.getAircraft();

            bool startReady = webRequest.getInstance().isAlive && connection != null;
            this.startFlightToolStripMenuItem.Enabled = startReady && !stateManager.flightActive;
            this.cancelFlightToolStripMenuItem.Enabled = startReady && stateManager.flightActive;
            this.requestAircraftToolStripMenuItem.Enabled = currentAircraft != null && !currentAircraft.aircraftInDatabase;

            updateStatusbar();
            stateManager.updateProgressBar(this.leaseTime);
            if (stateManager.currentFlight != null)
            {
                Flight currentFlight = stateManager.currentFlight;
                TimeSpan leaseLeft = currentFlight.leaseTimeLeft();
                this.rentalTimeLeftValue.Text = String.Format("{0:00}:{1:00}:{2:00}", leaseLeft.Hours, leaseLeft.Minutes, leaseLeft.Seconds);
                this.rentalCostValue.Text = String.Format("${0:0.00}", currentFlight.runningRentalCost());
                this.fuelCostValue.Text = String.Format("${0:0.00}", currentFlight.runningFuelCost());
                this.flightInfoBox.Visible = true;
            }
            else
            {
                this.flightInfoBox.Visible = false;
            }
        }

        void onRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION exception)
        {
            String message = "Exception number " + exception.dwException + "; packetId: " + exception.dwID + "; index=" + exception.dwIndex;
            MessageBox.Show(message); 
            
        }
        void onRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            this.fsStatusLabel.Text = "Connected";
            this.fsStatusLabel.Image = FSEconomy.Properties.Resources.icon_airplane_green;
            connection.SubscribeToSystemEvent(EVENTS.CRASH, "Crashed");
            connection.SubscribeToSystemEvent(EVENTS.JUMP, "PositionChanged");
            connection.SubscribeToSystemEvent(EVENTS.SIMSTATE, "Sim");
            Aircraft.SetDataDefinition(connection);
            connection.RequestDataOnSimObject(DATA_REQUESTID.AIRCRAFT_INFO, DATA_DEF.AIRCRAFT_INFO, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            stateManager.connection = connection;
            connection.MenuAddItem("FS Economy", EVENTS.MENU1, 0);
            connection.MenuAddSubItem(EVENTS.MENU1, "Start flight", EVENTS.MENU2, 0);
        }
        void onRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            this.fsStatusLabel.Text = "Not connected";
            this.fsStatusLabel.Image = FSEconomy.Properties.Resources.icon_airplane;
            connection.Dispose();
            connection = null;
            stateManager.connection = null;
        }

        private void worker_createConnection(object sender, DoWorkEventArgs e)
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;    // Turn off threading exceptions from SimConnect
                fsStatusLabel.Text = "Connecting";
                connection = new SimConnect("FS Economy", this.Handle, WM_USER_SIMCONNECT, null, 0);
                connection.OnRecvEvent += new SimConnect.RecvEventEventHandler(stateManager.onRecvEvent);
                connection.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(stateManager.onRecvSimobjectData);
                connection.OnRecvException += new SimConnect.RecvExceptionEventHandler(onRecvException);
                connection.OnRecvOpen += new SimConnect.RecvOpenEventHandler(onRecvOpen);
                connection.OnRecvQuit += new SimConnect.RecvQuitEventHandler(onRecvQuit);
            }
            catch (COMException)
            {
                fsStatusLabel.Text = "Not connected";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (connection != null)
                return;
            if (connectionWorker.IsBusy)
                return;
            connectionWorker.RunWorkerAsync();

        }

        private void writeSimconnectConfig()
        {
            if (Settings.Default.localComputer)
            {
                File.Delete("SimConnect.cfg");
            }
            else
            {
                using (FileStream configFile = File.OpenWrite("SimConnect.cfg"))
                {
                    configFile.SetLength(0);
                    StreamWriter writer = new StreamWriter(configFile);
                    writer.WriteLine("[SimConnect]");
                    writer.WriteLine("Protocol=IPv4");
                    writer.WriteLine("Address=" + Settings.Default.fsxHostname);
                    writer.WriteLine("Port=" + Settings.Default.fsxPort);
                    writer.Close();
                    configFile.Close();
                }
            }
        }

        private void configToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            String oldHostname = Settings.Default.fsxHostname;
            String oldPort = Settings.Default.fsxPort;
            String oldUsername = Settings.Default.username;
            String oldPassword = Settings.Default.password;
            bool wasLocalComputer = Settings.Default.localComputer;
            using (Config configDialog = new Config())
            {
                DialogResult result = configDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Settings.Default.localComputer = configDialog.isLocalComputer();
                    Settings.Default.Save();

                    if (oldUsername != Settings.Default.username || oldPassword != Settings.Default.password)
                        kickWebbrowser();

                    if (oldHostname == Settings.Default.fsxHostname &&
                        oldPort == Settings.Default.fsxPort && wasLocalComputer == Settings.Default.localComputer)
                        return;
                    writeSimconnectConfig();
                    Application.Restart();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();

        }

        private void startFlightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stateManager.startFlight();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void cancelFlightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stateManager.cancelFlight("");
        }

        private void requestAircraftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            webRequest.getInstance().requestAircraft(stateManager.getAircraft());
            MessageBox.Show("A request was added to the database.", "FS Economy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}