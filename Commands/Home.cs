﻿/* 
 * Starship Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.avilance.Starship.Packets;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.avilance.Starship.Util;
using com.avilance.Starship.Extensions;

namespace com.avilance.Starship.Commands
{
    class Home : CommandBase
    {
        public Home(Client client)
        {
            this.name = "home";
            this.HelpText = ": Allows you to teleport to your home planet.";
            this.Permission = new List<string>();
            this.Permission.Add("client.home");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            this.client.sendCommandMessage("Teleporting to your home planet.");

            MemoryStream packetWarp = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packetWarp);

            uint warp = (uint)WarpType.WarpToHomePlanet;
            string player = "";
            packetWrite.WriteBE(warp);
            packetWrite.Write(new WorldCoordinate());
            packetWrite.WriteStarString(player);
            client.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());

            return true;
        }
    }
}
