using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;

namespace NearRestaurants
{
    public partial class Default : System.Web.UI.Page
    {
        static string restDatabase = "http://cuonly.cs.cornell.edu/Courses/CS5412/2015sp/_cuonly/restaurants_all.csv";

        static string googleAddressURL = "https://maps.googleapis.com/maps/api/geocode/json?address=";
        static string googleLatLongURL = "https://maps.googleapis.com/maps/api/geocode/json?latlng=";
        static string googleApiKey = "AIzaSyBaey0c8jLN07Wh2LaDe-RDSVSBt56J1V4";
        static bool useGoogleKey = false;

        static Dictionary<string, List<string>> zipCodes = new Dictionary<string, List<string>>();
        static List<string> nearbyRestaurants;

        static bool userAddressEntry;
        static double radiusInMiles;
        static string addressBoxServer;
        static string gpsLatBoxServer;
        static string gpsLongBoxServer;
        static latLong userRequest;

        static System.TimeSpan timeTaken;

        static List<string> queryResults;
        static string errorLog;

        /*
         * Class to hold the details of the user input
         */
        public class latLong
        {
            public double latitude;
            public double longitude;
            public double latitudeR;
            public double longitudeR;
            public List<String> userZipCodes;

            public latLong(string latitude, string longitude)
            {
                this.latitude = Double.Parse(latitude);
                this.longitude = Double.Parse(longitude);
                this.latitudeR = Math.PI * (Double.Parse(latitude)) / 180;
                this.longitudeR = Math.PI * (Double.Parse(longitude)) / 180;
                userZipCodes = new List<String>();
            }

        }

        /*
         * Class to hold the response from the Google Geocoding API
         */
        public class GoogleGeoCodeResponse
        {

            public string status;
            public results[] results;

        }

        public class results
        {
            public string formatted_address;
            public geometry geometry;
            public string[] types;
            public address_component[] address_components;
        }

        public class geometry
        {
            public string location_type;
            public location location;
        }

        public class location
        {
            public string lat;
            public string lng;
        }

        public class address_component
        {
            public string long_name;
            public string short_name;
            public string[] types;
        }

        /*
         * Method to get the restaurant database into in memory
         */
        public static void getCSV()
        {
            try
            {
                //Http web request for the url which contains the restuarant details
                HttpWebRequest csvWebRequest = (HttpWebRequest)HttpWebRequest.Create(new Uri(restDatabase));

                if ((csvWebRequest.GetResponse().ContentLength > 0))
                {
                    //Create a IO Streamreader to read the data row by row
                    System.IO.StreamReader rowReader = new System.IO.StreamReader(csvWebRequest.GetResponse().GetResponseStream());
                    String csvRow;
                    //Do until the end of rows 
                    while ((csvRow = rowReader.ReadLine()) != null)
                    {
                        //Each row is comma seperated. So split the data by ','
                        string[] csvElements = csvRow.Split(',');

                        //zip code is the Key for storing the list of restaurants in the dictionary
                        string zipCode = csvElements[7];
                        List<string> restaurants;
                        if (zipCodes.TryGetValue(zipCode, out restaurants))
                        {
                            restaurants.Add(csvElements[3] + " " + csvElements[4] + " " +
                                csvElements[5] + " " + csvElements[6] + " " + csvElements[7]);
                            zipCodes[zipCode] = restaurants;
                        }
                        else //If the zipcode is already present in the dictionary, then just append the restaurant to the list. 
                        {
                            restaurants = new List<string>();
                            restaurants.Add(csvElements[3] + " " + csvElements[4] + " " +
                                csvElements[5] + " " + csvElements[6] + " " + csvElements[7]);
                            zipCodes[zipCode] = restaurants;
                        }
                    }
                    //Close the reader at the end of the each read
                    if (rowReader != null) rowReader.Close();
                }
            }
            catch (WebException ex)
            {
                errorLog = errorLog + "</br>Could not fetch the csv file";
            }
        }

        /*
         * Method to get all the user query information.  
        */
        public static void getUserRequest()
        {
            try
            {

                userRequest = null;
                if (userAddressEntry == true && addressBoxServer != "") //If the address is entered
                {
                    string urlAddress = googleAddressURL + Uri.EscapeDataString(addressBoxServer);
                    if (useGoogleKey == true)
                    {
                        urlAddress = urlAddress + "&key=" + googleApiKey;
                    }
                    //Get the latitude and longitude of the address entered.
                    var result = new System.Net.WebClient().DownloadString(urlAddress);
                    GoogleGeoCodeResponse response = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(result);
                    if (response.status == "OK")
                    {
                        userRequest = new latLong(response.results[0].geometry.location.lat, response.results[0].geometry.location.lng);
                    }
                    else
                    {
                        errorLog = errorLog + "</br> Google Geo Code Response: " + response.status;
                    }
                }
                else if (userAddressEntry == false && gpsLatBoxServer != "" && gpsLongBoxServer != "") //If the GPS coordinates are entered. 
                {
                    userRequest = new latLong(gpsLatBoxServer.ToString(), gpsLongBoxServer.ToString());
                }

                //Get the zipcode of the user entered location. 
                if (userRequest != null)
                {
                    string urlLatLong = googleLatLongURL + userRequest.latitude + "," + userRequest.longitude;
                    if (useGoogleKey == true)
                    {
                        urlLatLong = urlLatLong + "&key=" + googleApiKey;
                    }
                    var resultZipCodes = new System.Net.WebClient().DownloadString(urlLatLong);
                    var response = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(resultZipCodes);
                    if (response.status == "OK")
                    {
                        address_component[] address_components = response.results[0].address_components;
                        foreach (address_component add_comp in address_components)
                        {
                            //Zip code is present in the type "postal_code"
                            if (add_comp.types[0] == "postal_code")
                            {
                                userRequest.userZipCodes.Add(add_comp.long_name);
                            }
                        }
                    }
                    else
                    {
                        errorLog = errorLog + "</br>" + response.status;
                    }
                }
            }
            catch (WebException ex)
            {
                errorLog = errorLog + "</br>" + ex.Message;
            }
        }

        /*
         * Method to dfind the nearby restaurants. 
         */
        public void getRestaurants()
        {
            queryResults = new List<string>();
            nearbyRestaurants = new List<string>();
            List<string> restaurants = new List<string>();
            //Get only those retuarants which are in the zip code of the user query.
            foreach (string zipCode in userRequest.userZipCodes)
            {
                List<string> rests;
                if (zipCodes.TryGetValue(zipCode, out rests))
                {
                    restaurants.AddRange(zipCodes[zipCode]);
                }
            }
            //Check if each of the restuarant in the list falls in the given radius
            foreach (string restaurant in restaurants)
            {
                Stopwatch swR = new Stopwatch();
                swR.Start();
                getDistance(restaurant);
                swR.Stop();
                double timeElapsed = swR.Elapsed.TotalMilliseconds;
                if (timeElapsed < 200)
                {
                    Thread.Sleep((200 - (int)Math.Ceiling(timeElapsed)));
                }
            }
        }


        public static void getDistance(string restaurant)
        {
            try
            {
                //Get the latitude and longtitude of the user
                string urlAddress = googleAddressURL + Uri.EscapeDataString(restaurant);
                if (useGoogleKey == true)
                {
                    urlAddress = urlAddress + "&key=" + googleApiKey;
                }
                var result = new System.Net.WebClient().DownloadString(urlAddress);
                GoogleGeoCodeResponse response = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(result);
                if (response.status == "OK")
                {
                    double lat1 = Double.Parse(response.results[0].geometry.location.lat);
                    double long1 = Double.Parse(response.results[0].geometry.location.lng);
                    double latR1 = Math.PI * (lat1) / 180;
                    double longR1 = Math.PI * (long1) / 180;
                    //Calculate the distance between restaurant and user query
                    double distance = Math.Acos(Math.Sin(latR1) * Math.Sin(userRequest.latitudeR)
                        + Math.Cos(latR1) * Math.Cos(userRequest.latitudeR) * Math.Cos(longR1 - userRequest.longitudeR)) * 3963.1676;
                    //Add to the final list of restaurants if the distance is less than the radius mentioned 
                    if (distance <= radiusInMiles)
                    {
                        queryResults.Add(restaurant + " | Distance:" + distance + " Miles");
                        distance = Math.Truncate(100 * distance) / 100;
                        nearbyRestaurants.Add(restaurant + "," + lat1.ToString() + "," + long1.ToString() + ", | Distance:" + distance + " Miles");
                    }

                }
            }
            catch (WebException ex)
            {
                errorLog = errorLog + "</br>" + ex.Message;
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Stopwatch swO = new Stopwatch();
            swO.Start();
            string value = distanceRadiusBox.Text.ToString();
            if (value != "")
            {
                //Get the data from the user form
                userAddressEntry = addressEntry.Checked;
                addressBoxServer = addressBox.Text.ToString();
                gpsLatBoxServer = gpsLatBox.Text.ToString();
                gpsLongBoxServer = gpsLongBox.Text.ToString();
                radiusInMiles = Double.Parse(value);
                addressBox.Text = "";
                gpsLatBox.Text = "";
                gpsLongBox.Text = "";
                distanceRadiusBox.Text = "";
                //Get all the required information from user query
                getUserRequest();
                if (userRequest != null)
                {
                    if (zipCodes.Count == 0)
                    {
                        //Fetch the restaurant database and store in memory
                        getCSV();
                    }
                    if (zipCodes.Count > 0)
                    {
                        //Find the restaurants which fall in the given radius
                        getRestaurants();

                        //Insert the data into hidden div elements for the output display
                        hiddenMarkers.InnerHtml = "";
                        foreach (string restaurant in nearbyRestaurants)
                        {
                            hiddenMarkers.InnerHtml = hiddenMarkers.InnerHtml + ";" + restaurant;
                        }

                        hiddenLatitude.InnerHtml = userRequest.latitude.ToString();
                        hiddenLongitude.InnerHtml = userRequest.longitude.ToString();
                        hiddenRadius.InnerHtml = radiusInMiles.ToString();
                    }
                }
                swO.Stop();
                /*
                // WriteAllLines creates a file, writes a collection of strings to the file, 
                // and then closes the file.
                System.IO.File.WriteAllLines(@"C:\Nixie\Dropbox\Programming\Visual Studio\NearRestaurants\queryResults.txt", queryResults);
                 */
                int nearbyRestaurantsCount;
                if (nearbyRestaurants != null)
                {
                    nearbyRestaurantsCount = nearbyRestaurants.Count;
                }
                else
                {
                    nearbyRestaurantsCount = 0;
                }

                timeTaken = swO.Elapsed;
                runDetails.InnerHtml = "Total number of restaurants: " + nearbyRestaurantsCount.ToString() + "</br>Total time taken: "
                    + timeTaken.TotalMilliseconds.ToString() + " milliseconds";
                if (errorLog != null)
                {
                    runDetails.InnerHtml = runDetails.InnerHtml + "</br>Error Log: " + errorLog;
                }

            }

        }

    }
}
