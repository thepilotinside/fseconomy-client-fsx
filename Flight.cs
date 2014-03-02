using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;

namespace FSEconomy
{
    public enum DATA_REQUESTID
    {
        AIRCRAFT_SPECS,
        AIRCRAFT_INFO,
        FLIGHT_DATA,
        FLIGHT_FINISH_DATA
    }
   
    public enum DATA_DEF
    {
        AIRCRAFT_SPECS,
        AIRCRAFT_INFO,
        AIRCRAFT_FUEL,
        FLIGHT_DATA,
        AIRCRAFT_PAYLOAD,
        AIRCRAFT_START
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct aircraft_info
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public String title;
        public double lat;
        public double lon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct fuelStruct
    {
        public float fuelCenter;
        public float fuelCenter2;
        public float fuelCenter3;
        public float fuelLeftMain;
        public float fuelLeftAux;
        public float fuelLeftTip;
        public float fuelRightMain;
        public float fuelRightAux;
        public float fuelRightTip;
        public float fuelExt1;
        public float fuelExt2;

        public fuelStruct(String input)
        {
            String[] parts = input.Split(' ');
            fuelCenter = float.Parse(parts[0]);
            fuelLeftMain = float.Parse(parts[1]);
            fuelLeftAux = float.Parse(parts[2]);
            fuelLeftTip = float.Parse(parts[3]);
            fuelRightMain = float.Parse(parts[4]);
            fuelRightAux = float.Parse(parts[5]);
            fuelRightTip = float.Parse(parts[6]);
            fuelCenter2 = float.Parse(parts[7]);
            fuelCenter3 = float.Parse(parts[8]);
            fuelExt1 = float.Parse(parts[9]);
            fuelExt2 = float.Parse(parts[10]);
        }
        public fuelStruct(float fuelCenter, float fuelCenter2, float fuelCenter3, float fuelLeftMain,
            float fuelLeftAux, float fuelLeftTip, float fuelRightMain, float fuelRightAux, float fuelRightTip,
            float fuelExt1, float fuelExt2)
        {
            this.fuelCenter = fuelCenter;
            this.fuelCenter2 = fuelCenter2;
            this.fuelCenter3 = fuelCenter3;
            this.fuelLeftMain = fuelLeftMain;
            this.fuelLeftAux = fuelLeftAux;
            this.fuelLeftTip = fuelLeftTip;
            this.fuelRightMain = fuelRightMain;
            this.fuelRightAux = fuelRightAux;
            this.fuelRightTip = fuelRightTip;
            this.fuelExt1 = fuelExt1;
            this.fuelExt2 = fuelExt2;
        }
        public String getUrlParameters()
        {
            return String.Format("c={0:f}&lm={1:f}&la={2:f}&let={3:f}&rm={4:f}&ra={5:f}&rt={6:f}&c2={7:f}&c3={8:f}&x1={9:f}&x2={10:f}",
                fuelCenter, fuelLeftMain, fuelLeftAux, fuelLeftTip, fuelRightMain, fuelRightAux, fuelRightTip,
                fuelCenter2, fuelCenter3, fuelExt1, fuelExt2);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct flightData
    {
        public int altitude;
        public int altitude_agl;
        public int onground;
        public int wheelRpm;
        public int parkingBreak;
        public int groundVelocity;
        public int cht1;
        public int cht2;
        public int cht3;
        public int cht4;
        public int mixture1;
        public int mixture2;
        public int mixture3;
        public int mixture4;
        public int rpm1;
        public int rpm2;
        public int rpm3;
        public int rpm4;
        public int combustion1;
        public int combustion2;
        public int combustion3;
        public int combustion4;
        public float totalFuel;
        public int zuluTime;
        public float rpmPercentage;
        public int timeOfDay;
        public int visibility;
        public float crosswind;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct payloadStruct
    {
        public int stationx;

        public payloadStruct(int sx)
        {
            this.stationx = sx;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct startStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string atc;
        int slewAllowed;

        public startStruct(string atc, int slewAllowed)
        {
            if (atc.Length > 10)
                atc = atc.Substring(0, 10);
            this.atc = atc;
            this.slewAllowed = slewAllowed;
        }
    }

    public class Aircraft
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct aicraft_specs
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title;
            public int numEngines;
            public int engineType;
            public float fuelCenter;
            public float fuelCenter2;
            public float fuelCenter3;
            public float fuelLeftMain;
            public float fuelLeftAux;
            public float fuelLeftTip;
            public float fuelRightMain;
            public float fuelRightAux;
            public float fuelRightTip;
            public float fuelExt1;
            public float fuelExt2;
            public int emptyWeight;
            public int maxGrossWeight;
            public int estimateCruise;
            public float estimateFuelFlow;
            public int numPayloadStations;
            public fuelStruct getFuelStruct()
            {
                return new fuelStruct(fuelCenter, fuelCenter2, fuelCenter3, fuelLeftMain, fuelLeftAux, fuelLeftTip,
                    fuelRightMain, fuelRightAux, fuelRightTip, fuelExt1, fuelExt2);
            }
        }

        public aicraft_specs specifications;
        public aircraft_info latestInfo;
        private webRequest.aircraftProbeData _latestProbeData = null;
        public webRequest.aircraftProbeData latestProbeData
        {
            get {
                if (_latestProbeData == null)
                    _latestProbeData = doProbe();
                return _latestProbeData;
            }
        }
        public bool infoAvailable
        {
            get { return latestInfo.title != null; }
        }


        public bool aircraftInDatabase
        {
            get { return latestProbeData != null && latestProbeData.aircraftModel != null; }
        }
        public string aircraftDisplayName
        {
            get
            {
                if (latestProbeData != null && latestProbeData.aircraftModel != null)
                    return latestProbeData.aircraftModel;
                else
                    return specifications.title;
            }
        }

        public bool probeDataValid
        {
            get { return _latestProbeData != null; } 
        }
        // Indicate that the known position is no longer valid, force a reprobe
        public void invalidatePosition()
        {
            _latestProbeData = null;
        }
        
        // Probe the server for information about this aircraft
        private webRequest.aircraftProbeData doProbe()
        {
            if (latestInfo.title == null)
                return null;                                // Cannot do probe yet.
            webRequest.aircraftProbeData data;
            webRequest request = webRequest.getInstance();
            data = request.aircraftProbe(latestInfo.title, latestInfo.lat, latestInfo.lon);
            return data;
        }

        private void loadSpecsFromFS(SimConnect connection)
        {
            connection.RequestDataOnSimObject(DATA_REQUESTID.AIRCRAFT_SPECS, DATA_DEF.AIRCRAFT_SPECS, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 1);
        }
        public Aircraft(SimConnect connection)
        {
            loadSpecsFromFS(connection);
        }
        public void receiveData(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch ((DATA_REQUESTID)data.dwRequestID)
            {
                case DATA_REQUESTID.AIRCRAFT_SPECS:
                    {
                        specifications = (aicraft_specs)data.dwData[0];

                        specifications.estimateFuelFlow /= 1000.0f;         // estimateFF seems to be multiplied by 1000 by FS.
                        break;
                    }
            }
        }

        public static void SetDataDefinition(SimConnect connection)
        {          

            #region Register AIRCRAFT_SPECS
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Number of engines", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Engine type", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank center capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank center2 capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank center3 capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank left main capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank left aux capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank left tip capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank right main capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank right aux capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank right tip capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank external1 capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Fuel tank external2 capacity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Empty Weight", "kg", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "max gross weight", "kg", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Estimated cruise speed", "knots", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Estimated Fuel Flow", "gallon per hour", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_SPECS, "Payload station count", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.RegisterDataDefineStruct<aicraft_specs>(DATA_DEF.AIRCRAFT_SPECS);
            #endregion

            #region Register AIRCRAFT_INFO
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_INFO, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_INFO, "Plane latitude", "degree", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_INFO, "Plane longitude", "degree", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.RegisterDataDefineStruct<aircraft_info>(DATA_DEF.AIRCRAFT_INFO);
            #endregion

            #region Register AIRCRAFT_FUEL
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank center quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank center2 quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank center3 quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank left main quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank left aux quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank left tip quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank right main quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank right aux quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank right tip quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank external1 quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_FUEL, "Fuel tank external2 quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.RegisterDataDefineStruct<fuelStruct>(DATA_DEF.AIRCRAFT_FUEL);
            #endregion

            #region Register FLIGHT_DATA
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Plane altitude", "feet", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Plane alt above ground", "feet", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Sim on ground", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Wheel RPM", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Brake parking indicator", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Ground Velocity", "knots", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Recip eng cylinder head temperature:1", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Recip eng cylinder head temperature:2", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Recip eng cylinder head temperature:3", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Recip eng cylinder head temperature:4", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng mixture lever position:1", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng mixture lever position:2", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng mixture lever position:3", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng mixture lever position:4", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Prop RPM:1", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Prop RPM:2", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Prop RPM:3", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Prop RPM:4", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng combustion:1", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng combustion:2", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng combustion:3", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "General eng combustion:4", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Fuel total quantity", "gallons", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Zulu time", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Prop Max RPM Percent:1", null, SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Time of day", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Ambient Visibility", "meters", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.FLIGHT_DATA, "Aircraft wind X", "knots", SIMCONNECT_DATATYPE.FLOAT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            connection.RegisterDataDefineStruct<flightData>(DATA_DEF.FLIGHT_DATA);
            #endregion

            #region Register AIRCRAFT_PAYLOAD
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_PAYLOAD, "Payload station weight:1", "kg", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.RegisterDataDefineStruct<payloadStruct>(DATA_DEF.AIRCRAFT_PAYLOAD);
            #endregion

            #region Register AIRCRAFT_START
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_START, "ATC ID", null, SIMCONNECT_DATATYPE.STRINGV, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_START, "Is slew allowed", null, SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.RegisterDataDefineStruct<startStruct>(DATA_DEF.AIRCRAFT_START);
            #endregion
        }
    }
    class engine
    {
        int oldCht;
        int engineNumber;

        int runtime;
        float chtDamage;
        int mixtureDamage;

        public engine(int number)
        {
            this.engineNumber = number;
        }

        public void feed(int seconds, bool combustion, int rpm, int cht, int mixture, int altitude)
        {
            if (combustion)
                runtime += seconds;
            if (oldCht > 0)
            {
                float diff = Math.Abs(cht - oldCht) / (float)seconds;
                if (diff > 1)
                    chtDamage += diff;
            }
            oldCht = cht;
            if (mixture > 95 && altitude > 1000)
                mixtureDamage += seconds;
        }
        public String getUrlParameters()
        {
            return String.Format("mixture{0}={1:d}&heat{0}={2:d}&time{0}={3:d}", 
                engineNumber, mixtureDamage, (int)Math.Round(chtDamage), runtime);
        }
    }

    public class Flight
    {
        Aircraft aircraft;
        String registration;
        fuelStruct fuelState;
        public flightData latestData;
        TimeSpan leaseTime;
        DateTime started;
        bool rentedDry;
        bool isTacho;
        float rentalPrice;
        float rentalTicker = 0.0f;
        bool flightDataValid = false;
        int nightTime = 0;
        float initialFuel;
        float engineTime;
        float fuelPrice;
        engine[] engines;
        int payloadWeight;
        float envBonusTime = 0.0f;
        float envBonus = 0.0f;
        List<webRequest.assignment> assignmentList;

        public Flight(Aircraft aircraft, String registration, fuelStruct fuelState, TimeSpan leaseTime, bool rentedDry, float rentalPrice, bool isTacho, int payloadWeight, List<webRequest.assignment> assignmentList)
        {
            this.aircraft = aircraft;
            this.registration = registration;
            this.fuelState = fuelState;
            this.leaseTime = leaseTime;
            this.rentedDry = rentedDry;
            this.rentalPrice = rentalPrice;
            this.isTacho = isTacho;
            this.payloadWeight = payloadWeight;
            this.fuelPrice = aircraft.latestProbeData.currentAirport.fuelPrice;
            this.assignmentList = assignmentList;
            this.started = DateTime.UtcNow;
            engines = new engine[aircraft.specifications.numEngines];
            for (int c = 0; c < engines.Length; c++)
                engines[c] = new engine(c+1);
        }

        public float environmentBonus()
        {
            float result = envBonus / envBonusTime;
            if (result < 1)
                result = 1;
            if (result > 2.5)
                result = 2.5f;
            return result;
        }

        public TimeSpan leaseTimeLeft()
        {
            TimeSpan elapsed = DateTime.UtcNow.Subtract(started);
            return leaseTime.Subtract(elapsed);
        }

        public List<webRequest.assignment> getAssignmentList()
        {
            return assignmentList;
        }

        public float runningRentalCost()
        {
            return rentalPrice * (isTacho ? (rentalTicker / 100f) : (engineTime / 3600f));
        }

        public float runningFuelCost()
        {
            return rentedDry ? fuelPrice * (initialFuel - latestData.totalFuel) : 0.0f;
        }

        public void updateProgressBar(System.Windows.Forms.ProgressBar bar)
        {
            bar.Maximum = (int)leaseTime.TotalMilliseconds;
            TimeSpan diff = DateTime.UtcNow.Subtract(started);
            int delta = (int)(leaseTime.TotalMilliseconds - diff.TotalMilliseconds);
            if (delta < 0)
                delta = 0;
            bar.Value = delta;
        }

        void setPayloadStation(SimConnect connection, int station, int weight)
        {
            payloadStruct payload = new payloadStruct(weight);
            connection.ClearDataDefinition(DATA_DEF.AIRCRAFT_PAYLOAD);
            connection.AddToDataDefinition(DATA_DEF.AIRCRAFT_PAYLOAD, String.Format("Payload station weight:{0}", station), "kg", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            connection.SetDataOnSimObject(DATA_DEF.AIRCRAFT_PAYLOAD, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, payload);
        }

        public void start(SimConnect connection)
        {
            connection.SetDataOnSimObject(DATA_DEF.AIRCRAFT_FUEL, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, fuelState);           
            
            int payloadPerStation = (int)(payloadWeight/aircraft.specifications.numPayloadStations);
            for (int c=0; c< aircraft.specifications.numPayloadStations; c++)
                setPayloadStation(connection, c + 1, payloadPerStation);

            startStruct start = new startStruct(registration, 0);
            connection.SetDataOnSimObject(DATA_DEF.AIRCRAFT_START, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, start);
            connection.RequestDataOnSimObject(DATA_REQUESTID.FLIGHT_DATA, DATA_DEF.FLIGHT_DATA, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

        }
        public void stop(SimConnect connection)
        {
            startStruct start = new startStruct(registration, 1);
            connection.SetDataOnSimObject(DATA_DEF.AIRCRAFT_START, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, start);
            connection.RequestDataOnSimObject(DATA_REQUESTID.FLIGHT_DATA, DATA_DEF.FLIGHT_DATA, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        private float weatherSeverity()
        {
            float weather = 1;
            if (latestData.visibility <= 500)
                weather++;
            if (latestData.visibility <= 300)
                weather++;
            if (latestData.altitude_agl < 2000)
                weather += Math.Abs(latestData.crosswind) / 5.0f;
            return weather;

        }

        public void receiveData(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            int diffSeconds = latestData.zuluTime;
            latestData = (flightData)data.dwData[0];
            if (!flightDataValid)
            {
                // First reception of data
                initialFuel = latestData.totalFuel;
                flightDataValid = true;
                return;
            }
            diffSeconds = latestData.zuluTime - diffSeconds;
            if (diffSeconds <= 0)
                return;

            int envBonusFactor = latestData.altitude_agl <= 1000 ? 5 : latestData.altitude_agl < 2000 ? 3 : 1;
            envBonusTime += envBonusFactor * diffSeconds;
            envBonus += envBonusFactor * diffSeconds * weatherSeverity();

            if (isTacho)
                rentalTicker += diffSeconds * latestData.rpmPercentage / 36.0f;
            else
                engineTime += diffSeconds;

            if (latestData.timeOfDay != 1)
                nightTime += diffSeconds;

            if (engines.Length > 0)
                engines[0].feed(diffSeconds, latestData.combustion1 != 0, latestData.rpm1, latestData.cht1, latestData.mixture1, latestData.altitude);
            if (engines.Length > 1)
                engines[1].feed(diffSeconds, latestData.combustion2 != 0, latestData.rpm2, latestData.cht2, latestData.mixture2, latestData.altitude);
            if (engines.Length > 2)
                engines[2].feed(diffSeconds, latestData.combustion3 != 0, latestData.rpm3, latestData.cht3, latestData.mixture3, latestData.altitude);
            if (engines.Length > 3)
                engines[3].feed(diffSeconds, latestData.combustion4 != 0, latestData.rpm4, latestData.cht4, latestData.mixture4, latestData.altitude);

        }

        public void requestFinishData(SimConnect connection)
        {
            connection.RequestDataOnSimObject(DATA_REQUESTID.FLIGHT_FINISH_DATA, DATA_DEF.AIRCRAFT_FUEL, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);            
        }

        public bool nightBonus
        {
            get { return ((float) nightTime / (float) engineTime) > 0.5;  }
        }

        public String getUrlParameters()
        {
            String result = "";
            if (isTacho)
                result += String.Format("rentalTick={0}", (int)rentalTicker);
            else
                result += String.Format("rentalTime={0}", engineTime);
            result += String.Format("&night={0}", nightBonus ? 1 : 0);
            result += String.Format("&env={0:f}", environmentBonus());
            foreach (engine e in engines)
                result += "&" + e.getUrlParameters();
            return result;
        }        
    }
}
