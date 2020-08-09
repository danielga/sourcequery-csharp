using System.Net;

namespace SourceQuery
{
    public partial class MasterServer
    {
        public class Filter
        {
            public bool DedicatedOnly = false;
            public bool SecureOnly = false;
            public string GameDirectory;
            public string Map;
            public bool LinuxOnly = false;
            public bool AllowEmpty = true;
            public bool AllowFull = true;
            public bool SpectatorOnly = false;
            public uint AppID = 0;
            public uint NotAppID = 0;
            public bool EmptyOnly = false;
            public bool WhitelistedOnly = false;
            public string Tags;
            public string HiddenTagsAll;
            public string HiddenTagsAny;
            public string ServerName;
            public string Version;
            public bool OneResultPerAddress = false;
            public IPEndPoint ResultsFromAddressOnly;
            public string FilterOutAny;
            public string FilterOutAll;

            public override string ToString()
            {
                string filter = "";

                if (DedicatedOnly)
                    filter += "\\type\\d";

                if (SecureOnly)
                    filter += "\\secure\\1";

                if (!string.IsNullOrEmpty(GameDirectory))
                {
                    filter += "\\gamedir\\";
                    filter += GameDirectory;
                }

                if (!string.IsNullOrEmpty(Map))
                {
                    filter += "\\map\\";
                    filter += Map;
                }

                if (LinuxOnly)
                    filter += "\\linux\\1";

                if (AllowEmpty)
                    filter += "\\empty\\1";

                if (AllowFull)
                    filter += "\\full\\1";

                if (SpectatorOnly)
                    filter += "\\proxy\\1";

                if (AppID != 0)
                {
                    filter += "\\appid\\";
                    filter += AppID;
                }

                if (NotAppID != 0)
                {
                    filter += "\\napp\\";
                    filter += NotAppID;
                }

                if (EmptyOnly)
                    filter += "\\noplayers\\1";

                if (WhitelistedOnly)
                    filter += "\\white\\1";

                if (!string.IsNullOrEmpty(Tags))
                {
                    filter += "\\gametype\\";
                    filter += Tags;
                }

                if (!string.IsNullOrEmpty(HiddenTagsAll))
                {
                    filter += "\\gamedata\\";
                    filter += HiddenTagsAll;
                }

                if (!string.IsNullOrEmpty(HiddenTagsAny))
                {
                    filter += "\\gamedataor\\";
                    filter += HiddenTagsAny;
                }

                if (!string.IsNullOrEmpty(ServerName))
                {
                    filter += "\\name_match\\";
                    filter += ServerName;
                }

                if (!string.IsNullOrEmpty(Version))
                {
                    filter += "\\version_match\\";
                    filter += Version;
                }

                if (OneResultPerAddress)
                    filter += "\\collapse_addr_hash\\1";

                if (ResultsFromAddressOnly != null)
                {
                    filter += "\\gameaddr\\";

                    if (ResultsFromAddressOnly.Port == 0)
                        filter += ResultsFromAddressOnly.Address.ToString();
                    else
                        filter += ResultsFromAddressOnly.ToString();
                }

                if (!string.IsNullOrEmpty(FilterOutAny))
                {
                    filter += "\\nor\\";
                    filter += FilterOutAny;
                }

                if (!string.IsNullOrEmpty(FilterOutAll))
                {
                    filter += "\\nand\\";
                    filter += FilterOutAll;
                }

                return filter;
            }
        }
    }
}
