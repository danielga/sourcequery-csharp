using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SourceQuery
{
    public partial class MasterServer
    {
        private const string _masterServersDomain = "hl2master.steampowered.com";
        private const int _masterServersPort = 27011;
        private const byte A2M_GET_SERVERS_BATCH2 = 0x31; // '1'
        private const byte M2A_SERVER_BATCH = 0x66; // 'f'
        private const byte M2A_SERVER_BATCH_EXTRA = 0x0A; // '\n'

        public static async Task<List<MasterServer>> GetList()
        {
            IPHostEntry hosts = await Dns.GetHostEntryAsync(_masterServersDomain);
            List<MasterServer> masterServers = new List<MasterServer>();
            foreach (IPAddress host in hosts.AddressList)
                masterServers.Add(new MasterServer(new IPEndPoint(host, _masterServersPort)));

            return masterServers;
        }

        private readonly UdpClient _socket;

        private MasterServer(IPEndPoint masterServer)
        {
            _socket = new UdpClient();
            _socket.Connect(masterServer);
        }

        private void WriteStringToBinaryWriter(BinaryWriter writer, string str)
        {
            writer.Write(Encoding.UTF8.GetBytes(str));
            writer.Write((byte)0);
        }

        public async Task<List<IPEndPoint>> GetServerList(Region region, Filter filter)
        {
            List<IPEndPoint> servers = new List<IPEndPoint>();

            string strFilter = filter.ToString();
            string lastAddress = "0.0.0.0:0";

            byte[] octets = new byte[4] { 0, 0, 0, 0 };
            ushort port = 0;
            do
            {
                using (MemoryStream writerStream = new MemoryStream(2 /*initial bytes*/ + 21 /*ip address*/ + 1 + strFilter.Length + 1))
                using (BinaryWriter writer = new BinaryWriter(writerStream))
                {
                    writer.Write(A2M_GET_SERVERS_BATCH2);
                    writer.Write((byte)region);
                    WriteStringToBinaryWriter(writer, lastAddress);
                    WriteStringToBinaryWriter(writer, strFilter);
                    writer.Flush();

                    int length = (int)writerStream.Position;
                    int sent = await _socket.SendAsync(writerStream.GetBuffer(), length);
                    if (sent != length)
                        return null;
                }

                UdpReceiveResult recvRes = await _socket.ReceiveAsync();
                int recvd = recvRes.Buffer.Length;
                if (recvd < 6)
                    return null;

                using MemoryStream readerStream = new MemoryStream(recvRes.Buffer);
                using BinaryReader reader = new BinaryReader(readerStream);

                int header = reader.ReadInt32();
                byte type = reader.ReadByte();
                byte extra = reader.ReadByte();
                if (header != -1 || type != M2A_SERVER_BATCH || extra != M2A_SERVER_BATCH_EXTRA)
                    return null;

                while (readerStream.Position <= readerStream.Capacity - 6)
                {
                    if (reader.Read(octets, 0, 4) != 4)
                        break;

                    byte[] data = reader.ReadBytes(2);
                    Array.Reverse(data);
                    port = BitConverter.ToUInt16(data, 0);

                    if (octets[0] == 0 && octets[1] == 0 && octets[2] == 0 && octets[3] == 0 && port == 0)
                        break;

                    IPEndPoint serverAddress = new IPEndPoint(new IPAddress(octets), port);
                    servers.Add(serverAddress);
                    lastAddress = serverAddress.ToString();
                }
            }
            while (octets[0] != 0 && octets[1] != 0 && octets[2] != 0 && octets[3] != 0 && port != 0);

            return servers;
        }
    }
}
