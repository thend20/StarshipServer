/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.avilance.Starrybound.Util;
using System.IO;

namespace com.avilance.Starrybound
{
    class ListenerThread
    {
        public TcpListener tcpSocket;
        public Socket udpSocket;
        byte[] udpByteData = new byte[1024];

        public void runTcp()
        {
            try
            {
                IPAddress localAdd = IPAddress.Parse(StarryboundServer.config.proxyIP);
                tcpSocket = new TcpListener(localAdd, StarryboundServer.config.proxyPort);
                tcpSocket.Start();

                StarryboundServer.logInfo("Proxy server has been started on " + localAdd.ToString() + ":" + StarryboundServer.config.proxyPort);
                StarryboundServer.serverState = ServerState.ListenerReady;

                try
                {
                    while (true)
                    {
                        TcpClient clientSocket = tcpSocket.AcceptTcpClient();
                        clientSocket.ReceiveTimeout = StarryboundServer.config.clientSocketTimeout * 1000;
                        clientSocket.SendTimeout = StarryboundServer.config.internalSocketTimeout * 1000;
                        new Thread(new ThreadStart(new Client(clientSocket).run)).Start();
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception e)
                {
                    if ((int)StarryboundServer.serverState > 3) return;
                    StarryboundServer.logException("ListenerThread Exception: " + e.ToString());
                }

                tcpSocket.Stop();
                StarryboundServer.logFatal("ListenerThread has failed - No new connections will be possible.");
                StarryboundServer.serverState = ServerState.Crashed;
            }
            catch (ThreadAbortException) { }
            catch(SocketException e)
            {
                StarryboundServer.logFatal("TcpListener has failed to start: " + e.Message);
                StarryboundServer.serverState = ServerState.Crashed;
            }
        }

        public void runUdp()
        {
            try
            {
                IPAddress localAdd = IPAddress.Parse(StarryboundServer.config.proxyIP);

                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint ipEndPoint = new IPEndPoint(localAdd, StarryboundServer.config.proxyPort);

                udpSocket.Bind(ipEndPoint);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                //The epSender identifies the incoming clients
                EndPoint epSender = (EndPoint)ipeSender;

                StarryboundServer.logInfo("RCON listener has been started on UDP " + localAdd.ToString() + ":" + StarryboundServer.config.proxyPort);

                while (true)
                {
                    int bytesRead = udpSocket.ReceiveFrom(udpByteData, ref epSender);

                    StarryboundServer.logInfo("Receiving RCON Data...");
                    OnReceive(udpByteData, bytesRead, epSender);
                }

            }
            catch (Exception e)
            {
                StarryboundServer.logError("Something went wrong while trying to setup the UDP listener. " + e.ToString());
            }
        }

        private void SourceRequest(byte[] data, int bytesRead, EndPoint remote)
        {
            byte headerByte = data[4];
            byte[] dataArray = new byte[bytesRead - 5];

            switch (headerByte)
            {
                case 0x54:
                    Buffer.BlockCopy(data, 5, dataArray, 0, bytesRead - 6);

                    string text = Encoding.UTF8.GetString(dataArray, 0, dataArray.Length).Trim();
                    string needle = "Source Engine Query";

                    if (text != needle)
                    {
                        StarryboundServer.logError("RCON: Received invalid A2S_INFO request: " + text + " is invalid.");
                        StarryboundServer.logError("RCON: Length received " + text.Length + " but expected " + needle.Length);
                        return;
                    }

                    byte header = 0x49;
                    byte protocol = 0x02;
                    byte[] name = encodeString("A Starrybound Server");
                    byte[] map = encodeString("Starbound");
                    byte[] folder = encodeString("na");
                    byte[] game = encodeString("Starbound");
                    byte appID = Convert.ToByte(211820);
                    byte players = Convert.ToByte(StarryboundServer.clientCount);
                    byte maxplayers = Convert.ToByte(StarryboundServer.config.maxClients);
                    byte bots = Convert.ToByte(0);
                    byte servertype = Convert.ToByte("d");
                    byte environment = Convert.ToByte((StarryboundServer.IsMono ? 'l' : 'w'));
                    byte visibility = Convert.ToByte((StarryboundServer.config.proxyPass == "" ? 0 : 1));
                    byte vac = Convert.ToByte(0);
                    byte[] version = encodeString(StarryboundServer.starboundVersion.Name);

                    var s = new MemoryStream();
                    s.WriteByte(header);
                    s.WriteByte(protocol);
                    s.Write(name, 0, name.Length);
                    s.Write(map, 0, map.Length);
                    s.Write(folder, 0, folder.Length);
                    s.Write(game, 0, game.Length);
                    s.WriteByte(appID);
                    s.WriteByte(players);
                    s.WriteByte(maxplayers);
                    s.WriteByte(bots);
                    s.WriteByte(servertype);
                    s.WriteByte(environment);
                    s.WriteByte(visibility);
                    s.WriteByte(vac);
                    s.Write(version, 0, version.Length);

                    StarryboundServer.logInfo("RCON: Sending A2S_INFO Response packet to " + remote);

                    udpSocket.SendTo(s.ToArray(), remote);
                    break;
            }
        }

        private byte[] encodeString(string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        private void OnReceive(byte[] data, int bytesRead, EndPoint remote)
        {
            try
            {
                /*
                 * Source Query packets begin with 0xFF (x4)
                 */

                if (bytesRead > 4)
                {
                    byte[] sourceCheck = new byte[] { data[0], data[1], data[2], data[3] };

                    if (sourceCheck.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
                    {
                        SourceRequest(data, bytesRead, remote);
                        return;
                    }
                    else
                    {
                        StarryboundServer.logWarn("RCON: Source check failed - " + Utils.ByteArrayToString(sourceCheck));
                        StarryboundServer.logWarn("RCON: Full dump - " + Utils.ByteArrayToString(data));
                    }
                }

                string text = Encoding.UTF8.GetString(data, 0, bytesRead);

                StarryboundServer.logInfo(String.Format("RCON: Received non-source request of {0} bytes from {1}: {2}", bytesRead, remote, text));
            }
            catch (Exception e)
            {
                StarryboundServer.logError("Bad RCON request received. " + e.ToString());
            }
        }
    }
}
