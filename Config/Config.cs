/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.avilance.Starship.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;

namespace com.avilance.Starship
{
    class Config {
        internal static string RulesPath { get { return Path.Combine(StarshipServer.SavePath, "rules.txt"); } }
        internal static string MotdPath { get { return Path.Combine(StarshipServer.SavePath, "motd.txt"); } }
        internal static string ConfigPath { get { return Path.Combine(StarshipServer.SavePath, "config.json"); } }

        public static void CreateIfNot(string file, string data = "")
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, data);
            }
        }

        public static string ReadConfigFile(string file)
        {
            return File.ReadAllText(file);
        }

        public static string GetMotd()
        {
            string returnString = StarshipServer.motdData;

            returnString = returnString.Replace("%players%", StarshipServer.clientCount.ToString());
            returnString = returnString.Replace("%versionNum%", StarshipServer.VersionNum.ToString());

            return returnString;
        }

        public static string GetRules()
        {
            return StarshipServer.rulesData;
        }

        public static int[] GetSpamSettings()
        {
            if (!StarshipServer.config.enableSpamProtection) return new int[] { 0, 0 };

            string[] spamSplit = StarshipServer.config.spamInterval.Split(':');

            try
            {
                int numMessages = int.Parse(spamSplit[0]);
                int numSeconds = int.Parse(spamSplit[1]);

                return new int[] { numMessages, numSeconds };
            }
            catch (Exception e)
            {
                StarshipServer.logError("Unable to read settings for anti-spam system - Is it in numMessages:numSeconds format?");
                return new int[] { 0, 0 };
            }
        }

        public static void SetupConfig()
        {
            CreateIfNot(RulesPath, "1) Respect all players 2) No griefing/hacking 3) Have fun!");
            CreateIfNot(MotdPath, "This server is running Starship Server v%versionNum%. Type /help for a list of commands. There are currently %players% player(s) online.");

            StarshipServer.motdData = ReadConfigFile(MotdPath);
            StarshipServer.rulesData = ReadConfigFile(RulesPath);
            
            if (File.Exists(ConfigPath))
            {
                StarshipServer.config = ConfigFile.Read(ConfigPath);
            }

            if (StarshipServer.IsMono && StarshipServer.config == null)
                StarshipServer.config = new ConfigFile();

            StarshipServer.config.Write(ConfigPath);

#if DEBUG
            StarshipServer.config.logLevel = LogType.Debug;
            StarshipServer.logDebug("SetupConfig", "This was compiled in DEBUG, forcing debug logging!");
#endif
        }

        public static bool ReloadMOTD()
        {
            try
            {
                StarshipServer.motdData = ReadConfigFile(MotdPath);
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool ReloadRules()
        {
            try
            {
                StarshipServer.rulesData = ReadConfigFile(RulesPath);
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool ReloadConfig()
        {
            try
            {
                ConfigFile config;

                if (File.Exists(ConfigPath)) config = ConfigFile.Read(ConfigPath);
                else throw new FileNotFoundException();

                if (config.Equals(new ConfigFile())) return false;

                return true;
            }
            catch (Exception) { return false; }
        }
    }

    class SpamAction
    {
        public string actionName;
        public string reason;
        public int length;
        public int countToTrigger;

        public bool checkTrigger(int count, Client client)
        {
            if (count == countToTrigger)
            {
                PlayerData player = client.playerData;

                switch (actionName)
                {
                    case "mute":
                        player.isMuted = true;
                        StarshipServer.sendGlobalMessage("^#f75d5d;" + player.name + " has been muted automatically for spamming.");
                        break;

                    case "kick":
                        client.kickClient(reason);
                        break;

                    case "ban":
                        if (length != 0) length = Utils.getTimestamp() + (length * 60);

                        Bans.addNewBan(player.name, player.uuid, player.ip, Utils.getTimestamp(), "[SYSTEM]", length, reason);

                        client.banClient(reason);
                        break;
                }

                return true;
            }

            return false;
        }
    }

    class ConfigFile
    {
        [Description("")]
        public short serverPort = 21024;
        public string proxyIP = "0.0.0.0";
        public short proxyPort = 21025;
        public string proxyPass = "";
        public int passwordRounds = 5000;

        public int maxClients = 25;

        public string logFile = "proxy.log";
        public LogType logLevel = LogType.Info;

        public string serverName = "Starship Server";

        public bool allowSpaces = true;
        public bool allowSymbols = false;
        public string[] bannedUsernames = new string[] { "admin", "developer", "moderator", "owner" };

        public bool enableSpamProtection = true;
        public string spamInterval = "3:2";

        public bool freeFuelForNewPlayers = true;
        public string[] starterItems = new string[] { };

        public bool spawnWorldProtection = false;
        public string defaultSpawnCoordinates = "";
        public string buildErrorMessage = "You do not have permission to build on this server. You can apply for build rights on our forum.";

        public string[] sectors = new string[] { "alpha", "beta", "gamma", "delta", "sectorx" };

        public bool allowModdedClients = true;

        public bool attemptAutoRestart = true;

        public bool enableGeoIP = false;
        public int maxFailedConnections = 3;

        public string[] projectileBlacklist = new string[] { };
        public string[] projectileBlacklistSpawn = new string[] { };
        public string[] projectileGreylist = new string[] { };
        public bool projectileSpawnListIsWhitelist = false;

        public int connectTimeout = 5;
        public int internalSocketTimeout = 5;
        public int clientSocketTimeout = 15;

        public bool enableCallback = true;
        
        public static ConfigFile Read(string path) {
            if (!File.Exists(path))
                return new ConfigFile();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ConfigFile file = Read(fs);
                StarshipServer.logInfo("Starship config loaded successfully.");
                return file;
            }
        }

        public static ConfigFile Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd());
                }
            }
            catch (Exception) 
            {
                StarshipServer.logException("Starship config is unreadable - Re-creating config with default values");
                return new ConfigFile(); 
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }
}
