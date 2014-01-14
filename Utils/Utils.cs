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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using com.avilance.Starship.Extensions;

namespace com.avilance.Starship.Util
{
    public class Utils
    {
        public static string ByteArrayToString(byte[] buffer)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in buffer)
                sb.Append(b.ToString("X2"));

            return (sb.ToString());
        }

        public static string ByteToBinaryString(byte byteIn)
        {
            StringBuilder out_string = new StringBuilder();
            byte mask = 128;
            for (int i = 7; i >= 0; --i)
            {
                out_string.Append((byteIn & mask) != 0 ? "1" : "0");
                mask >>= 1;
            }
            return out_string.ToString();
        }

        public static string StarHashPassword(string message, string salt, int rounds)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] messageBuffer = sha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            byte[] saltBuffer = Encoding.UTF8.GetBytes(salt);

            while (rounds > 0)
            {
                MemoryStream ms = new MemoryStream();
                ms.Write(messageBuffer, 0, messageBuffer.Length);
                ms.Write(saltBuffer, 0, saltBuffer.Length);
                messageBuffer = sha256.ComputeHash(ms.ToArray());
                rounds--;
            }

            return Convert.ToBase64String(messageBuffer);
        }

        public static string GenerateSecureSalt()
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[24];
            rng.GetNonZeroBytes(buffer);
            return Convert.ToBase64String(buffer);
        }

        public static byte[] HashUUID(byte[] uuid)
        {
            MD5 md5 = MD5.Create();
            return md5.ComputeHash(uuid);
        }

        public static int getTimestamp()
        {
            int unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }

        public static WorldCoordinate findGlobalCoords(byte[] input)
        {
            try
            {
                for (int i = 0; i < input.Length - 26; i++)
                {
                    foreach (byte[] sector in StarshipServer.sectors)
                    {
                        byte[] buffer = new byte[sector.Length];
                        Buffer.BlockCopy(input, i, buffer, 0, sector.Length);
                        if (sector.SequenceEqual(buffer))
                        {
                            byte[] returnBytes = new byte[sector.Length + 21];
                            Buffer.BlockCopy(input, i - 1, returnBytes, 0, sector.Length + 21);
                            BinaryReader coords = new BinaryReader(new MemoryStream(returnBytes));
                            WorldCoordinate rCoords = coords.ReadStarWorldCoordinate();
                            if (String.IsNullOrEmpty(rCoords._syscoord._sector)) rCoords = null;
                            return rCoords;
                        }
                    }
                }
                return null;
            }
            catch(Exception e)
            {
                StarshipServer.logDebug("findGlobalCoords", "Exception: " + e.ToString());
                return null;
            }
        }
    }
}
