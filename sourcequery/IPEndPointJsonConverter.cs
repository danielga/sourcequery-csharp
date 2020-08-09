using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net;

namespace SourceQuery
{
    public class IPEndPointJsonConverter : JsonConverter<IPEndPoint>
    {
        public override void WriteJson(JsonWriter writer, IPEndPoint value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        private static IPEndPoint Parse(ReadOnlySpan<char> s)
        {
            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            IPAddress address = IPAddress.Parse(s.Slice(0, addressLength));
            uint port = 0;
            if (addressLength != s.Length)
            {
                port = uint.Parse(s.Slice(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture);
                if (port > IPEndPoint.MaxPort)
                {
                    throw new FormatException("An invalid IPEndPoint was specified.");
                }
            }

            return new IPEndPoint(address, (int)port);
        }

        public override IPEndPoint ReadJson(JsonReader reader, Type objectType, IPEndPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Parse((string)reader.Value);
        }
    }
}
