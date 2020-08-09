using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SourceQuery.Test
{
    [TestClass]
    public class SteamIDTests
    {
        [DataTestMethod]
        [DataRow("STEAM_0:0:66138017")]
        [DataRow("0:0:66138017")]
        public void SteamIDFromSteam2String(string strSteamID)
        {
            SteamID steamID = new SteamID();
            bool success = steamID.SetFromSteam2String(strSteamID);
            Assert.IsTrue(success, $"{strSteamID} failed to parse");
            string render = steamID.Render(false);
            Assert.IsTrue(render.Contains(strSteamID), $"{render} doesn't contain {strSteamID}");
        }

        [DataTestMethod]
        [DataRow("STEAM_0:0")]
        [DataRow("STEAM_0:0:12345678901")]
        [DataRow("0:0")]
        [DataRow("0:0:12345678901")]
        public void FailureSteamIDFromSteam2String(string strSteamID)
        {
            Assert.IsFalse(new SteamID().SetFromSteam2String(strSteamID));
        }

        [DataTestMethod]
        [DataRow("[U-1-0]")]
        [DataRow("[U:1:0]")]
        [DataRow("U:1:0")]
        [DataRow("U-1-0")]
        [DataRow("[U:1:0")]
        [DataRow("U:1:0]")]
        [DataRow("[U-1-0")]
        [DataRow("U-1-0]")]
        [DataRow("[A:1:0:0]")]
        [DataRow("[A:1:0(0)]")]
        [DataRow("A:1:0:0")]
        [DataRow("A:1:0(0)")]
        [DataRow("[A-1-0:0]")]
        [DataRow("[A-1-0(0)]")]
        [DataRow("A-1-0:0")]
        [DataRow("A-1-0(0)")]
        [DataRow("A-1-0:0)")]
        [DataRow("A-1-0(0")]
        [DataRow("[1-0]")]
        [DataRow("[1:0]")]
        [DataRow("1-0")]
        [DataRow("1:0")]
        public void SteamIDFromString(string strSteamID)
        {
            SteamID steamID = new SteamID();
            bool success = steamID.SetFromString(strSteamID);
            Assert.IsTrue(success, $"{strSteamID} failed to parse");
            string render = steamID.Render();
            string clean = strSteamID.Replace('-', ':').Replace('(', ':').Trim(')', '[', ']');
            Assert.IsTrue(render.Contains(clean), $"{render} doesn't contain {strSteamID} ({clean})");
        }

        [DataTestMethod]
        [DataRow("[A-0-0-0]")]
        [DataRow("[A-0-0:(0)]")]
        [DataRow("[U-0-0-0]")]
        [DataRow("A-0-0-0")]
        [DataRow("A-0-0:(0)")]
        [DataRow("U-0-0-0")]
        public void FailureSteamIDFromString(string strSteamID)
        {
            Assert.IsFalse(new SteamID().SetFromString(strSteamID));
        }

        [DataTestMethod]
        [DataRow("[U-0-0]")]
        [DataRow("[U:0:0]")]
        [DataRow("U:0:0")]
        [DataRow("U-0-0")]
        [DataRow("[A:0:0(0)]")]
        [DataRow("A:0:0(0)")]
        [DataRow("[A-0-0(0)]")]
        [DataRow("A-0-0(0)")]
        [DataRow("[0-0]")]
        [DataRow("[0:0]")]
        [DataRow("0-0")]
        [DataRow("0:0")]
        [DataRow("0")]
        [DataRow("[0]")]
        public void SteamIDFromStringStrict(string strSteamID)
        {
            Assert.IsTrue(new SteamID().SetFromStringStrict(strSteamID));
        }

        [DataTestMethod]
        [DataRow("[A-0-0:(0)]")]
        [DataRow("[U-0-0-0]")]
        [DataRow("[U:0:0:0]")]
        [DataRow("[U-0-0:0]")]
        [DataRow("[U:0:0")]
        [DataRow("U:0:0]")]
        [DataRow("[U-0-0")]
        [DataRow("U-0-0]")]
        [DataRow("A-0-0(0")]
        [DataRow("A-0-0:0)")]
        public void FailureSteamIDFromStringStrict(string strSteamID)
        {
            Assert.IsFalse(new SteamID().SetFromStringStrict(strSteamID));
        }
    }
}
