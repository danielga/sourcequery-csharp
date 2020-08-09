namespace SourceQuery
{
    public partial class SourceServer
    {
        public class Info
        {
            public byte version;
            public string hostname;
            public string map;
            public string game_directory;
            public string game_description;
            public short app_id;
            public byte num_players;
            public byte max_players;
            public byte num_of_bots;
            public char type;
            public char os;
            public bool password;
            public bool secure;
            public string game_version;
            public ushort port;
            public ulong steamid;
            public ushort tvport;
            public string tvname;
            public string tags;
            public ulong gameid;
        };
    }
}
