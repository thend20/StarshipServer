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
            this.HelpText = " <release>|<allow|revoke [user]>: Claims current planet without parameters or allows other users to build on owned planet.";

            this.client = client;
            this.player = client.playerData;
        }
        public override bool doProcess(string[] args)
        {
            WorldCoordinate world = this.player.loc;

            if (args.Length == 0)
            {
                StarshipServer.logDebug("Claim debugging", "Claiming the planet: " + world.ToString());
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
                        if (Claims.ReleaseClaim(world, this.player.uuid))
                        {
                            this.player.claimedSystems.Remove(world);
                            Users.SaveUser(this.player);
                            this.client.sendCommandMessage("Claim released.");
                        }
                        else
                            this.client.sendCommandMessage("Claim not yours or does not exist.");
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
                                    if (Claims.TryGetClaim(world, out claim))
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
                                    if (Claims.TryGetClaim(world, out claim))
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