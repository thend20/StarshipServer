/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Commands
{
    [ChatCommand]
    class Kick : CommandBase
    {
        public Kick(Client client)
        {
            this.name = "kick";
            this.HelpText = " <username> (reason): Kicks the user from the server for specified or, if not defined, default reason.";
            this.Permission = new List<string>();
            this.Permission.Add("admin.kick");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            string player;
            string reason;

            if (args.Length > 1)
            {
                player = args[0].Trim();
                reason = string.Join(" ", args).Substring(player.Length + 1).Trim();
            }
            else
            {
                player = string.Join(" ", args).Trim();
                reason = "breaking the rules";
            }

            if (player == null || player.Length < 1) { showHelpText(); return false; }

            Client target = StarshipServer.getClient(player);
            if (target != null)
            {
                target.kickClient(reason);
                return true;
            }
            else
            {
                this.client.sendCommandMessage("Player '" + player + "' not found.");
                return false;
            }
        }
    }
}
