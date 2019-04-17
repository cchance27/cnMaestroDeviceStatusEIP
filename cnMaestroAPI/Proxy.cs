using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace cnMaestro
{
    public class cnProxy : IHttpHandler
    {
        private static string _apiBaseAddress = ConfigurationManager.AppSettings["cnMaestroApiUrl"];
        private static string _cnMaestroClientID = ConfigurationManager.AppSettings["cnMaestroClientID"];
        private static string _cnMaestroClientSecret = ConfigurationManager.AppSettings["cnMaestroClientSecret"];
        private static string _cnMaestroBearer = ConfigurationManager.AppSettings["cnMaestroBearer"];
        private static string _proxyPath = ".cnmaestro";

        public cnProxy()
        {
            if (String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_apiBaseAddress))
                throw new ArgumentNullException("You must provide  cnMaestroClientID, cnMaestroClientSecret, cnMaestroApiUrl.");

            // We don't have a bearer so let's login
            if (String.IsNullOrEmpty(_cnMaestroBearer))
            {
                GetNewBearer();
            } else
            {
                CheckBearer();
            }
        }

        public void GetNewBearer()
        {
            using (var client = new WebClient())
            {
                try
                {
                    var nv = new NameValueCollection();
                    nv.Add("grant_type", "client_credentials");
                    nv.Add("client_id", _cnMaestroClientID);
                    nv.Add("client_secret", _cnMaestroClientSecret);

                    var response = client.UploadValues(_apiBaseAddress + "/access/token", nv);

                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.Default.GetString(response));
                    _cnMaestroBearer = tokenEndpointDecoded["access_token"];

                    if (String.IsNullOrEmpty(_cnMaestroBearer)) // Something went wrong we didn't get a bearer token
                        throw new WebException("Failed to get a Bearer Token for cnMaestro");
                }
                catch (WebException e)
                {
                    string errorBody = null;
                    using (StreamReader r = new StreamReader(e.Response.GetResponseStream()))
                    {
                        errorBody = r.ReadToEnd();
                    }
                    throw new WebException("cnMaestro GetNewBearer Failed:" + errorBody, e);
                }
            }
        }

        public bool CheckBearer()
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _cnMaestroBearer);
                    var response = client.DownloadString(_apiBaseAddress + "/access/validate_token");

                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                    var expires = tokenEndpointDecoded["expires_in"];
                    if (String.IsNullOrEmpty(expires))
                        GetNewBearer();
                }
                catch (WebException e)
                {
                    string errorBody = null;
                    using (StreamReader r = new StreamReader(e.Response.GetResponseStream()))
                    {
                        errorBody = r.ReadToEnd();
                        if (errorBody.Contains("invalid_token"))
                        {
                            // We're just expired so we got a 401, let's grab a new token.
                            GetNewBearer();
                        } else
                        {
                            throw new WebException("cnMaestro CheckBearer Failed:" + errorBody, e);
                        }
                    }
                }
            }

            // If we make it hear it means we either had a valid expires_in, 
            // or we got a new token as if we failed to get one we throw an exception
            return true; 
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string response = null;
            if (String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_cnMaestroBearer) || String.IsNullOrEmpty(_apiBaseAddress))
                throw new WebException("cnMaestro: Request Process setup failed.");

            // Make sure we still have a valid bearer refresh if needed.
            CheckBearer();

            var macFromPath = context.Request.Url.PathAndQuery.ToLower().Replace(_proxyPath.ToLower(), "");
            if (macFromPath.Contains("/"))
                macFromPath = macFromPath.Substring(macFromPath.LastIndexOf("/") + 1);

            // Check for valid mac if not we send back a json error, similar format to the not found response from api.
            Regex regex = new Regex(@"^(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}$");
            if (regex.Match(macFromPath).Success)
            {

                if (context.Request.RequestType.ToUpper() == "GET")
                {
                    // We're not handling posts only get requests for the Cambium
                    response = GetDevice(macFromPath);
                }
                else
                {
                    throw new NotImplementedException();
                }
            } else
            {
                response = "{\"error\": { \"level\": \"error\", \"cause\": \"Invalid MAC\", \"message\": \"Mac Address is Invalid Format\"} }";
            }
            context.Response.ContentType = "application/json";
            context.Response.Write(response);
        }

        public string GetDevice(string mac)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _cnMaestroBearer);
                    var response = client.DownloadString(_apiBaseAddress + "/devices/" + mac);
                    return response;
                }
                catch (WebException e)
                {
                    string errorBody = null;
                    using (StreamReader r = new StreamReader(e.Response.GetResponseStream()))
                    {
                        errorBody = r.ReadToEnd();
                    }

                    // if it's not a 404 throw the error, if it's a 404 let's just return the JSON error
                    if (((HttpWebResponse)e.Response).StatusCode != HttpStatusCode.NotFound)
                        throw new WebException("cnMaestro GetDevice(" + mac + ") Failed:" + errorBody, e);

                    return errorBody;
                }
            }
        }
    }
}