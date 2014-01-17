/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

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
            this.HelpText = "Claim current planet" + Environment.NewLine +
                "\toff - remove claim" + Environment.NewLine +
                "\tallow [user] - Whitelist user, allow them to build on this planet" + Environment.NewLine +
                "\tdeny [user] - remove user from whitelist";

            this.client = client;
            this.player = client.playerData;
        }
        public override bool doProcess(string[] args)
        {
            throw new NotImplementedException();
        }
    }
}