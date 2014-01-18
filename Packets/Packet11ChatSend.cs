/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Util;
using com.goodstuff.Starship.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using com.goodstuff.Starship.Commands;

namespace com.goodstuff.Starship.Packets
{
    class Packet11ChatSend : PacketBase
    {
        /// <summary>
        /// An array of strings that represent command keywords to be directly passed to the underlying Starbound server.
        /// </summary>
        private static readonly string[] passThrough = { "pvp", "w"};

        /// <summary>
        /// A dictionary of keyword => permission pairs that represent Starbound server commands that we are applying an
        /// additional permission check to.
        /// </summary>
        private static readonly Dictionary<string, string> customPerms = new Dictionary<string,string>() 
            {
                  { "nick", "cmd.nick" }
            }; 

        /// <summary>
        /// A dictionary of keyword => class constructor pairs that allow commands to be instantiated and executed from
        /// chat-based keywords.
        /// </summary>
        private static readonly Dictionary<string, Func<Client, CommandBase>> commandConstructors = new Dictionary<string, Func<Client, CommandBase>>();

        static Packet11ChatSend()
        {
            try
            {
                var dummy = new Client(null); // As commands are constructed per client, we need a dummy to retrieve it's keyword (without major rewrites)
                StarshipServer.logInfo("Loading command keywords...");
                foreach (Type type in (from type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                                       from attribute in type.GetCustomAttributes(true)
                                       where attribute is ChatCommandAttribute && typeof(CommandBase).IsAssignableFrom(type)
                                       orderby type.Name
                                       select type))
                {
                    StarshipServer.logDebug("Command Load", String.Format("Processing {0}...", type.Name));
                    var constructor = type.GetConstructor(new Type[] { typeof(Client) });
                    if (constructor == null)
                    {
                        StarshipServer.logError(String.Format("No valid constructor found for command type {0}", type.FullName));
                        continue;
                    }
                    try
                    {
                        CommandBase temp = (CommandBase)constructor.Invoke(new object[] { dummy });
                        List<string> keywords = new List<string>();

                        keywords.Add(temp.name.ToLower());
                        if (!Char.IsLetterOrDigit(keywords[0][0]) && keywords[0][0] != '?') // Special case
                        {
                            StarshipServer.logError(String.Format("{0} declares invalid keyword: {1}.  Command discarded.", type.FullName, keywords[0]));
                            continue;
                        }

                        if (temp.aliases == null)
                        {
                            StarshipServer.logDebug("Command Load", String.Format("Keyword found: {0}", temp.name.ToLower()));
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder(temp.name.ToLower());
                            foreach (string alias in temp.aliases)
                            {
                                if (!Char.IsLetterOrDigit(alias[0]) && alias[0] != '?')
                                {
                                    // If the alias doesn't begin with a letter or number, it will not be used as a keyword.
                                    StarshipServer.logWarn("Invalid command keyword: " + alias);
                                    continue;
                                }
                                sb.AppendFormat(", {0}", alias.ToLower());
                                keywords.Add(alias.ToLower());
                            }
                            StarshipServer.logDebug("Command Load", String.Format("Keywords found: {0}", sb));
                        }
                        Func<Client, CommandBase> func = (Client client) => { return (CommandBase)constructor.Invoke(new object[] { client }); };
                        foreach (string keyword in keywords)
                        {
                            if (commandConstructors.ContainsKey(keyword))
                                StarshipServer.logWarn(String.Format("{0} keyword overwritten by {1}!", keyword, type.FullName));

                            commandConstructors[keyword] = func;
                        }
                        StarshipServer.logDebug("Command Load", "Registering command with Help documentation");
                        Help.Commands.Add(temp);
                        StarshipServer.logDebug("Command Load", String.Format("{0} loaded.", type.FullName));
                    }
                    catch (Exception ex)
                    {
                        StarshipServer.logException(String.Format("Unhandled {0} exception while processing {1} command: {2}", ex.ToString(), type.Name, ex.Message));
                    }
                }
                StarshipServer.logInfo("Done loading command keywords.");
            }
            catch (Exception ex)
            {
                StarshipServer.logException(String.Format("{0} exception while loading command list: {1}{2}{3}", ex.ToString(), ex.Message, Environment.NewLine, ex.StackTrace));
                System.Threading.Thread.Sleep(5000);
                System.Environment.Exit(7); // Not sure of established exit codes
            }
        }

        Dictionary<string, object> tmpArray = new Dictionary<string, object>();

        public Packet11ChatSend(Client clientThread, BinaryReader stream, Direction direction)
        {
            this.client = clientThread;
            this.stream = stream;
            this.direction = direction;
        }

        public Packet11ChatSend(Client clientThread, Direction direction)
        {
            this.client = clientThread;
            this.direction = direction;
        }

        /// <summary>
        /// Reads the data from the packet stream
        /// </summary>
        /// <returns>true to maintain packet and send to client, false to drop packet, -1 will boot the client</returns>
        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.stream;

            string message = packetData.ReadStarString();
            byte context = packetData.ReadByte();

            #region Command Processor
            if (message.StartsWith("#"))
            {
                StarshipServer.logInfo("[Admin Chat] [" + this.client.playerData.name + "]: " + message);

                bool aChat = new AdminChat(this.client).doProcess(new string[] { message.Remove(0, 1) });

                return false;
            }
            else if (message.StartsWith("/"))
            {
                try
                {
                    StarshipServer.logInfo("[Command] [" + this.client.playerData.name + "]: " + message);
                    string[] args = message.Remove(0, 1).Split(' ');
                    string cmd = args[0].ToLower();

                    if (Packet11ChatSend.passThrough.Contains(cmd))
                        // Command passes directly to server
                        return true;

                    if (Packet11ChatSend.customPerms.ContainsKey(cmd))
                    {
                        // Command is subject to custom permission check before passing to server
                        if (this.client.playerData.hasPermission(Packet11ChatSend.customPerms[cmd]))
                        {
                            return true;
                        }
                        else
                        {
                            this.client.sendChatMessage(ChatReceiveContext.Whisper, "", "You do not have permission to use this command.");
                            return false;
                        }
                    }

                    // Execute custom command
                    args = parseArgs(message.Remove(0, cmd.Length + 1));

                    if (commandConstructors.ContainsKey(cmd))
                    {
                        commandConstructors[cmd](this.client).doProcess(args);
                    }
                    else
                        this.client.sendCommandMessage("Command " + cmd + " not found.");

                    return false;
                }
                catch (Exception e)
                {
                    this.client.sendCommandMessage("Command failed: " + e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
            #endregion

            if (this.client.playerData.isMuted)
            {
                this.client.sendCommandMessage("^#f75d5d;You try to speak, but nothing comes out... You have been muted.");
                return false;
            }

            StarshipServer.logInfo("[" + ((ChatSendContext)context).ToString() + "] [" + this.client.playerData.name + "]: " + message);
            return true;
        }

        // Test with http://gskinner.com/RegExr/
        // Current expression courtesy of http://stackoverflow.com/a/20303808
        /// <summary>
        /// The regular expression used when splitting arguments.
        /// Compiled as a static variable to speed up argument checking
        /// </summary>
        static System.Text.RegularExpressions.Regex argRegex = new System.Text.RegularExpressions.Regex(
            @"(?:^[ \t]*((?>[^ \t""\r\n]+|""[^""]+(?:""|$))+)|(?!^)[ \t]+((?>[^ \t""\\\r\n]+|(?<!\\)(?:\\\\)*""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*""{1,2}|(?:\\(?:\\\\)*"")+|\\+(?!""))+)|([^ \t\r\n]))", 
            System.Text.RegularExpressions.RegexOptions.Compiled);
        
        /// <summary>
        /// Parses the args into an array, taking quotes and brackets into account
        /// </summary>
        /// <param name="args">The string to parse</param>
        /// <returns></returns>
        private static string[] parseArgs(string args)
        {
            List<string> parsed;
            System.Text.RegularExpressions.MatchCollection matches;

            parsed = new List<string>();

            matches = argRegex.Matches(args);

            // Loop through matches (there should only ever be one if we're successful);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                {
                    string val;

                    // The value of the capture is our value
                    val = capture.Value;
                    // Trim out any trailing spaces
                    val = val.Trim();
                    // Trim out any characters used for sectioning off the argument
                    val = val.Trim('"', '(', ')', '\'');

                    parsed.Add(val);
                }
            }

            return parsed.ToArray();
        }

        public void prepare(ChatReceiveContext context, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", "");
            tmpArray.Add("entityID", 0);
            tmpArray.Add("name", "");
            tmpArray.Add("message", message);
        }

        public void prepare(ChatReceiveContext context, string name, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", "");
            tmpArray.Add("entityID", 0);
            tmpArray.Add("name", name);
            tmpArray.Add("message", message);
        }

        public void prepare(ChatReceiveContext context, string world, uint entityID, string name, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", world);
            tmpArray.Add("entityID", entityID);
            tmpArray.Add("name", name);
            tmpArray.Add("message", message);
        }

        public override void onSend()
        {
            if (tmpArray.Count < 5) return;

            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            ChatReceiveContext sContext = (ChatReceiveContext)tmpArray["context"];
            string sWorld = (string)tmpArray["world"];
            uint sClientID = (uint)tmpArray["entityID"]; // Player entity ID
            string sName = (string)tmpArray["name"]; // Name
            string sMessage = (string)tmpArray["message"]; // Message

            packetWrite.Write((byte)sContext);
            packetWrite.WriteStarString(sWorld);
            packetWrite.WriteBE(sClientID);
            packetWrite.WriteStarString(sName);
            packetWrite.WriteStarString(sMessage);
            this.client.sendClientPacket(Packet.ChatReceive, packet.ToArray());
        }

        public override int getPacketID()
        {
            return 11;
        }
    }
}
