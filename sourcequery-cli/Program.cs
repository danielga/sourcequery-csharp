using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SourceQuery.CLI
{
    class Program
    {
        private static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                //List<MasterServer> masterServers = await MasterServer.GetList();
                //List<IPEndPoint> servers = await masterServers[0].GetServerList(Region.World, new MasterServer.Filter());

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1]));
                SourceServer server = new SourceServer(endpoint);
                TimeSpan ping = await server.Ping();
                SourceServer.Info info = await server.GetInfo();
                List<SourceServer.Rule> rules = await server.GetRules();
                List<SourceServer.Player> players = await server.GetPlayers();

                /*SteamAPI api = new SteamAPI(args[2]);
                SteamAPI.ServerDetails[] details1 = await api.GetServerSteamIDsByIP(endpoint);
                if (details1 != null)
                {
                    SteamAPI.ServerDetails[] details2 = await api.GetServerIPsBySteamID(details1[0].SteamID);
                    SteamAPI.ServerPublicInfo publicInfo = await api.GetAccountPublicInfo(details1[0].SteamID);
                }

                Console.WriteLine();
            }).Wait();
        }
    }
}
