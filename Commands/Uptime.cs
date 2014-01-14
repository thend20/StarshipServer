/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.avilance.Starship.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.avilance.Starship.Util;

namespace com.avilance.Starship.Commands
{
    class Uptime : CommandBase
    {
        public Uptime(Client client)
        {
            this.name = "uptime";
            this.HelpText = ": Shows how long has past since the server was last restarted.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            int seconds = Utils.getTimestamp() - StarshipServer.startTime;

            this.client.sendCommandMessage("I have been online for " + string.Format("{0:0} hour(s) {1:0} minute(s) and {2:0} second(s).", seconds / 3600, (seconds / 60) % 60, seconds % 60));

            return true;
        }
    }
}
