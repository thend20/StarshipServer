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
    class Help : CommandBase
    {
        public static readonly List<CommandBase> Commands = new List<CommandBase>();
                                     
        public Help(Client client)
        {
            this.name = "Help";
            this.HelpText = ": Provides help for using commands.";
            this.aliases = new string[] {"?", "commands", "commandlist"};

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (args.Length == 1)
            {
                string commandToFind = args[0];
                foreach (CommandBase command in Help.Commands)
                {
                    if (command.name.ToLower().Equals(commandToFind.ToLower()))
                    {
                        bool hasPermission = true;
                        if (command.Permission != null && command.Permission.Count > 0)
                        {
                            foreach (string permission in command.Permission)
                            {
                                if (!this.player.hasPermission(permission))
                                {
                                    hasPermission = false;
                                }
                            }
                        }
                        if (hasPermission)
                        {
                            this.client.sendCommandMessage("/" + command.name + command.HelpText);
                            if (command.aliases != null && command.aliases.Length > 0)
                            {
                                string aliasesMessage = "Aliases: ";
                                for (int i = 0; i < command.aliases.Length; i++)
                                {
                                    aliasesMessage += "/" + command.aliases[i] + " ";
                                }
                                this.client.sendCommandMessage(aliasesMessage);
                            }
                            return false;
                        }
                        else
                        {
                            this.client.sendChatMessage(Util.ChatReceiveContext.CommandResult, "", "You do not have permission to view this command.");
                            return false;
                        }
                    }
                }
                this.client.sendChatMessage("Command "+commandToFind+" not found.");
                return false;
            }
            else
            {
                this.client.sendChatMessage("^#5dc4f4;Command list:");
                StringBuilder sb = new StringBuilder();
                foreach (CommandBase command in Help.Commands)
                {
                    bool hasPermission = true;
                    if (command.Permission != null)
                    {
                        foreach (string permission in command.Permission)
                        {
                            if (!this.player.hasPermission(permission))
                            {
                                hasPermission = false;
                            }
                        }
                    }
                    if (hasPermission)
                    {
                        if (sb.Length + command.name.Length < 58)
                        {
                            sb.Append("/").Append(command.name).Append(", ");
                        }
                        else
                        {
                            this.client.sendChatMessage("^#5dc4f4;" + sb.ToString());
                            sb.Clear();
                            sb.Append("/").Append(command.name).Append(", ");
                        }
                    }
                }
                this.client.sendChatMessage("^#5dc4f4;" + sb.Remove(sb.Length - 2, 2).ToString());
                this.client.sendChatMessage("^#5dc4f4;Use /help <command> for help with a specific command.");
                return false;
            }
        }
    }
}
