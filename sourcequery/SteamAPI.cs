using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SourceQuery
{
    public class SteamAPI
    {
        public class ServerDetails
        {
            [JsonProperty("addr")]
            public IPEndPoint Address;
            [JsonProperty("steamid")]
            public string SteamID;
        }

        public class ServerPublicInfo
        {
            [JsonProperty("steamid")]
            public string SteamID;
            [JsonProperty("appid")]
            public int AppID;
        }

        private static readonly Uri GetServerSteamIDsByIPEndpoint = new Uri("https://api.steampowered.com/IGameServersService/GetServerSteamIDsByIP/v1/");
        private static readonly Uri GetServerIPsBySteamIDEndpoint = new Uri("https://api.steampowered.com/IGameServersService/GetServerIPsBySteamID/v1/");
        private static readonly Uri GetAccountPublicInfoEndpoint = new Uri("https://api.steampowered.com/IGameServersService/GetAccountPublicInfo/v1/");
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly string _apiKey;

        public SteamAPI(string apiKey)
        {
            _apiKey = apiKey;
        }

        private struct ServerIPs
        {
            public IPEndPoint[] server_ips;
        }

        private struct ServerSteamIDs
        {
            public string[] server_steamids;
        }

        private struct ServerDetailsList
        {
#pragma warning disable 0649
            // This field is assigned to by JSON deserialization
            public ServerDetails[] servers;
#pragma warning restore 0649
        }

        private struct APIResponse<ResponseType>
        {
#pragma warning disable 0649
            // This field is assigned to by JSON deserialization
            public ResponseType response;
#pragma warning restore 0649
        }

        public async Task<ServerDetails[]> GetServerSteamIDsByIP(IPEndPoint[] addresses)
        {
            string input_json = JsonConvert.SerializeObject(new ServerIPs
            {
                server_ips = addresses
            }, new IPEndPointJsonConverter());
            Uri uri = new Uri(GetServerSteamIDsByIPEndpoint, "?key=" + _apiKey + "&input_json=" + input_json);
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return null;

            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<APIResponse<ServerDetailsList>>(content, new IPEndPointJsonConverter()).response.servers;
        }

        public async Task<ServerDetails[]> GetServerSteamIDsByIP(IPEndPoint address)
        {
            return await GetServerSteamIDsByIP(new IPEndPoint[1] { address });
        }

        public async Task<ServerDetails[]> GetServerIPsBySteamID(string[] steamIDs)
        {
            string input_json = JsonConvert.SerializeObject(new ServerSteamIDs
            {
                server_steamids = steamIDs
            });
            Uri uri = new Uri(GetServerIPsBySteamIDEndpoint, "?key=" + _apiKey + "&input_json=" + input_json);
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return null;

            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<APIResponse<ServerDetailsList>>(content, new IPEndPointJsonConverter()).response.servers;
        }

        public async Task<ServerDetails[]> GetServerIPsBySteamID(string steamID)
        {
            return await GetServerIPsBySteamID(new string[1] { steamID });
        }

        public async Task<ServerPublicInfo> GetAccountPublicInfo(string steamID)
        {
            Uri uri = new Uri(GetAccountPublicInfoEndpoint, "?key=" + _apiKey + "&steamid=" + steamID);
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return null;

            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<APIResponse<ServerPublicInfo>>(content).response;
        }
    }
}
