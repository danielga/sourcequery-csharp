using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Checksum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SourceQuery
{
    public partial class SourceServer
    {
        private static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        private const byte S2C_CHALLENGE = 0x41; // 'A'
        private const byte S2A_PLAYER = 0x44; // 'D'
        private const byte S2A_RULES = 0x45; // 'E'
        private const byte S2A_INFO = 0x49; // 'I'

        private static readonly byte[] A2S_HEADER = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly byte[] A2S_CHALLENGE = A2S_HEADER;

        private static readonly byte[] A2S_INFO_REQUEST = Combine(A2S_HEADER, Encoding.ASCII.GetBytes("TSource Engine Query\0"));

        private const byte A2S_PLAYER = 0x55; // 'U'
        private static readonly byte[] A2S_PLAYER_REQUEST = Combine(A2S_HEADER, new byte[1] { A2S_PLAYER });

        private const byte A2S_RULES = 0x56; // 'V'
        private static readonly byte[] A2S_RULES_REQUEST = Combine(A2S_HEADER, new byte[1] { A2S_RULES });

        private readonly UdpClient _socket;

        public SourceServer(IPEndPoint sourceServer, int timeoutMs)
        {
            _socket = new UdpClient();
            _socket.Connect(sourceServer);

            _socket.Client.ReceiveTimeout = timeoutMs;
            _socket.Client.SendTimeout = timeoutMs;
        }

        public SourceServer(IPEndPoint sourceServer) : this(sourceServer, 5000)
        { }

        private static async Task<UdpReceiveResult> ReceiveAsync(UdpClient client)
        {
            return await Task.Run(() => {
                try
                {
                    IPEndPoint remoteEP = null;
                    byte[] data = client.Receive(ref remoteEP);
                    return new UdpReceiveResult(data, remoteEP);
                }
                catch (SocketException)
                {
                    return new UdpReceiveResult();
                }
            });
        }

        public async Task<TimeSpan> Ping()
        {
            Stopwatch stopwatch = new Stopwatch();

            int sent = await _socket.SendAsync(A2S_INFO_REQUEST, A2S_INFO_REQUEST.Length);
            if (sent != A2S_INFO_REQUEST.Length)
                return TimeSpan.MinValue;

            stopwatch.Start();
            UdpReceiveResult recvRes = await ReceiveAsync(_socket);
            stopwatch.Stop();
            return (recvRes.Buffer != null && recvRes.Buffer.Length > 0) ? stopwatch.Elapsed : TimeSpan.MinValue;
        }

        private async Task<byte[]> ReceivePacket()
        {
            UdpReceiveResult recvRes = await ReceiveAsync(_socket);
            if (recvRes.Buffer == null || recvRes.Buffer.Length <= 0)
                return null;

            int type = BitConverter.ToInt32(recvRes.Buffer, 0);
            if (type == -1)
                return recvRes.Buffer;
            else if (type != -2)
                return null;

            bool compressed = false;
            byte numpackets = recvRes.Buffer[4 /* 0xFFFFFFFE */ + 4 /* requestID */], received = 0;
            uint packet_checksum = 0;
            int totalbytes = 0;
            byte[][] packets = new byte[numpackets][];

            do
            {
                using MemoryStream readerStream = new MemoryStream(recvRes.Buffer);
                readerStream.Seek(4 /* 0xFFFFFFFE */, SeekOrigin.Begin);
                using BinaryReader reader = new BinaryReader(readerStream);

                int requestid = reader.ReadInt32();

                numpackets = reader.ReadByte();
                if (numpackets != packets.Length)
                    break;

                byte numpacket = reader.ReadByte();
                short splitsize = reader.ReadInt16();

                compressed = (requestid & 0x80000000) != 0;

                if (compressed)
                    packet_checksum = reader.ReadUInt32();

                int size = (int)(readerStream.Length - readerStream.Position);
                packets[numpacket] = reader.ReadBytes(size);
                totalbytes += size;

                ++received;
                if (received >= numpackets)
                    break;

                recvRes = await ReceiveAsync(_socket);
                if (recvRes.Buffer.Length <= 0)
                    break;

                type = BitConverter.ToInt32(recvRes.Buffer, 0);
                Debug.Assert(type == -2);
            }
            while (received < numpackets);

            int offset = 0;
            byte[] buffer = new byte[totalbytes];
            for (byte k = 0; k < numpackets; ++k)
            {
                byte[] section = packets[k];
                Buffer.BlockCopy(section, 0, buffer, offset, section.Length);
                offset += section.Length;
            }

            if (compressed)
            {
                {
                    using MemoryStream compressedStream = new MemoryStream(buffer, false);
                    using MemoryStream decompressedStream = new MemoryStream();
                    BZip2.Decompress(compressedStream, decompressedStream, false);
                    buffer = decompressedStream.ToArray();
                }

                BZip2Crc crc = new BZip2Crc();
                crc.Update(buffer);
                if (crc.Value != packet_checksum)
                    return null;
            }

            return buffer;
        }

        private string ReadStringFromBinaryReader(BinaryReader reader)
        {
            StringBuilder builder = new StringBuilder();
            byte b;
            while ((b = reader.ReadByte()) != 0)
                builder.Append((char)b);

            return builder.ToString();
        }

        public async Task<Info> GetInfo()
        {
            int sent = await _socket.SendAsync(A2S_INFO_REQUEST, A2S_INFO_REQUEST.Length);
            if (sent != A2S_INFO_REQUEST.Length)
                return null;

            byte[] buffer = await ReceivePacket();
            if (buffer == null || buffer.Length == 0)
                return null;

            using MemoryStream readerStream = new MemoryStream(buffer);
            using BinaryReader reader = new BinaryReader(readerStream);

            int code = reader.ReadInt32();
            byte type = reader.ReadByte();
            if (code != -1 || type != S2A_INFO)
                return null;

            Info info = new Info
            {
                version = reader.ReadByte(),
                hostname = ReadStringFromBinaryReader(reader),
                map = ReadStringFromBinaryReader(reader),
                game_directory = ReadStringFromBinaryReader(reader),
                game_description = ReadStringFromBinaryReader(reader),
                app_id = reader.ReadInt16(),
                num_players = reader.ReadByte(),
                max_players = reader.ReadByte(),
                num_of_bots = reader.ReadByte(),
                type = (char)reader.ReadByte(),
                os = (char)reader.ReadByte(),
                password = reader.ReadBoolean(),
                secure = reader.ReadBoolean(),
                game_version = ReadStringFromBinaryReader(reader)
            };

            byte edf = reader.ReadByte();
            if (edf == 0)
                return info;

            if ((edf & 0x80) != 0)
                info.port = reader.ReadUInt16();

            if ((edf & 0x10) != 0)
                info.steamid = reader.ReadUInt64();

            if ((edf & 0x40) != 0)
            {
                info.tvport = reader.ReadUInt16();
                info.tvname = ReadStringFromBinaryReader(reader);
            }

            if ((edf & 0x20) != 0)
                info.tags = ReadStringFromBinaryReader(reader);

            if ((edf & 0x01) != 0)
                info.gameid = reader.ReadUInt64();

            return info;
        }

        private async Task<byte[]> ReceivePacketWithChallenge(byte type, byte[] request)
        {
            byte[] packet = Combine(request, A2S_CHALLENGE);
            int sent = await _socket.SendAsync(packet, packet.Length);
            if (sent != packet.Length)
                return null;

            byte[] buffer = await ReceivePacket();
            if (buffer == null || buffer.Length < 5)
                return null;

            int code = BitConverter.ToInt32(buffer, 0);
            byte curtype = buffer[4];
            if (code != -1 || (curtype != S2C_CHALLENGE && curtype != type))
                return null;

            if (curtype == S2C_CHALLENGE)
            {
                if (buffer.Length < 9)
                    return null;

                byte[] challenge = buffer.AsMemory(5, 4).ToArray();
                packet = Combine(request, challenge);
                sent = await _socket.SendAsync(packet, packet.Length);
                if (sent != packet.Length)
                    return null;

                buffer = await ReceivePacket();
                if (buffer == null || buffer.Length < 5)
                    return null;

                code = BitConverter.ToInt32(buffer, 0);
                curtype = buffer[4];
                if (code != -1 || curtype != type)
                    return null;
            }

            return buffer;
        }

        public async Task<List<Rule>> GetRules()
        {
            byte[] buffer = await ReceivePacketWithChallenge(S2A_RULES, A2S_RULES_REQUEST);
            if (buffer == null)
                return null;

            using MemoryStream readerStream = new MemoryStream(buffer);
            readerStream.Seek(4 /* 0xFFFFFFFF */ + 1 /* S2A_RULES */, SeekOrigin.Begin);
            using BinaryReader reader = new BinaryReader(readerStream);

            ushort numrules = reader.ReadUInt16();
            List<Rule> rules = new List<Rule>();
            for (ushort k = 0; k < numrules; ++k)
            {
                Rule rule = new Rule
                {
                    name = ReadStringFromBinaryReader(reader),
                    value = ReadStringFromBinaryReader(reader)
                };
                rules.Add(rule);
            }

            return rules;
        }

        public async Task<List<Player>> GetPlayers()
        {
            byte[] buffer = await ReceivePacketWithChallenge(S2A_PLAYER, A2S_PLAYER_REQUEST);
            if (buffer == null)
                return null;

            using MemoryStream readerStream = new MemoryStream(buffer);
            readerStream.Seek(4 /* 0xFFFFFFFF */ + 1 /* S2A_PLAYER */, SeekOrigin.Begin);
            using BinaryReader reader = new BinaryReader(readerStream);

            byte numplayers = reader.ReadByte();
            List<Player> players = new List<Player>();
            for (byte k = 0; k < numplayers; ++k)
            {
                Player player = new Player
                {
                    index = reader.ReadByte(),
                    player_name = ReadStringFromBinaryReader(reader),
                    kills = reader.ReadInt32(),
                    time_connected = reader.ReadSingle()
                };
                players.Add(player);
            }

            return players;
        }
    }
}
