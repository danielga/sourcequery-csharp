using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SourceQuery
{
    public class SteamID
    {
        /// <summary>
        /// Steam account types
        /// </summary>
        public enum EAccountType : byte
        {
            Invalid,
            Individual,       // single user account
            Multiseat,        // multiseat (e.g. cybercafe) account
            GameServer,       // game server account
            AnonGameServer,   // anonymous game server account
            Pending,          // pending
            ContentServer,    // content server
            Clan,
            Chat,
            ConsoleUser,      // Fake SteamID for local PSN account on PS3 or Live account on 360, etc.
            AnonUser,

            // Max of 16 items in this field
            Max
        };

        private static readonly Dictionary<EAccountType, char> AccountTypeToCharacterMap = new Dictionary<EAccountType, char>
        {
            { EAccountType.AnonGameServer, 'A' },
            { EAccountType.GameServer, 'G' },
            { EAccountType.Multiseat, 'M' },
            { EAccountType.Pending, 'P' },
            { EAccountType.ContentServer, 'C' },
            { EAccountType.Clan, 'g' },
            { EAccountType.Chat, 'T' }, // Lobby chat is 'L', Clan chat is 'c'
            { EAccountType.Invalid, 'I' }, // 'i' is also Invalid
            { EAccountType.Individual, 'U' },
            { EAccountType.AnonUser, 'a' },
        };

        private static readonly Dictionary<char, EAccountType> CharacterToAccountTypeMap = new Dictionary<char, EAccountType>
        {
            { 'i', EAccountType.Invalid },
            { 'I', EAccountType.Invalid },
            { 'U', EAccountType.Individual },
            { 'M', EAccountType.Multiseat },
            { 'G', EAccountType.GameServer },
            { 'A', EAccountType.AnonGameServer },
            { 'P', EAccountType.Pending },
            { 'C', EAccountType.ContentServer },
            { 'g', EAccountType.Clan },
            { 'T', EAccountType.Chat },
            { 'L', EAccountType.Chat },
            { 'c', EAccountType.Chat },
            { 'a', EAccountType.AnonUser }
        };

        /// <summary>
        /// Steam universes. Each universe is a self-contained Steam instance.
        /// </summary>
        public enum EUniverse : byte
        {
            Invalid,
            Public,
            Beta,
            Internal,
            Dev,

            // RC, // no such universe anymore

            Max
        };

        /// <summary>
        /// Steam allow 3 simultaneous user account instances right now, 1 = desktop, 2 = console, 4 = web, 0 = all
        /// </summary>
        private enum EUserInstance : uint
        {
            All = 0,
            Desktop = 1,
            Console = 2,
            Web = 4,
            Max
        };

        private const uint AccountIDOffset = 0;
        private const ulong AccountIDMask = 0xFFFFFFFFUL;

        private const uint AccountInstanceOffset = 32;
        private const ulong AccountInstanceMask = 0xFFFFFUL;

        private const uint AccountTypeOffset = 52;
        private const ulong AccountTypeMask = 0xFUL;

        private const uint AccountUniverseOffset = 56;
        private const ulong AccountUniverseMask = 0xFFUL;

        /// <summary>
        /// Special flags for Chat accounts - they go in the top 8 bits of the steam ID's "instance", leaving 12 for the actual instances
        /// </summary>
        public enum EChatInstanceFlag : uint
        {
            Mask = 0xFFFU, // top 8 bits are flags

            Clan = (Mask + 1) >> 1, // top bit
            Lobby = (Mask + 1) >> 2, // next one down, etc
            MMSLobby = (Mask + 1) >> 3, // next one down, etc

            // Max of 8 flags
        };

        private static readonly Regex Steam2Regex = new Regex(@"^(?:STEAM_)?(?:1|[0-4]:(?<authserver>[0-1])):(?<accountid>\d{1,10})$", RegexOptions.Compiled);
        private static readonly Regex Steam3StrictRegex = new Regex(@"^(?:(?<type>A)[:\-]?(?:(?<universe>[0-4])[:\-])?(?<account>\d+)(?:[:\-](?<instance>\d{1,10})|\((?<instance>\d{1,10})\))?|(?:(?<type>[GMPCgcLTIUai])[:\-]?)?(?:(?<universe>[0-4])[:\-])?(?<account>\d+))$", RegexOptions.Compiled);
        private static readonly Regex Steam3LooseRegex = new Regex(@"^\[?(?:(?<type>A)[:\-]?(?:(?<universe>[0-4])[:\-])?(?<account>\d+)(?:(?::|\()(?<instance>\d{1,10})\)?)?|(?:(?<type>[GMPCgcLTIUai])[:\-]?)?\[?(?:(?<universe>[0-4])[:\-])?(?<account>\d+)\]?)\]?$", RegexOptions.Compiled);

        private readonly BitVector64 steamID = new BitVector64();

        /// <summary>
        /// Unique account identifier
        /// </summary>
        public uint AccountID
        {
            get => (uint)steamID[AccountIDOffset, AccountIDMask];
            set => steamID[AccountIDOffset, AccountIDMask] = value;
        }

        /// <summary>
        /// Dynamic instance ID
        /// </summary>
        public uint AccountInstance
        {
            get => (uint)steamID[AccountInstanceOffset, AccountInstanceMask];
            set => steamID[AccountInstanceOffset, AccountInstanceMask] = value;
        }

        /// <summary>
        /// Type of account
        /// </summary>
        public EAccountType AccountType
        {
            get => (EAccountType)steamID[AccountTypeOffset, AccountTypeMask];
            set => steamID[AccountTypeOffset, AccountTypeMask] = (ulong)value;
        }

        /// <summary>
        /// Universe this account belongs to
        /// </summary>
        public EUniverse AccountUniverse
        {
            get => (EUniverse)steamID[AccountUniverseOffset, AccountUniverseMask];
            set => steamID[AccountUniverseOffset, AccountUniverseMask] = (ulong)value;
        }

        public SteamID() { }

        public SteamID(string strSteamID, EUniverse defUniverse = EUniverse.Public)
        {
            if (!SetFromString(strSteamID, defUniverse))
            {
                throw new FormatException("Unknown SteamID format");
            }
        }

        /// <summary>
        /// Set SteamID from a Steam2 formatted string
        /// </summary>
        /// <remarks>
        /// The STEAM_ prefix is optional.
        /// Only desktop instances and individual accounts are known by Steam2.
        /// </remarks>
        public bool SetFromSteam2String(string strSteamID, EUniverse universe = EUniverse.Public)
        {
            if (string.IsNullOrEmpty(strSteamID) || universe == EUniverse.Invalid || universe >= EUniverse.Max)
            {
                return false;
            }

            Match m = Steam2Regex.Match(strSteamID);
            if (!m.Success)
            {
                return false;
            }

            if (!uint.TryParse(m.Groups["accountid"].Value, out uint accId))
            {
                return false;
            }

            uint authServer = 0;
            Group authserverGroup = m.Groups["authserver"];
            if (authserverGroup.Success)
            {
                if (!uint.TryParse(authserverGroup.Value, out authServer))
                {
                    return false;
                }

                accId <<= 1;
            }

            AccountID = accId | authServer;
            AccountInstance = (uint)EUserInstance.Desktop; // Steam2 only knew desktop instances
            AccountType = EAccountType.Individual; // Steam2 accounts always map to account type of individual
            AccountUniverse = universe;

            return true;
        }

        private bool SetFromStringCommon(Match m, EUniverse defUniverse)
        {
            if (!ulong.TryParse(m.Groups["account"].Value, out ulong account))
            {
                return false;
            }

            EUniverse universe = defUniverse;
            Group universeGroup = m.Groups["universe"];
            if (universeGroup.Success)
            {
                if (!byte.TryParse(universeGroup.Value, out byte tmpUniverse) || tmpUniverse >= (byte)EUniverse.Max)
                {
                    return false;
                }

                universe = (EUniverse)tmpUniverse;
                if (universe == EUniverse.Invalid)
                {
                    universe = defUniverse;
                }
            }

            char charType = '\0';
            EAccountType type = EAccountType.Individual;
            Group typeGroup = m.Groups["type"];
            if (typeGroup.Success)
            {
                charType = typeGroup.Value[0];
                if (!CharacterToAccountTypeMap.ContainsKey(charType))
                {
                    return false;
                }

                type = CharacterToAccountTypeMap[charType];
            }

            uint instance = 1;
            Group instanceGroup = m.Groups["instance"];
            if (instanceGroup.Success && (!uint.TryParse(instanceGroup.Value, out instance) || instance > AccountInstanceMask))
            {
                return false;
            }

            if (!universeGroup.Success && !typeGroup.Success && !instanceGroup.Success && account > AccountIDMask)
            {
                return SetFromUInt64(account);
            }
            else if (account > AccountIDMask)
            {
                return false;
            }

            switch (type)
            {
                case EAccountType.Clan:
                    instance = 0;
                    break;

                case EAccountType.Individual:
                case EAccountType.Invalid:
                    instance = 1;
                    break;

                case EAccountType.Chat:
                    switch (charType)
                    {
                        case 'T':
                            instance = 0;
                            break;

                        case 'L':
                            instance = (uint)EChatInstanceFlag.Lobby;
                            break;

                        case 'c':
                            instance = (uint)EChatInstanceFlag.Clan;
                            break;
                    }

                    break;

                case EAccountType.AnonGameServer:
                    if (account == 0)
                    {
                        instance = 0;
                    }

                    break;
            }

            AccountID = (uint)account;
            AccountInstance = instance;
            AccountType = type;
            AccountUniverse = universe;

            return true;
        }

        /// <summary>
        /// Set SteamID from a Steam3 strictly formatted string or Steam64 string
        /// </summary>
        /// <remarks>
        /// All but one section of the Steam3 format is optional, the account ID.
        /// Square brackets are optional (e.g. U:0:0 is the same as [U:0:0]).
        /// </remarks>
        public bool SetFromStringStrict(string strSteamID, EUniverse defUniverse = EUniverse.Public)
        {
            if (string.IsNullOrEmpty(strSteamID) || defUniverse == EUniverse.Invalid || defUniverse >= EUniverse.Max)
            {
                return false;
            }

            int offset = 0, length = strSteamID.Length;
            if (strSteamID[0] == '[' && strSteamID[^1] == ']')
            {
                offset = 1;
                length = strSteamID.Length - 2;
            }
            else if (!(strSteamID[0] != '[' && strSteamID[^1] != ']'))
            {
                return false;
            }

            Match m = Steam3StrictRegex.Match(strSteamID, offset, length);
            return m.Success && SetFromStringCommon(m, defUniverse);
        }

        /// <summary>
        /// Set SteamID from a Steam3 formatted string
        /// </summary>
        /// <remarks>
        /// All but one section of the Steam3 format is optional, the account ID.
        /// Square brackets are optional (e.g. U:0:0 is the same as [U:0:0]).
        /// </remarks>
        public bool SetFromString(string strSteamID, EUniverse defUniverse = EUniverse.Public)
        {
            if (string.IsNullOrEmpty(strSteamID) || defUniverse == EUniverse.Invalid || defUniverse >= EUniverse.Max)
            {
                return false;
            }

            Match m = Steam3LooseRegex.Match(strSteamID);
            return m.Success && SetFromStringCommon(m, defUniverse);
        }

        /// <summary>
        /// Set SteamID from a Steam64 UInt64
        /// </summary>
        public bool SetFromUInt64(ulong steam64)
        {
            if (steam64 <= AccountIDMask)
            {
                return false;
            }

            steamID.Data = steam64;
            return true;
        }

        public bool IsValid()
        {
            if (AccountUniverse == EUniverse.Invalid || AccountUniverse >= EUniverse.Max)
            {
                return false;
            }

            EAccountType accountType = AccountType;
            switch (accountType)
            {
                case EAccountType.Invalid:
                    return false;

                case EAccountType.Individual:
                    return AccountID != 0 && AccountInstance < (uint)EUserInstance.Max;

                case EAccountType.Clan:
                    return AccountID != 0 && AccountInstance == 0;

                case EAccountType.GameServer:
                    return AccountID != 0;

                case EAccountType.AnonGameServer:
                    return AccountID != 0 || AccountInstance != 0;

                default:
                    return accountType < EAccountType.Max;
            }
        }

        private string RenderSteam2()
        {
            if (AccountType != EAccountType.Invalid && AccountType != EAccountType.Individual)
            {
                return null;
            }

            uint universeDigit = AccountUniverse <= EUniverse.Public ? 0 : (uint)AccountUniverse;
            return $"STEAM_{universeDigit}:{AccountID & 1}:{AccountID >> 1}";
        }

        private string RenderSteam3()
        {
            if (!AccountTypeToCharacterMap.TryGetValue(AccountType, out char type))
            {
                type = 'i';
            }

            bool renderInstance = false;
            switch (AccountType)
            {
                case EAccountType.AnonGameServer:
                case EAccountType.Multiseat:
                    renderInstance = true;
                    break;

                case EAccountType.Individual:
                    renderInstance = AccountInstance != (uint)EUserInstance.Desktop;
                    break;

                case EAccountType.Chat:
                    if ((AccountInstance & (uint)EChatInstanceFlag.Clan) != 0)
                    {
                        type = 'c';
                    }
                    else if ((AccountInstance & (uint)EChatInstanceFlag.Lobby) != 0)
                    {
                        type = 'L';
                    }

                    break;
            }

            if (renderInstance)
            {
                return $"[{type}:{(uint)AccountUniverse}:{AccountID}:{AccountInstance}]";
            }

            return $"[{type}:{(uint)AccountUniverse}:{AccountID}]";
        }

        public string Render(bool steam3 = true)
        {
            if (steam3)
            {
                return RenderSteam3();
            }

            return RenderSteam2();
        }
    }
}
