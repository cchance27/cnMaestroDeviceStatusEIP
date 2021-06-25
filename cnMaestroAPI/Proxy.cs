using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using Newtonsoft.Json;

namespace cnMaestro
{
    public class API : IHttpHandler, IRequiresSessionState
    {
        private static string _apiBaseAddress = ConfigurationManager.AppSettings["cnMaestroApiUrl"];
        private static string _cnMaestroClientID = ConfigurationManager.AppSettings["cnMaestroClientID"];
        private static string _cnMaestroClientSecret = ConfigurationManager.AppSettings["cnMaestroClientSecret"];
        private static string _proxyPath = ".cnmaestro";
        private static string _cnMaestroBearer = "";

        public API()
        {
            if (String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_cnMaestroClientSecret) || String.IsNullOrEmpty(_apiBaseAddress))
                throw new ArgumentNullException("You must provide  cnMaestroClientID, cnMaestroClientSecret, cnMaestroApiUrl.");

            
            // We don't have a bearer so let's login
            if (String.IsNullOrEmpty(_cnMaestroBearer))
            {
                GetNewBearer();
            } 
            else
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
            eipResponse response;
            if (context is null || context.Session is null)
            {
                throw new ArgumentNullException(nameof(context), "The context or session was empty!");
            }
#if !DEBUG // We will ignore UserID EIP Login Check if we're in debug build.
            if (context.Session["UserID"] is null)
            {
                throw new UnauthorizedAccessException("You must be logged into EngageIP to use the cnMaestroAPI Endpoint.");
            }
#endif

            string macFromPath = context.Request.Url.Segments[context.Request.Url.Segments.Length - 1].ToLower().Replace(_proxyPath.ToLower(), "");

            // Check for valid mac if not we send back a json error, similar format to the not found response from api.
            Regex regex = new Regex(@"^(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}$");
            if (regex.Match(macFromPath).Success)
            {
                if (context.Request.RequestType.ToUpper() == "GET")
                {
                    // Our request was a valid mac address format, and a get request so we can serve it.
                    response = GetDevice(macFromPath, true);
                }
                else
                {
                    // We don't handle anything except get requests
                    throw new NotImplementedException();
                }
            } else
            {
                // Invalid Mac Format
                throw new ArgumentOutOfRangeException(nameof(macFromPath));
            }
            context.Response.ContentType = "application/json";
            context.Response.Write(JsonConvert.SerializeObject(response));
        }

        public eipResponse GetDevice(string mac, bool allowRetry = true)
        {
            var client = new WebClient();
            try
            {
                client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _cnMaestroBearer);
                var response = "";

                response = client.DownloadString($"{_apiBaseAddress}/devices/{mac}?fields=product%2Ctower%2Cname%2Csoftware_version%2Cstatus%2Cstatus_time%2Cip");
                apiResponse<apiDevice> device = JsonConvert.DeserializeObject<apiResponse<apiDevice>>(response);

                response = client.DownloadString($"{_apiBaseAddress}/devices/{mac}/statistics?fields=radio.dl_snr_v%2Cradio.dl_snr_h%2Cradio.ul_snr_v%2Cradio.ul_snr_h%2Cradio.ul_rssi%2Cradio.dl_rssi%2Cradio.dl_modulation%2Cradio.ul_modulation%2Cradio.ul_lqi%2Cradio.dl_lqi");
                apiResponse<apiStatistics> statistics = JsonConvert.DeserializeObject<apiResponse<apiStatistics>>(response);

                return new eipResponse() { device = device.data[0], statistics = statistics.data[0].radio };
            }
            catch (WebException e)
            {
                string errorBody = null;
                using (StreamReader r = new StreamReader(e.Response.GetResponseStream()))
                {
                    errorBody = r.ReadToEnd();
                }

                if (!errorBody.Contains("invalid_token"))
                {
                    // We got something besides an invalid token we can't handle so just fail.
                    throw new WebException("cnMaestro GetDevice(" + mac + ") Failed:" + errorBody, e);
                }
            }
            finally
            {
                client.Dispose();
            }

            // If we reach here, we didn't succeed, and we got an error with invalid_token in the body
            if (allowRetry)
            {
                // We're set to allow retries, so let's try again, but we won't allow a retry this next time.
                var response = GetDevice(mac, false);
                return response;
            }
            else
            {
                // We're not allowed to retry, throw an exception.
                throw new WebException("cnMaestro Invalid Token, and we can't retry further.");
            }
        }
    }
}