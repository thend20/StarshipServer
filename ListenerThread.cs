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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.goodstuff.Starship.Util;
using System.IO;

namespace com.goodstuff.Starship
{
    class ListenerThread
    {
        public TcpListener tcpSocket;
        public Socket udpSocket;
        byte[] udpByteData = new byte[1024];

        Dictionary<EndPoint, byte[]> challengeData = new Dictionary<EndPoint, byte[]>();

        public void runTcp()
        {
            try
            {
                IPAddress localAdd = IPAddress.Parse(StarshipServer.config.proxyIP);
                tcpSocket = new TcpListener(localAdd, StarshipServer.config.proxyPort);
                tcpSocket.Start();

                StarshipServer.logInfo("Proxy server has been started on " + localAdd.ToString() + ":" + StarshipServer.config.proxyPort);
                StarshipServer.serverState = ServerState.ListenerReady;

                try
                {
                    while (true)
                    {
                        TcpClient clientSocket = tcpSocket.AcceptTcpClient();
                        clientSocket.ReceiveTimeout = StarshipServer.config.clientSocketTimeout * 1000;
                        clientSocket.SendTimeout = StarshipServer.config.internalSocketTimeout * 1000;
                        new Thread(new ThreadStart(new Client(clientSocket).run)).Start();
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception e)
                {
                    if ((int)StarshipServer.serverState > 3) return;
                    StarshipServer.logException("ListenerThread Exception: " + e.ToString());
                }

                tcpSocket.Stop();
                StarshipServer.logFatal("ListenerThread has failed - No new connections will be possible.");
                StarshipServer.serverState = ServerState.Crashed;
            }
            catch (ThreadAbortException) { }
            catch(SocketException e)
            {
                StarshipServer.logFatal("TcpListener has failed to start: " + e.Message);
                StarshipServer.serverState = ServerState.Crashed;
            }
        }

        public void runUdp()
        {
            try
            {
                IPAddress localAdd = IPAddress.Parse(StarshipServer.config.proxyIP);

                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint ipEndPoint = new IPEndPoint(localAdd, StarshipServer.config.proxyPort);

                udpSocket.Bind(ipEndPoint);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                //The epSender identifies the incoming clients
                EndPoint epSender = (EndPoint)ipeSender;

                StarshipServer.logInfo("RCON listener has been started on UDP " + localAdd.ToString() + ":" + StarshipServer.config.proxyPort);

                while (true)
                {
                    int bytesRead = udpSocket.ReceiveFrom(udpByteData, ref epSender);

                    StarshipServer.logInfo("Receiving RCON Data...");
                    OnReceive(udpByteData, bytesRead, epSender);
                }

            }
            catch (Exception e)
            {
                StarshipServer.logError("Something went wrong while trying to setup the UDP listener. " + e.ToString());
            }
        }

        private void SourceRequest(byte[] data, EndPoint remote)
        {
            byte headerByte = data[4];
            byte[] dataArray;

            switch (headerByte)
            {
                case 0x54:
                    dataArray = new byte[data.Length - 6];

                    Buffer.BlockCopy(data, 5, dataArray, 0, dataArray.Length);

                    string text = Encoding.UTF8.GetString(dataArray);
                    string needle = "Source Engine Query";

                    if (text != needle)
                    {
                        StarshipServer.logError("RCON: Received invalid A2S_INFO request: " + text + " is invalid.");
                        return;
                    }
                    else StarshipServer.logDebug("ListenerThread::SourceRequest", "RCON: Matched A2S_INFO request!");

                    try
                    {
                        byte header = 0x49;
                        byte protocol = 0x02;
                        byte[] name = encodeString(StarshipServer.config.serverName);
                        byte[] map = encodeString("Starbound");
                        byte[] folder = encodeString("na");
                        byte[] game = encodeString("Starbound");
                        byte[] appID = BitConverter.GetBytes(Convert.ToUInt16(1337));
                        byte players = Convert.ToByte((uint)StarshipServer.clientCount);
                        byte maxplayers = Convert.ToByte((uint)StarshipServer.config.maxClients);
                        byte bots = Convert.ToByte((uint)0);
                        byte servertype = Convert.ToByte('d');
                        byte environment = Convert.ToByte((StarshipServer.IsMono ? 'l' : 'w'));
                        byte visibility = Convert.ToByte((uint)(StarshipServer.config.proxyPass == "" ? 0 : 1));
                        byte vac = Convert.ToByte((uint)0);
                        byte[] version = encodeString(StarshipServer.starboundVersion.Name);

                        var s = new MemoryStream();
                        s.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4);
                        s.WriteByte(header);
                        s.WriteByte(protocol);
                        s.Write(name, 0, name.Length);
                        s.Write(map, 0, map.Length);
                        s.Write(folder, 0, folder.Length);
                        s.Write(game, 0, game.Length);
                        s.Write(appID, 0, appID.Length);
                        s.WriteByte(players);
                        s.WriteByte(maxplayers);
                        s.WriteByte(bots);
                        s.WriteByte(servertype);
                        s.WriteByte(environment);
                        s.WriteByte(visibility);
                        s.WriteByte(vac);
                        s.Write(version, 0, version.Length);

                        StarshipServer.logInfo("RCON: Sending A2S_INFO Response packet to " + remote);
                        udpSocket.SendTo(s.ToArray(), remote);
                    }
                    catch (Exception e)
                    {
                        StarshipServer.logError("RCON: Unable to send data to stream! An error occurred.");
                        StarshipServer.logError("RCON: " + e.ToString());
                    }
                    break;

                case 0x55:
                    StarshipServer.logDebug("ListenerThread::SourceRequest", "RCON: Received A2S_PLAYER request from " + remote);

                    dataArray = new byte[4];
                    Buffer.BlockCopy(data, 5, dataArray, 0, dataArray.Length);

                    if (dataArray.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
                    {
                        var buffer = new byte[4];
                        new Random().NextBytes(buffer);

                        if (challengeData.ContainsKey(remote)) challengeData.Remove(remote);
                        challengeData.Add(remote, buffer);

                        var s = new MemoryStream();
                        s.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4);
                        s.WriteByte(0x41);
                        s.Write(buffer, 0, 4);

                        StarshipServer.logInfo("RCON: Sending A2S_PLAYER Challenge Response packet to " + remote);
                        udpSocket.SendTo(s.ToArray(), remote);
                    }
                    else
                    {
                        if (!challengeData.ContainsKey(remote)) StarshipServer.logError("RCON: Illegal A2S_PLAYER request received from " + remote + ". No challenge number has been issued to this address.");
                        else
                        {
                            var s = new MemoryStream();
                            s.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4);
                            s.WriteByte(0x44);

                            s.WriteByte(Convert.ToByte((uint)StarshipServer.clientCount));

                            List<Client> clientList = StarshipServer.getClients();

                            for (var i = 0; i < clientList.Count; i++)
                            {
                                Client client = clientList[i];
                                s.WriteByte(Convert.ToByte((uint)i));

                                byte[] name = encodeString(client.playerData.name);
                                s.Write(name, 0, name.Length);

                                byte[] score = new byte[4];
                                score = BitConverter.GetBytes((int)0);
                                s.Write(score, 0, score.Length);

                                float seconds = Utils.getTimestamp() - client.connectedTime;
                                byte[] connected = new byte[4];
                                connected = BitConverter.GetBytes(seconds);
                                s.Write(connected, 0, connected.Length);

                                StarshipServer.logDebug("ListenerThread::SourceA2SPlayer", "Client ID #" + i + ": " + Utils.ByteArrayToString(new byte[] { Convert.ToByte((uint)i) }) + Utils.ByteArrayToString(name) + Utils.ByteArrayToString(score) + Utils.ByteArrayToString(connected));
                            }

                            StarshipServer.logInfo("RCON: Sending A2S_PLAYER Response packet for " + StarshipServer.clientCount + " player(s) to " + remote);
                            StarshipServer.logDebug("ListenerThread::SourceA2SPlayer", "RCON: Dump packet: " + Utils.ByteArrayToString(s.ToArray()));
                            udpSocket.SendTo(s.ToArray(), remote);
                        }
                    }
                    break;

                default:
                    StarshipServer.logError("RCON: Received unknown or unsupported header byte - " + headerByte);
                    break;
            }
        }

        private byte[] encodeString(string data)
        {
            return Encoding.UTF8.GetBytes(data + "\0");
        }

        private void OnReceive(byte[] dataBuffer, int bytesRead, EndPoint remote)
        {
            byte[] data = new byte[bytesRead];

            try
            {
                Buffer.BlockCopy(dataBuffer, 0, data, 0, bytesRead);

                /*
                 * Source Query packets begin with 0xFF (x4)
                 */

                if (bytesRead > 4)
                {
                    byte[] sourceCheck = new byte[] { data[0], data[1], data[2], data[3] };

                    if (sourceCheck.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
                    {
                        SourceRequest(data, remote);
                        return;
                    }
                }

                string text = Encoding.UTF8.GetString(data, 0, bytesRead);

                StarshipServer.logInfo(String.Format("RCON: Received non-source request of {0} bytes from {1}: {2}", bytesRead, remote, text));
            }
            catch (Exception e)
            {
                StarshipServer.logError("Bad RCON request received. " + e.ToString());
                StarshipServer.logError("RCON: Binary data: " + Utils.ByteArrayToString(data));
            }
        }
    }
}
