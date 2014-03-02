using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using FSEconomy.Properties;

namespace FSEconomy
{
    public class FSEconomyException : Exception
    {

        public FSEconomyException(String message):base(message)
        {
        }
    }


    public class webRequest
    {
        private static webRequest instance;
        private bool alive = false;
        public bool isAlive { get { return alive; } }

        public static webRequest getInstance()
        {
            if (instance == null)
                instance = new webRequest();
            return instance;
        }

        private String createParameter(object o1, object o2)
        {
            return HttpUtility.UrlEncode(o1.ToString()) + "=" + HttpUtility.UrlEncode(o2.ToString());
        }
        public static String getTextNode(XmlElement doc, String name)
        {
            XmlNodeList list = doc.GetElementsByTagName(name);
            if (list.Count == 0)
                return null;
            return list[0].FirstChild != null ? list[0].FirstChild.Value : "";
        }
        private XmlDocument callFixedParam(String requestType, String parameters)
        {
            XmlDocument xmldoc;
            try
            {
                if (parameters != "")
                    parameters = "&" + parameters;
                String uri = Properties.Settings.Default.server + "/fsagentFSX?" +
                    createParameter("action", requestType) +
                    "&" + createParameter("user", Properties.Settings.Default.username) +
                    "&" + createParameter("pass", Properties.Settings.Default.password) +
                    parameters;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

                WebResponse response = request.GetResponse();
                xmldoc = new XmlDocument();

                xmldoc.Load(response.GetResponseStream());
            }
            catch (Exception)
            {
                alive = false;
                throw new FSEconomyException("Server unavailable");
            }
            alive = true;
            String error = getTextNode(xmldoc.DocumentElement, "error");
            if (error != null)
            {
                throw new FSEconomyException(error);
            }
            return xmldoc;
        }

        public class airportInfo
        {
            public String icao;
            public String name;
            public float fuelPrice;
            public airportInfo(XmlElement input)
            {
                this.icao = webRequest.getTextNode(input, "icao");
                this.name = webRequest.getTextNode(input, "name");
                this.fuelPrice = float.Parse(webRequest.getTextNode(input, "fuelPrice"));
            }

        }
        public class closeAirport
        {
            public String icao;
            public float distance;
            public float bearing;
            public closeAirport(XmlElement input)
            {
                this.icao = webRequest.getTextNode(input, "icao");
                this.distance = float.Parse(webRequest.getTextNode(input, "distance"));
                this.bearing = float.Parse(webRequest.getTextNode(input, "bearing"));
            }
        }
        public class altAircraft
        {
            public String registration;
            public String model;
            public altAircraft(XmlElement input)
            {
                this.registration = webRequest.getTextNode(input, "registration");
                this.model = webRequest.getTextNode(input, "type");
            }
        }

        private XmlDocument call(String requestType, params Object[] args)
        {
            String uri = "";
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i > 0)
                    uri += "&";
                uri += createParameter(args[i], args[i + 1]);
            }
            return callFixedParam(requestType, uri);
        }

        public class aircraftProbeData
        {
            public String aircraftTitle;
            public String aircraftModel;
            public airportInfo currentAirport;
            public List<closeAirport> airports;
            public List<altAircraft> altAircraft;
            public bool availableAtCurrentAirport;
            public aircraftProbeData(String aircraftTitle, String aircraftModel, List<closeAirport> airports, airportInfo currentAirport, bool availableAtCurrentAirport, List<altAircraft> AltAircraft)
            {
                this.aircraftTitle = aircraftTitle;
                this.aircraftModel = aircraftModel;
                this.airports = airports;
                this.currentAirport = currentAirport;
                this.availableAtCurrentAirport = availableAtCurrentAirport;
                this.altAircraft = AltAircraft;
            }
        }

        public aircraftProbeData aircraftProbe(String aircraft, double lat, double lon)
        {
            XmlDocument result;
            try
            {
                result = call("aircraftProbe", "aircraft", aircraft, "lat", lat, "lon", lon);
            }
            catch (Exception)
            {
                return null;
            }
            if (result == null)
                return null;
            String aircraftType = getTextNode(result.DocumentElement, "aircraftType");
            List<closeAirport> airports = new List<closeAirport>();
            List<altAircraft> AltAircraft = new List<altAircraft>();
            airportInfo currentAirport = null;
            bool availableAtCurrentAirport = false;
            XmlNodeList airportInfo = result.GetElementsByTagName("airport");
            if (airportInfo.Count > 0)
                currentAirport = new airportInfo((XmlElement)airportInfo[0]);
            foreach (XmlNode airport in result.GetElementsByTagName("closeAirport"))
            {
                closeAirport ap = new closeAirport((XmlElement)airport);
                if (currentAirport != null && currentAirport.icao == ap.icao)
                    availableAtCurrentAirport = true;
                else
                    airports.Add(ap);
            }
            foreach (XmlNode aircraftNode in result.GetElementsByTagName("aircraft"))
            {
                AltAircraft.Add(new altAircraft((XmlElement)aircraftNode));
            }
            return new aircraftProbeData(aircraft, aircraftType, airports, currentAirport, availableAtCurrentAirport, AltAircraft);
        }

        public bool accountCheck()
        {
            try
            {

                XmlDocument result = call("accountCheck");
                if (result == null)
                    return false;
                return result.GetElementsByTagName("ok").Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public struct assignment
        {
            public string from;
            public string to;
            public string cargo;
            public string comment;
            public assignment(string from, string to, string cargo, string comment)
            {
                this.from = from;
                this.to = to;
                this.cargo = cargo;
                this.comment = comment;
            }
        }
        public Flight startFlight(String aircraft, double lat, double lon, Aircraft currentAircraft)
        {
            XmlDocument result;
            result = call("startFlight", "aircraft", aircraft, "lat", lat, "lon", lon);

            String registration = getTextNode(result.DocumentElement, "registration");
            String fuel = getTextNode(result.DocumentElement, "fuel");
            String leaseExpires = getTextNode(result.DocumentElement, "leaseExpires");
            bool isTacho = getTextNode(result.DocumentElement, "accounting") == "tacho";
            TimeSpan leaseTime = new TimeSpan(0, 0, Int32.Parse(leaseExpires));
            fuelStruct startFuel = new fuelStruct(fuel);
            bool rentedDry = getTextNode(result.DocumentElement, "rentedDry") == "true";
            float rentalPrice = float.Parse(getTextNode(result.DocumentElement, "rentalPrice"));
            int payloadWeight = Int32.Parse(getTextNode(result.DocumentElement, "payloadWeight"));
            List<assignment> assignmentList = new List<assignment>();
            foreach (XmlNode node in result.GetElementsByTagName("assignment"))
            {
                assignmentList.Add(new assignment(
                    getTextNode((XmlElement)node, "from"),
                    getTextNode((XmlElement)node, "to"),
                    getTextNode((XmlElement)node, "cargo"),
                    getTextNode((XmlElement)node, "comment")
                    ));
            }
            return new Flight(currentAircraft, registration, startFuel, leaseTime, rentedDry, rentalPrice, isTacho, payloadWeight, assignmentList);
        }

        public String endFlight(String parameters)
        {
            XmlDocument result = callFixedParam("arrive", parameters);
            return getTextNode(result.DocumentElement, "result");
        }

        public void cancelFlight()
        {
            call("cancel");
        }

        public void requestAircraft(Aircraft aircraft)
        {
            String parameters = createParameter("aircraft", aircraft.specifications.title) + "&" +
                aircraft.specifications.getFuelStruct().getUrlParameters();
            callFixedParam("addModel", parameters);
        }
    }
}
