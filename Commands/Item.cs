/* 
 * Starship Server
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

namespace com.avilance.Starship.Commands
{
    class Item : CommandBase
    {
        public Item(Client client)
        {
            this.name = "item";
            this.HelpText = "<item> <amount>: Allows you to give items to yourself.";
            this.aliases = new string[] { "give" };
            this.Permission = new List<string>();
            this.Permission.Add("admin.give");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length < 2) { showHelpText(); return false; }

            string item = args[0];
            uint count = Convert.ToUInt32(args[1]) + 1;
            if (String.IsNullOrEmpty(item) || count < 1) { showHelpText(); return false; }

            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            packetWrite.WriteStarString(item);
            packetWrite.WriteVarUInt32(count);
            packetWrite.Write((byte)0); //0 length Star::Variant
            client.sendClientPacket(Packet.GiveItem, packet.ToArray());
            client.sendCommandMessage("Gave you " + (count - 1) + " " + item);

            return true;
        }
    }
}
