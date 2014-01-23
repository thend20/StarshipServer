/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using com.goodstuff.Starship.Extensions;
using com.goodstuff.Starship.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Commands
{
    [ChatCommand]
    internal class Claim : CommandBase
    {
        public Claim(Client client)
        {
            this.name = "claim";
            this.HelpText = " <release [#]>|<list>|<who>|<allow|revoke [user]>: Claims current planet without parameters, gives a list of currently owned worlds, displays the owner of the current world, or allows other users to build on the owned planet.";

            this.client = client;
            this.player = client.playerData;
        }
        public override bool doProcess(string[] args)
        {
            if (args.Length == 0)
            {
                WorldCoordinate world = this.player.loc;
                if (this.player.claimedSystems.Contains(world))
                {
                    this.client.sendCommandMessage("You have already claimed this world.");
                }
                else if (Claims.IsClaimed(world))
                {
                    this.client.sendCommandMessage("This world has already been claimed.");
                }
                else if (this.player.claimedSystems.Count >= this.player.maxOwnedWorlds) 
                {
                    this.client.sendCommandMessage("You are at your limit of claimed worlds.");
                }
                else
                {
                    if (Claims.SaveClaim(world, this.player, null))
                        this.client.sendCommandMessage("You have claimed this world.");
                    else
                        this.client.sendCommandMessage("There was an error processing the command, world not claimed.");
                }
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "release":
                        if (this.client.playerData.claimedSystems.Count == 0)
                        {
                            this.client.sendCommandMessage("No claimed systems to release.");
                            break; // No need to save
                        }
                        else if (args.Length >= 2)
                        {
                            try
                            {
                                int index = Convert.ToInt32(args[1]) - 1;
                                if(index < 0 || index >= this.player.claimedSystems.Count)
                                    throw new OverflowException();
                                if (Claims.ReleaseClaim(this.player.claimedSystems[index], this.player.uuid))
                                {
                                    this.client.sendCommandMessage("Released claim on world: " + this.player.claimedSystems[index].ToString());
                                    this.player.claimedSystems.RemoveAt(index);
                                }
                                else
                                {
                                    this.client.sendCommandMessage("Claim not yours or does not exist");
                                    StarshipServer.logError(String.Format("Attempted to release invalid claim on world {0} by player {1}, uuid: {2}", this.player.claimedSystems[index].ToString(), player.name, player.uuid));
                                }
                            }
                            catch (FormatException)
                            {
                                this.client.sendCommandMessage("Invalid world designator: " + args[1]);
                            }
                            catch (OverflowException)
                            {
                                this.client.sendCommandMessage("Index out of range.");
                            }
                        }
                        else
                        {
                            foreach(var world in this.player.claimedSystems) {
                                if (Claims.ReleaseClaim(world, this.player.uuid))
                                    this.client.sendCommandMessage("Released claim on world: " + world.ToString());
                                else
                                {
                                    this.client.sendCommandMessage("Claim not yours or does not exist");
                                    StarshipServer.logError(String.Format("Attempted to release invalid claim on world {0} by player {1}, uuid: {2}", world.ToString(), player.name, player.uuid));
                                }
                            }
                            this.player.claimedSystems.Clear();
                        }
                        Users.SaveUser(this.player);
                        break;

                    case "allow":
                        if (args.Length < 2)
                            this.client.sendCommandMessage("Insufficient parameters.");
                        else
                        {
                            for (int i = 1; i < args.Length; i++)
                            {
                                Client target = StarshipServer.getClient(args[i]);
                                if (target == null)
                                {
                                    this.client.sendCommandMessage(args[i] + " not found.");
                                }
                                else
                                {
                                    TerritoryClaim claim;
                                    if (Claims.TryGetClaim(this.player.loc, out claim))
                                    {
                                        if (claim.uuid != this.player.uuid)
                                        {
                                            this.client.sendCommandMessage("You are not the owner of this world.");
                                        }
                                        else
                                        {
                                            this.client.sendCommandMessage("You have allowed " + args[i] + " to build on this world.");
                                            claim.allowed.Add(target.playerData.uuid);
                                            Claims.SaveClaim(claim);
                                        }
                                    }
                                    else
                                    {
                                        this.client.sendCommandMessage("World is unclaimed.");
                                    }
                                }
                            }
                        }
                        break;

                    case "revoke":
                        if (args.Length < 2)
                        {
                            this.client.sendCommandMessage("Insufficient parameters.");
                            break;
                        }
                        for (int i = 1; i < args.Length; i++)
                        {
                            Client target = StarshipServer.getClient(args[i]);
                            if (target == null)
                            {
                                this.client.sendCommandMessage(args[i] + " not found.");
                            }
                            else
                            {
                                TerritoryClaim claim;
                                if (Claims.TryGetClaim(this.player.loc, out claim))
                                {
                                    if (claim.uuid != this.player.uuid)
                                    {
                                        this.client.sendCommandMessage("You are not the owner of this world.");
                                    }
                                    else
                                    {
                                        if (claim.allowed.Remove(target.playerData.uuid))
                                        {
                                            this.client.sendCommandMessage("You have revoked " + args[i] + "'s permission to build on this world.");
                                            Claims.SaveClaim(claim);
                                        }
                                        else
                                        {
                                            this.client.sendCommandMessage(args[i] + " did not have permission to build on this world.");
                                        }
                                    }
                                }
                                else
                                {
                                    this.client.sendCommandMessage("World is unclaimed.");
                                }
                            }
                        }
                        break;

                    case "list":
                        if (this.player.claimedSystems.Count == 0)
                        {
                            this.client.sendCommandMessage("You have no claimed worlds.");
                        }
                        else
                        {
                            int n = 1;
                            foreach (var world in this.player.claimedSystems)
                                this.client.sendCommandMessage(String.Format("{0}: {1}", n++, world.ToString()));
                        }
                        break;

                    case "who":
                        TerritoryClaim t;
                        if(Claims.TryGetClaim(this.player.loc, out t))
                        {
                            if (t.uuid == this.player.uuid)
                                this.client.sendCommandMessage("You are the owner of this world.");
                            else
                                this.client.sendCommandMessage("This world is claimed by " + t.ownerName);
                        }
                        else
                        {
                            this.client.sendCommandMessage("This world is not claimed.");
                        }
                        break;

                    default:
                        this.client.sendCommandMessage("Invalid claim operation: " + args[0]);
                        break;
                }
            }
            return false;
        }
    }
}