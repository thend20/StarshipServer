/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Commands
{
    [ChatCommand]
    internal class Claim : CommandBase
    {
        /*
            Claim planet for build/destroy protection
            Allow player to build
            Disabled for guest
            One claim per user, new claim removes previous

            Commands:
            /claim - claim current planet
            /claim off - remove claim
            /claim allow [user] - Whitelist user, allow them to build
            /claim deny [user] - remove user from whitelist 
        */
        public Claim(Client client)
        {
            this.name = "claim";
            this.HelpText = " <off>|<allow|deny [user]>: Claims current planet without parameters or allows other users to build on owned planet.";

            this.client = client;
            this.player = client.playerData;
        }
        public override bool doProcess(string[] args)
        {
            SystemCoordinate currentPlanet = this.player.loc._syscoord; // Identify current planet?
            if (args.Length == 0)
            {
                //TODO: Check if player already owns a planet and claim current planet
                StarshipServer.logDebug("Claim debugging", "Claiming the planet: " + currentPlanet.ToString());
            }
            else
            {
                string user = null;
                if (args.Length >= 2)
                    user = args[1];

                switch (args[0].ToLower())
                {
                    case "off":
                        //TODO: Check if owned and disown current planet.
                        break;

                    case "allow":
                        if (user == null)
                            this.client.sendCommandMessage("Insufficient parameters.");
                        else
                        {
                            //TODO: Check if owned and allow given user to build on the current system
                        }
                        break;

                    case "deny":
                        if (user == null)
                            this.client.sendCommandMessage("Insufficient parameters.");
                        else
                        {
                            //TODO: Check if owned and remove the given users permission to build on the current system
                        }
                        break;

                    default:
                        this.client.sendCommandMessage("Invalid claim operation: " + args[0]);
                        break;
                }
            }
            return false;
        }
    }
}