/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Packets;
using com.goodstuff.Starship.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Commands
{
    class Shutdown : CommandBase
    {
        public Shutdown(Client client)
        {
            this.name = "shutdown";
            this.HelpText = ": Gracefully closes all connections";
            this.Permission = new List<string>();
            this.Permission.Add("admin.shutdown");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            StarshipServer.serverState = ServerState.GracefulShutdown;
            return true;
        }
    }

    class Restart : CommandBase
    {
        public Restart(Client client)
        {
            this.name = "restart";
            this.HelpText = "Initiate a restart of the server, 30 second delay.";
            this.Permission = new List<string>();
            this.Permission.Add("admin.restart");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (StarshipServer.restartTime != 0)
            {
                StarshipServer.sendGlobalMessage("^#f75d5d;The server restart has been aborted by " + this.player.name);
                StarshipServer.logWarn("The server restart has been aborted.");
                StarshipServer.serverState = ServerState.Running;
                StarshipServer.restartTime = 0;
            }
            else
            {
                StarshipServer.sendGlobalMessage("^#f75d5d;The server will restart in 30 seconds. We will be back shortly.");
                StarshipServer.logWarn("The server will restart in 30 seconds.");
                StarshipServer.restartTime = Utils.getTimestamp() + 30;
            }

            return true;
        }
    }
}
