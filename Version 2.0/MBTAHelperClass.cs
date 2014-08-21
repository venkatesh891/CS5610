
/*
    File: ~/app_code/venkatesh/mbta/MBTAHelperClass.cs
    
    Copyright 2014,
    Venkatesh Balasubramanian,
    College of Computer and Information Science
    Northeastern University, Boston, MA 02115
    venkat89@ccs.neu.edu
 */

using System;
using System.Data;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Globalization;
using System.Data.SqlClient;

namespace edu.neu.ccis.venkat89
{
    /// <summary>
    /// MBTA Helper Class is used to consume the MBTA API for retrieving the trains data
    /// </summary>
    public class MBTAHelperClass
    {
        private string appKey = string.Empty;

        // Constructor to set the app key value
        public MBTAHelperClass(string appKey)
        {
            this.appKey = appKey;
        }


        #region Common MBTA Helper Logics


        // GetMBTAServerTime : Void -> String
        // GIVEN: takes no argument
        // RETURNS: the MBTA server time in epoch unix time format
        public string GetMBTAServerTime()
        {
            string serverTime = string.Empty;
            try
            {
                string baseURL = "http://realtime.mbta.com/developer/api/v2/servertime?api_key=" + appKey;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            // Third index position in the xml holds the server time in unix time stamp format
                            reader.MoveToAttribute(2);
                            serverTime = reader.Value.Trim();
                            break;
                        }
                    }
                }

                return serverTime;
            }
            catch (Exception ex)
            {
                return serverTime;
            }
        }



        // GetServerMachineTimeInUnixEpochFormat : Void -> String
        // GIVEN: takes no argument
        // RETURNS: the CCIS server machine time in epoch unix time format
        public string GetServerMachineTimeInUnixEpochFormat()
        {
            string serverTime = string.Empty;
            try
            {
                serverTime = ConvertNormalDateTimeToEpochTime(DateTime.Now);
                return serverTime;
            }
            catch (Exception ex)
            {
                return serverTime;
            }

        }


        // IsMBTAShutDown : Void -> Boolean
        // GIVEN: takes no argument
        // RETURNS: true iff when the MBTA has stopped running during night times (00:15 untill 05:30)
        public bool IsMBTAShutDown()
        {
            bool isClosed = false;
            try
            {
                // The MBTA Train service is halted from 00:15 to 05:30
                const int serviceStopTimeHours = 0;
                const int serviceStopTimeMinutes = 15;
                const int serviceResumeTimeHours = 5;
                const int serviceResumeTimeMinutes = 30;

                string serverTime = string.Empty;
                /* The below code was written to fetch the server time from MBTA. */
                //serverTime = GetServerMachineTimeInUnixEpochFormat();
                //serverTime = ConvertEpochTimeToNormalDateTime(double.Parse(serverTime));

                serverTime = DateTime.Now.ToString("HH:mm").Trim();

                if (serverTime.Length > 0)
                {
                    string[] tempArray = serverTime.Split(':');
                    int currentHour = int.Parse(tempArray[0]);
                    int currentMinute = int.Parse(tempArray[1]);

                    // Checking for 00:15 minutes
                    if (currentHour == serviceStopTimeHours)
                    {
                        if (currentMinute >= serviceStopTimeMinutes)
                        {
                            isClosed = true;
                        }
                    }

                    // Checking for 05:30 minutes
                    else if (currentHour == serviceResumeTimeHours)
                    {
                        if (currentMinute <= serviceResumeTimeMinutes)
                        {
                            isClosed = true;
                        }
                    }

                    // Checking for interval between 00:30 to 05:30
                    else if ((currentHour > serviceStopTimeHours) && (currentHour < serviceResumeTimeHours))
                    {
                        isClosed = true;
                    }
                }

                return isClosed;
            }
            catch (Exception ex)
            {
                return isClosed;
            }
        }


        // GetRouteIDFromStopID : String -> List<String>
        // GIVEN: a stopID as argument
        // RETURNS: all the routeID's for the corressponding stopID which was passed as argument
        public List<string> GetRouteIDFromStopID(string stopID)
        {
            List<string> routeID = new List<string>();
            try
            {
                string baseURL = "http://realtime.mbta.com/developer/api/v2/routesbystop?api_key=" + appKey + "&stop=" + stopID;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                /* Sample Response XML Contents
                <route_list xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" stop_id="70250" stop_name="Brigham Circle - Inbound">
                  <mode route_type="0" mode_name="Subway">
                    <route route_id="880_" route_name="Green Line"/>
                    <route route_id="882_" route_name="Green Line"/>
                  </mode>
                </route_list>
                */

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "route":
                                    // First index position in the <route> tag contains ID
                                    reader.MoveToAttribute(0);
                                    routeID.Add(reader.Value.Trim());
                                    break;
                            }
                        }
                    }
                }

                return routeID;
            }
            catch (Exception ex)
            {
                return routeID;
            }
        }


        // GetMatchingRouteIDFromStartAndEndPlace : List<String> List<String> -> List<String>
        // GIVEN: list of boardingRouteIDs and destinationRouteIDs as argument
        // RETURNS: the list of matching routeID's from the boarding and destination route
        public List<string> GetMatchingRouteIDFromStartAndEndPlace(List<string> boardingRouteIDs, List<string> destinationRouteIDs)
        {
            List<string> matchingRouteIDs = new List<string>();
            try
            {
                foreach (string routeID in boardingRouteIDs)
                {
                    foreach (string destRouteID in destinationRouteIDs)
                    {
                        if (routeID.Equals(destRouteID))
                        {
                            matchingRouteIDs.Add(routeID);
                            break;
                        }
                    }
                }

                return matchingRouteIDs;
            }
            catch (Exception ex)
            {
                return matchingRouteIDs;
            }
        }


        // PullAPIResponseInXMLStringFormat : String -> String
        // GIVEN: a constructedURL with which the API call has to be made to MBTA
        // RETURNS: response of the call in XML Format
        public string PullAPIResponseInXMLStringFormat(string constructedURL)
        {
            string responseXMLContents = string.Empty;
            try
            {
                WebClient wc = new WebClient();
                // Forcing the response type to be XML instead of JSON
                wc.Headers["Accept"] = "application/xml";
                responseXMLContents = wc.DownloadString(constructedURL);
                return responseXMLContents;
            }
            catch (Exception ex)
            {
                return responseXMLContents;
            }
        }


        // ConvertEpochTimeToNormalDateTime : Double -> String
        // GIVEN: timestamp in epoch unix format
        // RETURNS: time in 24hrs clock format
        public string ConvertEpochTimeToNormalDateTime(double epochTime)
        {
            string normalDateTime = string.Empty;
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime dateT = epoch.AddSeconds(epochTime);

                // Converting the date time to EST Time Standards dynamically based on the server timings
                TimeZone zone = TimeZone.CurrentTimeZone;
                // Get offset.
                TimeSpan offset = zone.GetUtcOffset(DateTime.Now);
                dateT = dateT.AddHours(offset.Hours);

                // Formatting the datetime to have only the time in 24hrs clock format
                normalDateTime = dateT.ToString("HH:mm");
                return normalDateTime;
            }
            catch (Exception ex)
            {
                return normalDateTime;
            }

        }



        // ConvertEpochTimeToNormalDateTime : DateTime -> String
        // GIVEN: timestamp in normal date time format
        // RETURNS: time in epoch unix format
        public string ConvertNormalDateTimeToEpochTime(DateTime dateTime)
        {
            string epochUnixTime = string.Empty;
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                double epochSeconds = (dateTime - epoch).TotalSeconds;
                epochUnixTime = epochSeconds.ToString();
                return epochUnixTime;
            }
            catch (Exception ex)
            {
                return epochUnixTime;
            }

        }



        // GetDirectionFromStopsByRoute : String String String -> String
        // GIVEN: a routeID, boardingPlace and destinationPlace as arguments
        // RETURNS: direction which is either 0 or 1
        public string GetDirectionFromStopsByRoute(string routeID, string boardingPlace, string destinationPlace)
        {
            string direction = string.Empty;
            try
            {
                string baseURL = "http://realtime.mbta.com/developer/api/v2/stopsbyroute?api_key=" + appKey + "&route=" + routeID;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                bool isDirectionFound = false;
                bool directionFlag = false;
                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (!isDirectionFound)
                            {
                                switch (reader.Name)
                                {
                                    case "direction":
                                        //First index value holds the direction id in the xml
                                        reader.MoveToAttribute(0);
                                        direction = reader.Value;
                                        //Direction flag is required to identify the direction (east or west bounds)
                                        directionFlag = true;
                                        break;

                                    case "stop":
                                        //Fourth index value holds the parent stop name in xml
                                        reader.MoveToAttribute(4);
                                        string xmlStopName = reader.Value.Trim();
                                        if ((xmlStopName.Equals(boardingPlace)) || (xmlStopName.Equals(destinationPlace)))
                                        {
                                            if ((xmlStopName.Equals(boardingPlace)) & (directionFlag))
                                            {
                                                isDirectionFound = true;
                                            }
                                            else
                                            {
                                                //The directionFlag has to be reverted as the places would occure twice in the xml
                                                directionFlag = false;
                                            }
                                        }

                                        break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                return direction;
            }
            catch (Exception ex)
            {
                return direction;
            }
        }


        // GetStopOrder : String String String -> String
        // GIVEN: a routeID, direction and parentStationName as arguments
        // RETURNS: stopOrder for the particular station
        public string GetStopOrder(string routeID, string direction, string parentStationName)
        {
            string stopOrder = string.Empty;
            try
            {
                bool isRightDirectionFound = false;
                bool isStopOrderFound = false;
                string baseURL = "http://realtime.mbta.com/developer/api/v2/stopsbyroute?api_key=" + appKey + "&route=" + routeID;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (!isStopOrderFound)
                            {
                                switch (reader.Name)
                                {
                                    case "direction":
                                        //Zeroth index value holds the direction id in the xml
                                        reader.MoveToAttribute(0);
                                        string directionInXML = reader.Value.Trim();
                                        if (directionInXML.Equals(direction.Trim()))
                                        {
                                            //isRightDirectionFound flag is required to identify the direction (inbound or outbound)
                                            //only after the right direction is found, we start scanning the stop tags in xml 
                                            isRightDirectionFound = true;
                                        }
                                        break;

                                    case "stop":
                                        if (isRightDirectionFound)
                                        {
                                            //Fourth index value holds the parent stop name in the xml
                                            reader.MoveToAttribute(4);
                                            string parentStopNameInXML = reader.Value.ToLower().Trim();
                                            if (parentStopNameInXML.Equals(parentStationName.ToLower().Trim()))
                                            {
                                                //Zeroth index value holds the stop order in the xml
                                                reader.MoveToAttribute(0);
                                                stopOrder = reader.Value.Trim();
                                                isStopOrderFound = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                return stopOrder;
            }
            catch (Exception ex)
            {
                return stopOrder;
            }
        }


        // ComputeTheFutureAvailableTrainStartTime : DateTime String -> DateTime
        // GIVEN: someStartTimeOfTrain in the date time format and gap duration between 2 trains as arguments
        // RETURNS: when the someStartTimeOfTrain is greater than the server time then return that value
        //            else compute the next train based on the gap duration which is passed as argument
        public DateTime ComputeTheFutureAvailableTrainStartTime(DateTime someStartTimeOfTrain, string nextTrainGap)
        {
            DateTime nextTrainStartTime = someStartTimeOfTrain;
            try
            {
                bool isFutureTrainTimingsComputed = false;
                int travelTimeInMinutes = int.Parse(nextTrainGap);

                while (!isFutureTrainTimingsComputed)
                {
                    // Break the while loop once when the future train timing is computed
                    if (IsProvidedTimeInFuture(nextTrainStartTime))
                    {
                        isFutureTrainTimingsComputed = true;
                        break;
                    }
                    else
                    {
                        // Compute the next train time
                        nextTrainStartTime = nextTrainStartTime.AddMinutes(travelTimeInMinutes);
                    }
                }

                return nextTrainStartTime;
            }
            catch (Exception ex)
            {
                return nextTrainStartTime;
            }
        }



        // ComputeTheFutureAvailableHopTrainStartTime : DateTime DateTime String -> DateTime
        // GIVEN: initial train reach time to the hop station in date time format, hopTrainStartTime in
        //          the date time format and gap duration between 2 trains as arguments
        // RETURNS: when the someStartTimeOfTrain is greater than the server time then return that value
        //            else compute the next train based on the gap duration which is passed as argument
        public DateTime ComputeTheFutureAvailableHopTrainStartTime(DateTime firstTrainReachTime, DateTime hopTrainStartTime, string nextTrainGap)
        {
            DateTime nextTrainStartTime = hopTrainStartTime;
            try
            {
                bool isFutureTrainTimingsComputed = false;
                int travelTimeInMinutes = int.Parse(nextTrainGap);

                while (!isFutureTrainTimingsComputed)
                {
                    // Break the while loop once when the hop train timing is greater than the first train reach time
                    if (nextTrainStartTime > firstTrainReachTime)
                    {
                        isFutureTrainTimingsComputed = true;
                        break;
                    }
                    else
                    {
                        // Compute the next train time
                        nextTrainStartTime = nextTrainStartTime.AddMinutes(travelTimeInMinutes);
                    }
                }

                return nextTrainStartTime;
            }
            catch (Exception ex)
            {
                return nextTrainStartTime;
            }
        }



        // IsProvidedTimeInFuture : DateTime -> bool
        // GIVEN: anyClockTime in the DateTime format as argument
        // RETURNS: true iff the passed anyClockTime argument time is greater than the server time
        public bool IsProvidedTimeInFuture(DateTime anyClockTime)
        {
            bool isCurrentTimeInFuture = false;
            try
            {
                /* Commening the below 2 lines of code as we would cutting down the API calls */
                //string epochServerTime = GetServerMachineTimeInUnixEpochFormat();
                //string serverTimeInString = ConvertEpochTimeToNormalDateTime(double.Parse(epochServerTime));

                DateTime serverTime = DateTime.Now;
                // The boolean is set true iff the passed argument anyClockTime is greater than the server time
                if (anyClockTime > serverTime)
                {
                    isCurrentTimeInFuture = true;
                }

                return isCurrentTimeInFuture;
            }
            catch (Exception ex)
            {
                return isCurrentTimeInFuture;
            }
        }


        // FrameTheTrainSchedule : DateTime String String String String -> DataSet
        // GIVEN: train start time, travel duration, time gap between 2 consecutive trains, boarding place and destination place as arguments
        // RETURNS: a dataset with single row having the train arrival time at the boarding place and time when it reaches the destination place
        public DataSet FrameTheTrainSchedule(DateTime trainStartTime, string travelDuration, string nextTrainGap, string boardingPlace, string destinationPlace)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                dt.Columns.Add("Arrives On");
                dt.Columns.Add("Reaches By");

                // First row hold the information boarding and destination stop name
                dt.Rows.Add();
                dt.Rows[0][0] = "Arrives - " + boardingPlace + " - By ";
                dt.Rows[0][1] = "Reaches - " + destinationPlace + " - By ";

                // Second row holds the first train schedule information
                dt.Rows.Add();
                dt.Rows[1][0] = trainStartTime.ToString("HH:mm").Trim();
                int travelTimeInMinutes = int.Parse(travelDuration);
                dt.Rows[1][1] = trainStartTime.AddMinutes(travelTimeInMinutes).ToString("HH:mm").Trim();

                // Third row holds the next train schedule information
                dt.Rows.Add();
                int nextTrainGapInMinutes = int.Parse(nextTrainGap);
                string lastTrainStartTime = trainStartTime.AddMinutes(nextTrainGapInMinutes).ToString("HH:mm").Trim();
                dt.Rows[2][0] = lastTrainStartTime;

                DateTime lastTrainInDateTimeFormat = DateTime.ParseExact(lastTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                dt.Rows[2][1] = lastTrainInDateTimeFormat.AddMinutes(travelTimeInMinutes).ToString("HH:mm").Trim();


                // Loading the datatable contents into dataset
                ds.Tables.Add(dt);

                return ds;

            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // IsThereAnyServiceDisruption : String String -> Boolean
        // GIVEN: boardingPlace and destinationPlace as arguments
        // RETURNS: true iff there is any train service disruption in the travel routes
        public bool IsThereAnyServiceDisruption(string boardingPlace, string destinationPlace)
        {
            bool isServiceDisruptedAtTheseStops = false;
            try
            {
                //Beginning March 22, 2014, Government Center Station will be closed for two years while crews work to reconstruct the station into a fully accessible, safer, modern, more comfortable facility.";
                const string govtCenterStation = "Government Center Station";
                if ((boardingPlace.Trim().Equals(govtCenterStation.Trim())) ||
                    (destinationPlace.Trim().Equals(govtCenterStation.Trim())))
                {
                    isServiceDisruptedAtTheseStops = true;
                }
                return isServiceDisruptedAtTheseStops;
            }
            catch (Exception ex)
            {
                return isServiceDisruptedAtTheseStops;
            }
        }


        // GetServiceDisruptionMessage : String String -> String
        // GIVEN: boardingPlace and destinationPlace as arguments
        // RETURNS: message containing the details about train service disruption
        public string GetServiceDisruptionMessage(string boardingPlace, string destinationPlace)
        {
            string serviceDisruptionMessage = string.Empty;
            try
            {
                const string govtCenterStation = "Government Center Station";
                if ((boardingPlace.Trim().Equals(govtCenterStation.Trim())) ||
                    (destinationPlace.Trim().Equals(govtCenterStation.Trim())))
                {
                    serviceDisruptionMessage = "Beginning March 22, 2014, Government Center Station will be closed for two years while crews work to reconstruct the station into a fully accessible, safer, modern, more comfortable facility.";
                }

                return serviceDisruptionMessage;
            }
            catch (Exception ex)
            {
                return serviceDisruptionMessage;
            }
        }


        // GetTravelDuration : Dataset -> String
        // GIVEN: a dataset containing the train schedule as argument
        // RETURNS: returns travel duration between boarding and destination point
        public string GetTravelDuration(DataSet ds)
        {
            string travelTime = string.Empty;
            try
            {
                if (ds.Tables[0].Rows.Count > 1)
                {
                    string firstTrainStartTime = ds.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = ds.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    travelTime = ts1.Minutes.ToString().Trim();
                }

                return travelTime;
            }
            catch (Exception ex)
            {
                return travelTime;
            }
        }


        // GetTrainArrivalTime : Dataset -> String
        // GIVEN: a dataset containing the train schedule as argument
        // RETURNS: returns the train arrival time at the boarding place
        public string GetTrainArrivalTime(DataSet ds)
        {
            string arrivalTime = string.Empty;
            try
            {
                if (ds.Tables[0].Rows.Count > 1)
                {
                    arrivalTime = ds.Tables[0].Rows[1][0].ToString().Trim();
                }

                return arrivalTime;
            }
            catch (Exception ex)
            {
                return arrivalTime;
            }
        }


        // GetTrainReachesByTime : Dataset -> String
        // GIVEN: a dataset containing the train schedule as argument
        // RETURNS: returns the train reaches by time to the destination place
        public string GetTrainReachesByTime(DataSet ds)
        {
            string reachesByTime = string.Empty;
            try
            {
                if (ds.Tables[0].Rows.Count > 1)
                {
                    reachesByTime = ds.Tables[0].Rows[1][1].ToString().Trim();
                }

                return reachesByTime;
            }
            catch (Exception ex)
            {
                return reachesByTime;
            }
        }


        // IsTheFirstTimeEarlier : String String -> Boolean
        // GIVEN: two train timings in the string (HH:mm) format as arguments
        // RETURNS: returns true iff the first train time is earlier than the second train time
        public bool IsTheFirstTimeEarlier(string firstTime, string secondTime)
        {
            bool isFirstTimeEarly = false;
            try
            {
                DateTime firstTrainDateTimeFormat = DateTime.ParseExact(firstTime, "HH:mm", new DateTimeFormatInfo());
                DateTime secondTrainDateTimeFormat = DateTime.ParseExact(secondTime, "HH:mm", new DateTimeFormatInfo());
                if (firstTrainDateTimeFormat < secondTrainDateTimeFormat)
                {
                    isFirstTimeEarly = true;
                }
                return isFirstTimeEarly;
            }
            catch (Exception ex)
            { return isFirstTimeEarly; }
        }


        #endregion


        #region SQLConnection

        // EstablishConnectionWithSQLDB : Void -> SQLConnection
        // GIVEN: takes no argument as input 
        // RETURNS: SQLConnection instance after establishing connection with the SQL DB
        public SqlConnection EstablishConnectionWithSQLDB()
        {
            SqlConnection connection = null;
            try
            {
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["ProductionSQLConnectionString"].ConnectionString;
                connection = new SqlConnection(connString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                return connection;
            }
        }

        // KillSQLConnection : SQLConnection -> Void
        // GIVEN: takes SQLConnection instance as input 
        // RETURNS: void. This method kills the SQL Connection instance
        public void KillSQLConnection(SqlConnection connection)
        {
            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {

            }
        }


        // FetchDataFromSQLDB : SQLQuery, SQLConnection -> DataSet
        // GIVEN: takes SQLQuery and SQLConnection instance as argument
        // RETURNS: DataSet which holds the values fetched from SQLDB after executing the query
        public DataSet FetchDataFromSQLDB(string query, SqlConnection connection)
        {
            DataSet ds = new DataSet();
            try
            {
                SqlDataAdapter newAdapter = new SqlDataAdapter(query, connection);
                //90 seconds time out
                newAdapter.SelectCommand.CommandTimeout = 90;
                newAdapter.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            { return ds; }
        }

        #endregion


        #region GreenLine-E Train Logics

        // GetGreenLineEStopID : String -> String
        // GIVEN: a stopName as argument
        // RETURNS: stopID for the corressponding stopName which was passed as argument
        public string GetGreenLineEStopID(string stopName)
        {
            string stopID = string.Empty;
            try
            {
                string query = "select StopID from venkat89.GreenLineEStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    stopID = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return stopID;
            }
            catch (Exception ex)
            {
                return stopID;
            }
        }


        // GetGreenLineEStopNameForServices : String -> DataSet
        // GIVEN:  takes keywords of stop name as argument
        // RETURNS: the dataset containing stop names which matches with the passed argument
        public DataSet GetGreenLineEStopNameForServices(string partialStopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.GreenLineEStopDetails where StopName like '%" + partialStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetGreenLineEStopCoordinatesForServices : String -> DataSet
        // GIVEN:  takes the stop name as argument
        // RETURNS: the dataset containing stop coordinates which matches with the passed argument
        public DataSet GetGreenLineEStopCoordinatesForServices(string stopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopLatitude, StopLongitude from venkat89.GreenLineEStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetGreenLineEScheduleByRoute : String String String String -> DataSet
        // GIVEN: a routeID, direction, boardingPlace and destinationPlace as arguments
        // RETURNS: the next available train timings in the dataset format
        public DataSet GetGreenLineEScheduleByRoute(string routeID, string direction, string boardingPlace, string destinationPlace)
        {
            List<string> scheduleInfo = new List<string>();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Arrives On");
            dt.Columns.Add("Reaches By");
            try
            {
                // We would be needing the stop order to scan the xml for retrieving the arrival and departure times
                string boardingPlaceStopOrder = GetStopOrder(routeID, direction, boardingPlace);
                string destinationPlaceStopOrder = GetStopOrder(routeID, direction, destinationPlace);

                string baseURL = "http://realtime.mbta.com/developer/api/v2/schedulebyroute?api_key=" + appKey + "&route=" + routeID + "&direction=" + direction;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "stop":
                                    // Zeroth index value holds the stop name in xml
                                    reader.MoveToAttribute(0);
                                    string xmlStopSequence = reader.Value.Trim();
                                    if (xmlStopSequence.Equals(boardingPlaceStopOrder))
                                    {
                                        //Third index value holds the arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    if (xmlStopSequence.Equals(destinationPlaceStopOrder))
                                    {
                                        //Third index value holds the destination arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    break;
                            }
                        }
                    }
                }

                string[] arrayContents = scheduleInfo.ToArray();
                int arrayCounter = 0;
                for (int iRow = 0; iRow < (scheduleInfo.Count / 2); iRow++)
                {
                    dt.Rows.Add();
                    if (iRow == 0)
                    {
                        dt.Rows[iRow][0] = "Arrives - " + boardingPlace + " - By ";
                        dt.Rows[iRow][1] = "Reaches - " + destinationPlace + " - By ";
                        dt.Rows.Add();
                        iRow++;
                    }
                    // Only 2 columns are retrieved from XML which represents the Scheduled Arrival and Scheduled Departure Timings
                    for (int iCol = 0; iCol < 2; iCol++)
                    {
                        dt.Rows[iRow][iCol] = arrayContents[arrayCounter];
                        arrayCounter++;
                    }
                }

                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // CalculateNextAvailableGreenELineTrain : DataSet -> DataSet
        // GIVEN: a dataset as argument
        // RETURNS: a dataset with additional row predicting the next available train
        public DataSet CalculateNextAvailableGreenLineETrain(DataSet currentScheduleDS, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableTrainStartTime(latestTrainStartTime, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        // IsStopNameValidInGreenLineE : DataSet -> DataSet
        // GIVEN: a stop name in green line e as argument
        // RETURNS: true iff the stop name is valid and is present in the database
        public bool IsStopNameValidInGreenLineE(string stopName)
        {
            bool isStopNameValid = false;
            try
            {
                string query = "select StopID from venkat89.GreenLineEStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                // Make the flag true iff the record is returned from the SQL DB
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isStopNameValid = true;
                }

                return isStopNameValid;
            }
            catch (Exception ex)
            {
                return isStopNameValid;
            }
        }


        #endregion


        #region OrangeLine Train Logics


        // IsStopNameValidInOrangeLine : DataSet -> DataSet
        // GIVEN: a stop name in orange line as argument
        // RETURNS: true iff the stop name is valid and is present in the database
        public bool IsStopNameValidInOrangeLine(string stopName)
        {
            bool isStopNameValid = false;
            try
            {
                string query = "select StopID from venkat89.OrangeLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                // Make the flag true iff the record is returned from the SQL DB
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isStopNameValid = true;
                }

                return isStopNameValid;
            }
            catch (Exception ex)
            {
                return isStopNameValid;
            }
        }


        // GetOrangeLineStopNameForServices : String -> DataSet
        // GIVEN:  takes keywords of stop name as argument
        // RETURNS: the dataset containing stop names which matches with the passed argument
        public DataSet GetOrangeLineStopNameForServices(string partialStopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.OrangeLineStopDetails where StopName like '%" + partialStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetOrangeLineStopCoordinatesForServices : String -> DataSet
        // GIVEN:  takes the stop name as argument
        // RETURNS: the dataset containing stop coordinates which matches with the passed argument
        public DataSet GetOrangeLineStopCoordinatesForServices(string stopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopLatitude, StopLongitude from venkat89.OrangeLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetOrangeLineAllStopDetailsForServices : Void -> DataSet
        // GIVEN:  takes no argument
        // RETURNS: the dataset containing all the orange line stop details
        public DataSet GetOrangeLineAllStopDetailsForServices()
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select * from venkat89.OrangeLineStopDetails";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }



        // GetOrangeLineStopName : String -> String
        // GIVEN: a stopName as argument
        // RETURNS: stopID for the corressponding stopName which was passed as argument
        public string GetOrangeLineStopID(string stopName)
        {
            string stopID = string.Empty;
            try
            {
                string query = "select StopID from venkat89.OrangeLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    stopID = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return stopID;
            }
            catch (Exception ex)
            {
                return stopID;
            }
        }


        // GetOrangeLineScheduleByRoute : String String String String -> DataSet
        // GIVEN: a routeID, direction, boardingPlace and destinationPlace as arguments
        // RETURNS: the next available train timings in the dataset format
        public DataSet GetOrangeLineScheduleByRoute(string routeID, string direction, string boardingPlace, string destinationPlace)
        {
            List<string> scheduleInfo = new List<string>();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Arrives On");
            dt.Columns.Add("Reaches By");
            try
            {
                // We would be needing the stop order to scan the xml for retrieving the arrival and departure times
                string boardingPlaceStopOrder = GetStopOrder(routeID, direction, boardingPlace);
                string destinationPlaceStopOrder = GetStopOrder(routeID, direction, destinationPlace);

                string baseURL = "http://realtime.mbta.com/developer/api/v2/schedulebyroute?api_key=" + appKey + "&route=" + routeID + "&direction=" + direction;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "stop":
                                    // Zeroth index value holds the stop name in xml
                                    reader.MoveToAttribute(0);
                                    string xmlStopSequence = reader.Value.Trim();
                                    if (xmlStopSequence.Equals(boardingPlaceStopOrder))
                                    {
                                        //Third index value holds the arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    if (xmlStopSequence.Equals(destinationPlaceStopOrder))
                                    {
                                        //Third index value holds the destination arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    break;
                            }
                        }
                    }
                }

                string[] arrayContents = scheduleInfo.ToArray();
                int arrayCounter = 0;
                for (int iRow = 0; iRow < (scheduleInfo.Count / 2); iRow++)
                {
                    dt.Rows.Add();
                    if (iRow == 0)
                    {
                        dt.Rows[iRow][0] = "Arrives - " + boardingPlace + " - By ";
                        dt.Rows[iRow][1] = "Reaches - " + destinationPlace + " - By ";
                        dt.Rows.Add();
                        iRow++;
                    }
                    // Only 2 columns are retrieved from XML which represents the Scheduled Arrival and Scheduled Departure Timings
                    for (int iCol = 0; iCol < 2; iCol++)
                    {
                        dt.Rows[iRow][iCol] = arrayContents[arrayCounter];
                        arrayCounter++;
                    }
                }

                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // CalculateNextAvailableOrangeLineTrain : DataSet String String -> DataSet
        // GIVEN: a dataset containing the train schedule which was pulled from MBTA, boarding and destination place as argument
        // RETURNS: a dataset containing the train schedule which is adjusted based on the server timings
        public DataSet CalculateNextAvailableOrangeLineTrain(DataSet currentScheduleDS, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableTrainStartTime(latestTrainStartTime, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        #endregion


        #region RedLine Train Logics


        // IsStopNameValidInRedLine : DataSet -> DataSet
        // GIVEN: a stop name in red line as argument
        // RETURNS: true iff the stop name is valid and is present in the database
        public bool IsStopNameValidInRedLine(string stopName)
        {
            bool isStopNameValid = false;
            try
            {
                string query = "select StopID from venkat89.RedLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                // Make the flag true iff the record is returned from the SQL DB
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isStopNameValid = true;
                }

                return isStopNameValid;
            }
            catch (Exception ex)
            {
                return isStopNameValid;
            }
        }


        // GetRedLineStopNameForServices : String -> DataSet
        // GIVEN:  takes keywords of stop name as argument
        // RETURNS: the dataset containing stop names which matches with the passed argument
        public DataSet GetRedLineStopNameForServices(string partialStopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.RedLineStopDetails where StopName like '%" + partialStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetRedLineStopCoordinatesForServices : String -> DataSet
        // GIVEN:  takes the stop name as argument
        // RETURNS: the dataset containing stop coordinates which matches with the passed argument
        public DataSet GetRedLineStopCoordinatesForServices(string stopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopLatitude, StopLongitude from venkat89.RedLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetRedLineAllStopDetailsForServices : Void -> DataSet
        // GIVEN:  takes no argument
        // RETURNS: the dataset containing all the red line stop details
        public DataSet GetRedLineAllStopDetailsForServices()
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select * from venkat89.RedLineStopDetails";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetRedLineStopID : String -> String
        // GIVEN: a stopName as argument
        // RETURNS: stopID for the corressponding stopName which was passed as argument
        public string GetRedLineStopID(string stopName)
        {
            string stopID = string.Empty;
            try
            {
                string query = "select StopID from venkat89.RedLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    stopID = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return stopID;
            }
            catch (Exception ex)
            {
                return stopID;
            }
        }


        // GetRedLineRouteID : String -> String
        // GIVEN: a stopID as argument
        // RETURNS: routeID for the corressponding stopID which was passed as argument
        public List<string> GetRedLineRouteID(string stopID)
        {
            List<string> routeIDs = new List<string>();
            try
            {
                string query = "select RouteID from venkat89.RedLineRouteID where StopID = " + stopID;
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    routeIDs.Add(ds.Tables[0].Rows[i][0].ToString().Trim());
                }

                return routeIDs;
            }
            catch (Exception ex)
            {
                return routeIDs;
            }
        }


        // GetRedLineScheduleByRoute : String String String String -> DataSet
        // GIVEN: a routeID, direction, boardingPlace and destinationPlace as arguments
        // RETURNS: the next available train timings in the dataset format
        public DataSet GetRedLineScheduleByRoute(string routeID, string direction, string boardingPlace, string destinationPlace)
        {
            List<string> scheduleInfo = new List<string>();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Arrives On");
            dt.Columns.Add("Reaches By");
            try
            {
                // We would be needing the stop order to scan the xml for retrieving the arrival and departure times
                string boardingPlaceStopOrder = GetStopOrder(routeID, direction, boardingPlace);
                string destinationPlaceStopOrder = GetStopOrder(routeID, direction, destinationPlace);

                string baseURL = "http://realtime.mbta.com/developer/api/v2/schedulebyroute?api_key=" + appKey + "&route=" + routeID + "&direction=" + direction;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "stop":
                                    // Zeroth index value holds the stop name in xml
                                    reader.MoveToAttribute(0);
                                    string xmlStopSequence = reader.Value.Trim();
                                    if (xmlStopSequence.Equals(boardingPlaceStopOrder))
                                    {
                                        //Third index value holds the arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    if (xmlStopSequence.Equals(destinationPlaceStopOrder))
                                    {
                                        //Third index value holds the destination arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    break;
                            }
                        }
                    }
                }

                string[] arrayContents = scheduleInfo.ToArray();
                int arrayCounter = 0;
                for (int iRow = 0; iRow < (scheduleInfo.Count / 2); iRow++)
                {
                    dt.Rows.Add();
                    if (iRow == 0)
                    {
                        dt.Rows[iRow][0] = "Arrives - " + boardingPlace + " - By ";
                        dt.Rows[iRow][1] = "Reaches - " + destinationPlace + " - By ";
                        dt.Rows.Add();
                        iRow++;
                    }
                    // Only 2 columns are retrieved from XML which represents the Scheduled Arrival and Scheduled Departure Timings
                    for (int iCol = 0; iCol < 2; iCol++)
                    {
                        dt.Rows[iRow][iCol] = arrayContents[arrayCounter];
                        arrayCounter++;
                    }
                }

                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // CalculateNextAvailableRedLineTrain : DataSet String String -> DataSet
        // GIVEN: a dataset containing the train schedule which was pulled from MBTA, boarding and destination place as argument
        // RETURNS: a dataset containing the train schedule which is adjusted based on the server timings
        public DataSet CalculateNextAvailableRedLineTrain(DataSet currentScheduleDS, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableTrainStartTime(latestTrainStartTime, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        #endregion


        #region BlueLine Train Logics


        // GetBlueLineStopNameForAutoComplete : Void -> DataSet
        // GIVEN: takes no argument
        // RETURNS: the dataset containing all the stop names in blue line
        public DataSet GetBlueLineStopNameForAutoComplete()
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.BlueLineStopDetails";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // IsStopNameValidInBlueLine : DataSet -> DataSet
        // GIVEN: a stop name in blue line as argument
        // RETURNS: true iff the stop name is valid and is present in the database
        public bool IsStopNameValidInBlueLine(string stopName)
        {
            bool isStopNameValid = false;
            try
            {
                string query = "select StopID from venkat89.BlueLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                // Make the flag true iff the record is returned from the SQL DB
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isStopNameValid = true;
                }

                return isStopNameValid;
            }
            catch (Exception ex)
            {
                return isStopNameValid;
            }
        }


        // GetBlueLineStopNameForServices : String -> DataSet
        // GIVEN:  takes keywords of stop name as argument
        // RETURNS: the dataset containing stop names which matches with the passed argument
        public DataSet GetBlueLineStopNameForServices(string partialStopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.BlueLineStopDetails where StopName like '%" + partialStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetBlueLineStopCoordinatesForServices : String -> DataSet
        // GIVEN:  takes the stop name as argument
        // RETURNS: the dataset containing stop coordinates which matches with the passed argument
        public DataSet GetBlueLineStopCoordinatesForServices(string stopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopLatitude, StopLongitude from venkat89.BlueLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }

        // GetBlueLineAllStopDetailsForServices : Void -> DataSet
        // GIVEN:  takes no argument
        // RETURNS: the dataset containing all the blue line stop details
        public DataSet GetBlueLineAllStopDetailsForServices()
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select * from venkat89.BlueLineStopDetails";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetBlueLineStopID : String -> String
        // GIVEN: a stopName as argument
        // RETURNS: stopID for the corressponding stopName which was passed as argument
        public string GetBlueLineStopID(string stopName)
        {
            string stopID = string.Empty;
            try
            {
                string query = "select StopID from venkat89.BlueLineStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    stopID = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return stopID;
            }
            catch (Exception ex)
            {
                return stopID;
            }
        }


        // GetBlueLineScheduleByRoute : String String String String -> DataSet
        // GIVEN: a routeID, direction, boardingPlace and destinationPlace as arguments
        // RETURNS: the next available train timings in the dataset format
        public DataSet GetBlueLineScheduleByRoute(string routeID, string direction, string boardingPlace, string destinationPlace)
        {
            List<string> scheduleInfo = new List<string>();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Arrives On");
            dt.Columns.Add("Reaches By");
            try
            {
                // We would be needing the stop order to scan the xml for retrieving the arrival and departure times
                string boardingPlaceStopOrder = GetStopOrder(routeID, direction, boardingPlace);
                string destinationPlaceStopOrder = GetStopOrder(routeID, direction, destinationPlace);

                string baseURL = "http://realtime.mbta.com/developer/api/v2/schedulebyroute?api_key=" + appKey + "&route=" + routeID + "&direction=" + direction;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "stop":
                                    // Zeroth index value holds the stop name in xml
                                    reader.MoveToAttribute(0);
                                    string xmlStopSequence = reader.Value.Trim();
                                    if (xmlStopSequence.Equals(boardingPlaceStopOrder))
                                    {
                                        //Third index value holds the arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    if (xmlStopSequence.Equals(destinationPlaceStopOrder))
                                    {
                                        //Third index value holds the destination arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    break;
                            }
                        }
                    }
                }

                string[] arrayContents = scheduleInfo.ToArray();
                int arrayCounter = 0;
                for (int iRow = 0; iRow < (scheduleInfo.Count / 2); iRow++)
                {
                    dt.Rows.Add();
                    if (iRow == 0)
                    {
                        dt.Rows[iRow][0] = "Arrives - " + boardingPlace + " - By ";
                        dt.Rows[iRow][1] = "Reaches - " + destinationPlace + " - By ";
                        dt.Rows.Add();
                        iRow++;
                    }
                    // Only 2 columns are retrieved from XML which represents the Scheduled Arrival and Scheduled Departure Timings
                    for (int iCol = 0; iCol < 2; iCol++)
                    {
                        dt.Rows[iRow][iCol] = arrayContents[arrayCounter];
                        arrayCounter++;
                    }
                }

                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // CalculateNextAvailableBlueLineTrain : DataSet String String -> DataSet
        // GIVEN: a dataset containing the train schedule which was pulled from MBTA, boarding and destination place as argument
        // RETURNS: a dataset containing the train schedule which is adjusted based on the server timings
        public DataSet CalculateNextAvailableBlueLineTrain(DataSet currentScheduleDS, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableTrainStartTime(latestTrainStartTime, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        #endregion


        #region GreenLines Integrated Train Logics


        // IsStopNameValidInGreenLinesIntegrated : string -> bool
        // GIVEN: a stop name in all the green lines as argument
        // RETURNS: true iff the stop name is valid and is present in the database
        public bool IsStopNameValidInGreenLinesIntegrated(string stopName)
        {
            bool isStopNameValid = false;
            try
            {
                string query = "select StopID from venkat89.GreenLinesIntegratedStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                // Make the flag true iff the record is returned from the SQL DB
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isStopNameValid = true;
                }

                return isStopNameValid;
            }
            catch (Exception ex)
            {
                return isStopNameValid;
            }
        }


        // GetGreenLinesIntegratedStopNameForServices : String -> DataSet
        // GIVEN:  takes keywords of stop name as argument
        // RETURNS: the dataset containing stop names which matches with the passed argument
        public DataSet GetGreenLinesIntegratedStopNameForServices(string partialStopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopName from venkat89.GreenLinesIntegratedStopDetails where StopName like '%" + partialStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetGreenLinesIntegratedStopCoordinatesForServices : String -> DataSet
        // GIVEN:  takes the stop name as argument
        // RETURNS: the dataset containing stop coordinates which matches with the passed argument
        public DataSet GetGreenLinesIntegratedStopCoordinatesForServices(string stopName)
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select StopLatitude, StopLongitude from venkat89.GreenLinesIntegratedStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetGreenLinesIntegratedAllStopDetailsForServices : Void -> DataSet
        // GIVEN:  takes no argument
        // RETURNS: the dataset containing all the green line stop details
        public DataSet GetGreenLinesIntegratedAllStopDetailsForServices()
        {
            DataSet ds = new DataSet();
            try
            {
                string query = "select * from venkat89.GreenLinesIntegratedStopDetails";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // GetLineFromGreenLines : String -> String
        // GIVEN: a stopID as argument
        // RETURNS: line (E, D, C, B) for the corressponding stopID which was passed as argument
        public string GetLineFromGreenLines(string stopID)
        {
            string line = string.Empty;
            try
            {
                string query = "select Line from venkat89.GreenLinesIntegratedStopDetails where StopID = " + stopID;
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    line = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return line;
            }
            catch (Exception ex)
            {
                return line;
            }
        }


        // GetGreenLinesIntegratedStopID : String -> String
        // GIVEN: a stopName as argument
        // RETURNS: stopID for the corressponding stopName which was passed as argument
        public string GetGreenLinesIntegratedStopID(string stopName)
        {
            string stopID = string.Empty;
            try
            {
                string query = "select StopID from venkat89.GreenLinesIntegratedStopDetails where StopName = '" + stopName + "'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    stopID = ds.Tables[0].Rows[0][0].ToString().Trim();
                }

                return stopID;
            }
            catch (Exception ex)
            {
                return stopID;
            }
        }


        // GetCopleyStationDetailsFromGreenLinesIntegratedTable : DataSet -> String
        // GIVEN: the train schedule information as argument
        // RETURNS: the first train reaching time to the destination
        public string GetHopStationReachTime(DataSet trainSchedule)
        {
            string reachTime = string.Empty;
            try
            {
                if (trainSchedule.Tables[0].Rows.Count > 1)
                {
                    reachTime = trainSchedule.Tables[0].Rows[1][1].ToString().Trim();
                }
                return reachTime;
            }
            catch (Exception ex)
            {
                return reachTime;
            }
        }


        // GetCopleyStationDetailsFromGreenLinesIntegratedTable : Void -> String[]
        // GIVEN: void
        // RETURNS: the Copley station details in the string array format
        public string[] GetCopleyStationDetailsFromGreenLinesIntegratedTable()
        {
            // The GreenLinesIntegratedStopDetails table has 5 columns which is why I have set the capacity of array to length 5
            string[] stationDetails = new string[5];
            try
            {
                const string copleyStopName = "Copley";
                string query = "select * from venkat89.GreenLinesIntegratedStopDetails where StopName like '" + copleyStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
                    {
                        stationDetails[i] = ds.Tables[0].Rows[0][i].ToString().Trim();
                    }
                }
                return stationDetails;
            }
            catch (Exception ex)
            {
                return stationDetails;
            }
        }


        // GetKenmoreStationDetailsFromGreenLinesIntegratedTable : Void -> String[]
        // GIVEN: void
        // RETURNS: the Kenmore station details in the string array format
        public string[] GetKenmoreStationDetailsFromGreenLinesIntegratedTable()
        {
            // The GreenLinesIntegratedStopDetails table has 5 columns which is why I have set the capacity of array to length 5
            string[] stationDetails = new string[5];
            try
            {
                const string copleyStopName = "Kenmore";
                string query = "select * from venkat89.GreenLinesIntegratedStopDetails where StopName like '" + copleyStopName + "%'";
                SqlConnection connection = EstablishConnectionWithSQLDB();
                DataSet ds = FetchDataFromSQLDB(query, connection);
                KillSQLConnection(connection);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
                    {
                        stationDetails[i] = ds.Tables[0].Rows[0][i].ToString().Trim();
                    }
                }
                return stationDetails;
            }
            catch (Exception ex)
            {
                return stationDetails;
            }
        }


        // GetHopStationDetailsFromGreenLinesIntegrated : String String -> String[]
        // GIVEN: the boarding point stop id and destination point stop id as arguments
        // RETURNS: the hop station details which holds the complete details of the stop
        public string[] GetHopStationDetailsFromGreenLinesIntegrated(string boardingPointID, string destinationPointID)
        {
            // The GreenLinesIntegratedStopDetails table has 5 columns which is why I have set the capacity of array to length 5
            string[] hopStationDetails = new string[5];
            try
            {
                // Retrieve the lines(E, D, C, B) for the selected boarding and destination points
                string boardingPointLine = GetLineFromGreenLines(boardingPointID);
                string destinationPointLine = GetLineFromGreenLines(destinationPointID);

                // If the boarding or destination place is in the green line E then the hop should happen at Copley stop
                if ((boardingPointLine.Equals("E")) || (destinationPointLine.Equals("E")))
                {
                    hopStationDetails = GetCopleyStationDetailsFromGreenLinesIntegratedTable();
                }

                // If the boarding or destination place is not in the green line E then the hop should happen at Kenmore stop
                else
                {
                    hopStationDetails = GetKenmoreStationDetailsFromGreenLinesIntegratedTable();
                }

                return hopStationDetails;
            }
            catch (Exception ex)
            {
                return hopStationDetails;
            }
        }


        // GetGreenLinesIntegratedScheduleByRoute : String String String String -> DataSet
        // GIVEN: a routeID, direction, boardingPlace and destinationPlace as arguments
        // RETURNS: the next available train timings in the dataset format
        public DataSet GetGreenLinesIntegratedScheduleByRoute(string routeID, string direction, string boardingPlace, string destinationPlace)
        {
            List<string> scheduleInfo = new List<string>();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Arrives On");
            dt.Columns.Add("Reaches By");
            try
            {
                // We would be needing the stop order to scan the xml for retrieving the arrival and departure times
                string boardingPlaceStopOrder = GetStopOrder(routeID, direction, boardingPlace);
                string destinationPlaceStopOrder = GetStopOrder(routeID, direction, destinationPlace);

                string baseURL = "http://realtime.mbta.com/developer/api/v2/schedulebyroute?api_key=" + appKey + "&route=" + routeID + "&direction=" + direction;
                string responseXMLContents = PullAPIResponseInXMLStringFormat(baseURL);

                using (StringReader stringReader = new StringReader(responseXMLContents))
                using (XmlTextReader reader = new XmlTextReader(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "stop":
                                    // Zeroth index value holds the stop name in xml
                                    reader.MoveToAttribute(0);
                                    string xmlStopSequence = reader.Value.Trim();
                                    if (xmlStopSequence.Equals(boardingPlaceStopOrder))
                                    {
                                        //Third index value holds the arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    if (xmlStopSequence.Equals(destinationPlaceStopOrder))
                                    {
                                        //Third index value holds the destination arrival time information
                                        reader.MoveToAttribute(3);
                                        string epochTime = reader.Value;
                                        scheduleInfo.Add(ConvertEpochTimeToNormalDateTime(int.Parse(epochTime)));
                                    }

                                    break;
                            }
                        }
                    }
                }

                string[] arrayContents = scheduleInfo.ToArray();
                int arrayCounter = 0;
                for (int iRow = 0; iRow < (scheduleInfo.Count / 2); iRow++)
                {
                    dt.Rows.Add();
                    if (iRow == 0)
                    {
                        dt.Rows[iRow][0] = "Arrives - " + boardingPlace + " - By ";
                        dt.Rows[iRow][1] = "Reaches - " + destinationPlace + " - By ";
                        dt.Rows.Add();
                        iRow++;
                    }
                    // Only 2 columns are retrieved from XML which represents the Scheduled Arrival and Scheduled Departure Timings
                    for (int iCol = 0; iCol < 2; iCol++)
                    {
                        dt.Rows[iRow][iCol] = arrayContents[arrayCounter];
                        arrayCounter++;
                    }
                }

                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                return ds;
            }
        }


        // CalculateNextAvailableGreenLinesIntegratedTrain : DataSet String String -> DataSet
        // GIVEN: a dataset containing the train schedule which was pulled from MBTA, boarding and destination place as argument
        // RETURNS: a dataset containing the train schedule which is adjusted based on the server timings
        public DataSet CalculateNextAvailableGreenLinesIntegratedTrain(DataSet currentScheduleDS, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableTrainStartTime(latestTrainStartTime, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        // CalculateNextAvailableHopGreenLinesIntegratedTrain : DataSet String String String -> DataSet
        // GIVEN: a dataset containing the train schedule which was pulled from MBTA, the arrival time of
        //           the first train to its destination place, boarding(hop station) and destination place as argument
        // RETURNS: a dataset containing the train schedule which is adjusted based on the server timings
        public DataSet CalculateNextAvailableHopGreenLinesIntegratedTrain(DataSet currentScheduleDS, string boardingTrainReachTime, string boardingPlace, string destinationPlace)
        {
            DataSet trainScheduleDS = new DataSet();
            DataTable trainScheduleDT = new DataTable();
            trainScheduleDT = currentScheduleDS.Tables[0].Copy();
            try
            {
                // Atleast three rows should be present in the existing dataset to predict the next arrival train
                // Ignore the first row in data table as it just contains the boarding and destination place name
                int rowCount = currentScheduleDS.Tables[0].Rows.Count;
                if (rowCount >= 3)
                {
                    string firstTrainStartTime = currentScheduleDS.Tables[0].Rows[1][0].ToString();
                    string firstTrainEndTime = currentScheduleDS.Tables[0].Rows[1][1].ToString();

                    // Computing the travel duration from boarding point to the destination point
                    DateTime dt1 = DateTime.ParseExact(firstTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    DateTime dt2 = DateTime.ParseExact(firstTrainEndTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts1 = dt2.Subtract(dt1);
                    string travelDuration = ts1.Minutes.ToString().Trim();


                    // Retrieving the latest train arrival time
                    string latestTrainStartTimeInString = currentScheduleDS.Tables[0].Rows[rowCount - 1][0].ToString();
                    DateTime latestTrainStartTime = DateTime.ParseExact(latestTrainStartTimeInString, "HH:mm", new DateTimeFormatInfo());


                    // Computing the waiting time between latest two trains
                    string latestButOneTrainStartTime = currentScheduleDS.Tables[0].Rows[rowCount - 2][0].ToString();
                    DateTime dt3 = DateTime.ParseExact(latestButOneTrainStartTime, "HH:mm", new DateTimeFormatInfo());
                    TimeSpan ts2 = latestTrainStartTime.Subtract(dt3);
                    string nextTrainGap = ts2.Minutes.ToString().Trim();


                    DateTime boardingTrainReachTimeInDateTimeFormat = DateTime.ParseExact(boardingTrainReachTime, "HH:mm", new DateTimeFormatInfo());
                    // Frame the immediate future train timings
                    DateTime finalTrainTime = ComputeTheFutureAvailableHopTrainStartTime(boardingTrainReachTimeInDateTimeFormat, dt3, nextTrainGap);
                    // Frame the entire immediate future train schedule which includes the start time and journey duration time
                    DataSet finalTrainScheduleDS = FrameTheTrainSchedule(finalTrainTime, travelDuration, nextTrainGap, boardingPlace, destinationPlace);
                    trainScheduleDT.Clear();
                    trainScheduleDT = finalTrainScheduleDS.Tables[0].Copy();
                }
                // Incorporate the below logic to clear the table contents if there are not multiple trains running
                //   in the same route as this method is invoked only for hop journey
                else
                {
                    trainScheduleDT.Clear();
                }

                // Loading all the datatable contents into the dataset
                trainScheduleDS.Tables.Add(trainScheduleDT);
                return trainScheduleDS;
            }
            catch (Exception ex)
            {
                return trainScheduleDS;
            }
        }


        #endregion

    }
}