/* 
 * Starship Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.avilance.Starship.Extensions;
using com.avilance.Starship.Util;
using com.avilance.Starship.Permissions;

namespace com.avilance.Starship.Commands
{
    class Fuel : CommandBase
    {
        public Fuel(Client client)
        {
            this.name = "fuel";
            this.HelpText = "Provides enough fuel to travel to another planet from spawn.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (this.client.playerData.freeFuel)
            {
                client.sendCommandMessage("Sorry, you have already received free starter fuel on this server.");
                return false;
            }
            else if (!StarshipServer.config.freeFuelForNewPlayers)
            {
                client.sendCommandMessage("Sorry, this server does not provide free starter fuel.");
                return false;
            }

            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            packetWrite.WriteStarString("solariumore");
            packetWrite.WriteVarUInt32(31); // Seems like it always gives 1 less than the amount entered, so we need 31 to give 30.
            packetWrite.Write((byte)0); //0 length Star::Variant
            client.sendClientPacket(Packet.GiveItem, packet.ToArray());
            client.sendCommandMessage("You have received 30 Solarium Ore as free starter fuel!");

            this.client.playerData.freeFuel = true;
            Users.SaveUser(this.client.playerData);

            return true;
        }
    }
}
