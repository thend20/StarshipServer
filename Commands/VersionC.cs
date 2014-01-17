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
    class VersionC : CommandBase
    {
        public VersionC(Client client)
        {
            this.name = "version";
            this.HelpText = "";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            this.client.sendCommandMessage("This server is running Starship Server version " + StarshipServer.VersionNum.ToString() + ".");
            this.client.sendCommandMessage("Running Starbound Server version " + StarshipServer.starboundVersion.Name + " (" + StarshipServer.ProtocolVersion + ").");
            return true;
        }
    }
}
