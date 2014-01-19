/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using com.goodstuff.Starship.Extensions;

namespace com.goodstuff.Starship
{
    public class PlayerData
    {
        public string name;
        public string account;
        public string ip;
        public uint id;
        public string uuid;
        public string serverName;
        public List<WorldCoordinate> claimedSystems = new List<WorldCoordinate>();

        public bool sentMotd = false;
        public bool freeFuel = false;
        public bool receivedStarterKit = false;

        public Group group;

        public WorldCoordinate loc;
        public WorldCoordinate home;

        public int lastOnline = 0;

        public bool inPlayerShip = false;

        public string client { get { if (String.IsNullOrEmpty(name)) return ip; else return name; } }

        public bool isMuted = false;
        public bool canBuild = true;
        public int maxOwnedWorlds = 1;

        public bool privateShip = false;
        public List<string> shipWhitelist = new List<string>();
        public List<string> shipBlacklist = new List<string>();

        public string formatName 
        {
            get 
            { 
                string prefix = group.prefix;
                string color = group.nameColor;

                if (prefix == null) prefix = "";
                if (color == null) color = "";

                return ((prefix != "") ? prefix + " " : "") + ((color != "") ? "^" + color + ";" : "") + this.name; 
            }
            set { return; }
        }

        public string format
        {
            get
            {
                string prefix = group.prefix;
                string color = group.nameColor;

                if (prefix == null) prefix = "";
                if (color == null) color = "";

                return ((prefix != "") ? prefix + " " : "") + ((color != "") ? "^" + color + ";" : "");
            }
            set { return; }
        }

        public bool hasPermission(string node)
        {
            if (this.group.hasPermission(node)) return true;
            else return false;
        }

        public bool isInSameWorldAs(PlayerData otherPlayer)
        {
            return loc.Equals(otherPlayer.loc);
        }

        public bool canIBuild()
        {
            if (StarshipServer.serverConfig.useDefaultWorldCoordinate && StarshipServer.config.spawnWorldProtection)
            {
                if (loc != null)
                {
                    if ((StarshipServer.spawnPlanet.Equals(loc)) && !group.hasPermission("admin.spawnbuild") && !inPlayerShip)
                        return false;
                }
                else
                    return false;
            }
            else if (!hasPermission("world.build")) return false;
            else if (loc != null && !Claims.CanUserBuild(uuid, loc)) return false;
            else if (!canBuild) return false;
            return true;
        }

        public bool canAccessShip(PlayerData otherPlayer)
        {
            if (this.hasPermission("admin.ignoreshipaccess")) return true; // Admins can bypass access control
            if (otherPlayer.shipBlacklist.Contains(this.name)) return false; // Player is in blacklist

            if (otherPlayer.privateShip) // Ship is on private mode
            {
                if (otherPlayer.shipWhitelist.Contains(this.name)) return true; // Player is in whitelist
                else return false; // Player is NOT in whitelist
            }
            
            else return true; // Ship is on public mode
        }
    }
}
