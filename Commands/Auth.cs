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

using com.avilance.Starship.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.avilance.Starship.Util;
using com.avilance.Starship.Permissions;
using System.IO;

namespace com.avilance.Starship.Commands
{
    class Auth : CommandBase
    {
        public Auth(Client client)
        {
            this.name = "auth";
            this.HelpText = "Used to authenticate as superadmin when first setting up Starship.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (args.Length < 1)
            {
                this.client.sendCommandMessage("Incorrect auth code. This incident has been logged.");
                StarshipServer.logWarn(this.client.playerData.name + " (IP: " + this.client.playerData.ip + ") attempted to use /auth even though it was disabled.");
                return false;
            }

            string code = args[0];

            if (code != StarshipServer.authCode)
            {
                this.client.sendCommandMessage("Incorrect auth code. This incident has been logged.");
                StarshipServer.logWarn(this.client.playerData.name + " (IP: " + this.client.playerData.ip + ") attempted to use /auth even though it was disabled.");
                return false;
            }

            this.client.playerData.group = StarshipServer.groups["superadmin"];
            Users.SaveUser(this.client.playerData);
            StarshipServer.authCode = null;

            if (File.Exists(Path.Combine(StarshipServer.SavePath, "authcode.txt"))) File.Delete(Path.Combine(StarshipServer.SavePath, "authcode.txt"));

            this.client.sendCommandMessage("Thank you for installing Starship v" + StarshipServer.VersionNum + "!");
            this.client.sendCommandMessage("You are now superadmin, your setup is now complete.");
            this.client.sendCommandMessage("You can use /group adduser <username> <group> to provide access to other users.");

            return true;
        }
    }
}
