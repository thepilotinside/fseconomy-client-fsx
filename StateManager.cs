using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.FlightSimulator.SimConnect;
using System.Windows.Forms;

namespace FSEconomy
{
    enum EVENTS
    {
        CRASH, JUMP, SIMSTATE, MENU1, MENU2, MENU3
    };
    delegate void updateScreen();

    class StateManager
    {
        enum FSECONOMY_STATES { IDLE, STARTED_GROUND, AIRBORNE, LANDED, FINISHED }

        DataGridView cargoView;
        public Flight currentFlight = null;
        Aircraft currentAircraft = null;
        FSECONOMY_STATES currentState = FSECONOMY_STATES.IDLE;
        event updateScreen updateFunction, updateProbedataFunction;
        public SimConnect connection = null;

        public bool flightActive
        {
            get { return currentFlight != null; } 
        }
        public StateManager(updateScreen func, updateScreen updateProbedataFunction, DataGridView cargoView)
        {
            this.updateFunction = func;
            this.updateProbedataFunction = updateProbedataFunction;
            this.cargoView = cargoView;
        }

        public Aircraft getAircraft()
        {
            return currentAircraft;
        }

        private void eventAircraftChanged(SimConnect simConnect)
        {
            if (currentState == FSECONOMY_STATES.IDLE)
                currentAircraft = new Aircraft(simConnect);
            else
                cancelFlight("Aircraft was changed.");
        }
        private void eventJump()
        {
            if (currentState == FSECONOMY_STATES.IDLE)
            {
                if (currentAircraft != null)
                {
                    currentAircraft.invalidatePosition();
                }
            }
            else
                cancelFlight("Aircraft was repositioned.");
        }
        private void eventCrash()
        {
            if (currentState != FSECONOMY_STATES.IDLE)
                cancelFlight("Aircraft crashed.");
        }

        public void updateProgressBar(System.Windows.Forms.ProgressBar bar)
        {
            if (currentFlight != null)
                currentFlight.updateProgressBar(bar);
            else
            {
                bar.Maximum = bar.Value = 100;
            }
        }

        /*
         * Data has been received from FS
         */
        public void onRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch ((DATA_REQUESTID)data.dwRequestID)
            {
                case DATA_REQUESTID.AIRCRAFT_SPECS:
                    {
                        if (currentAircraft != null)
                        {
                            currentAircraft.receiveData(data);
                        }
                        break;
                    }
                case DATA_REQUESTID.AIRCRAFT_INFO:
                    {
                        aircraft_info latestInfo = (aircraft_info)data.dwData[0];
                        if (currentAircraft != null)
                        {
                            if (currentAircraft.specifications.title != latestInfo.title)
                                eventAircraftChanged(sender);
                            else
                            {
                                currentAircraft.latestInfo = latestInfo;
                                if (!currentAircraft.probeDataValid)
                                    updateProbedataFunction();
                                updateFunction();
                            }
                        }
                        break;
                    }
                case DATA_REQUESTID.FLIGHT_DATA:
                    {
                        if (currentFlight != null)
                        {
                            currentFlight.receiveData(data);
                            checkCurrentState(currentFlight.latestData);
                        }
                        break;
                    }
                case DATA_REQUESTID.FLIGHT_FINISH_DATA:
                    {
                        finishFlight(data);
                        break;
                    }

            }

        }

        private void switchToState(FSECONOMY_STATES newState)
        {            
            currentState = newState;
            if (currentState == FSECONOMY_STATES.FINISHED)
            {
                if (currentFlight != null)
                    currentFlight.requestFinishData(connection);
            }
        }
        private void checkCurrentState(flightData data)
        {
            switch (currentState)
            {
                case FSECONOMY_STATES.IDLE:
                    break;
                case FSECONOMY_STATES.STARTED_GROUND:
                    if (data.onground == 0)
                        switchToState(FSECONOMY_STATES.AIRBORNE);
                    break;
                case FSECONOMY_STATES.AIRBORNE:
                    if (data.onground != 0 && data.groundVelocity < 20)
                        switchToState(FSECONOMY_STATES.LANDED);
                    break;
                case FSECONOMY_STATES.LANDED:
                    if (data.onground != 0 && data.parkingBreak != 0 && data.wheelRpm == 0)
                        switchToState(FSECONOMY_STATES.FINISHED);
                    break;
            }
        }

       /*
        * A system event has been received from FS
        */
        public void onRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            switch ((EVENTS)recEvent.uEventID)
            {
                case EVENTS.SIMSTATE:
                    {
                        /*
                         * Simulator state has changed. If the simulator is running and we
                         * haven't yet loaded aircraft data, do so now
                         */

                        if (recEvent.dwData == 1 && currentAircraft == null)
                            currentAircraft = new Aircraft(sender);
                        break;
                    }
                case EVENTS.JUMP:
                    {
                        /*
                         * A jump has been detected.
                         */
                        eventJump();
                        break;
                    }
                case EVENTS.CRASH:
                    {
                        /*
                         * A crash was detected.
                         */
                        eventCrash();
                        break;
                    }
                case EVENTS.MENU2:
                    {
                        /*
                         * Start flight was requested from FS
                         */
                        startFlight();
                        break;
                    }
                case EVENTS.MENU3:
                    {
                        /*
                         * Cancel flight was requested from FS
                         */
                        cancelFlight(null);
                        break;
                    }

            }
        }


        void disposeFlight()
        {
            if (currentFlight != null)
            {
                currentFlight.stop(connection);
                currentFlight = null;
            }
            cargoView.Rows.Clear();
            currentState = FSECONOMY_STATES.IDLE;
            connection.MenuDeleteSubItem(EVENTS.MENU1, EVENTS.MENU3);
            connection.MenuAddSubItem(EVENTS.MENU1, "Start flight", EVENTS.MENU2, 0);          
        }

        /*
         * An event has occured that forces this flight to stop.
         */
        public void cancelFlight(String reason)
        {
            if (reason != null)
            {
                reason = reason == "" ? "." : (" : " + reason);
                MessageBox.Show("The flight was cancelled" + reason, "Flight Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            webRequest.getInstance().cancelFlight();
            disposeFlight();
        }

        void showError(String text)
        {
            MessageBox.Show(text, "FS Economy", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /*
         * The user requests a flight start
         */
        public void startFlight()
        {
            try
            {
                if (currentAircraft == null || !currentAircraft.infoAvailable)
                    throw new FSEconomyException("No aircraft information received from Flight Simulator yet.");

                currentFlight = webRequest.getInstance().startFlight(currentAircraft.latestInfo.title, currentAircraft.latestInfo.lat, currentAircraft.latestInfo.lon, currentAircraft);
                currentFlight.start(connection);
                foreach (webRequest.assignment assignment in currentFlight.getAssignmentList())
                {
                    cargoView.Rows.Add(assignment.from, assignment.to, assignment.cargo, assignment.comment);
                }
                switchToState(FSECONOMY_STATES.STARTED_GROUND);
                connection.MenuDeleteSubItem(EVENTS.MENU1, EVENTS.MENU2);
                connection.MenuAddSubItem(EVENTS.MENU1, "Cancel flight", EVENTS.MENU3, 0);
            } catch (FSEconomyException e)
            {
                showError(e.Message);
            }
        }

        void finishFlight(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            fuelStruct fuel = (fuelStruct)data.dwData[0];
            String parameters = currentFlight.getUrlParameters();
            parameters += String.Format("&lat={0:f}&lon={1:f}", currentAircraft.latestInfo.lat, currentAircraft.latestInfo.lon);
            parameters += "&" + fuel.getUrlParameters();
            using (SubmitForm dialog = new SubmitForm(parameters))
            {
                dialog.ShowDialog();
            }

            disposeFlight();
            updateFunction();
            currentAircraft.invalidatePosition();
        }


    }
}
