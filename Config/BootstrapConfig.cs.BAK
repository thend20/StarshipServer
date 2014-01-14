/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using com.avilance.Starship.Util;
using System.Threading;

namespace com.avilance.Starship
{
    class BootstrapConfig
    {
        internal static string BootstrapPath { get { return "bootstrap.config"; } }

        public static void SetupConfig()
        {
            if (File.Exists(BootstrapPath))
            {
                StarshipServer.bootstrapConfig = BootstrapFile.Read(BootstrapPath);
                StarshipServer.SavePath = StarshipServer.bootstrapConfig.storageDirectory + Path.DirectorySeparatorChar + "starship";
            }
            else
            {
                Console.WriteLine("[FATAL ERROR] bootstrap.config file could not be detected!");
                Thread.Sleep(5000);
                Environment.Exit(7);
            }
            if (!Directory.Exists(StarshipServer.SavePath))
            {
                Directory.CreateDirectory(StarshipServer.SavePath);
            }
        }
    }

    class BootstrapFile
    {

        public string[] assetSources = new string[] { "../assets" };

        public string modSource = "../mods";

        public string storageDirectory = "..";

        public static BootstrapFile Read(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BootstrapFile file = Read(fs);
                return file;
            }
        }

        public static BootstrapFile Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<BootstrapFile>(sr.ReadToEnd());
                }
            }
            catch (Exception)
            {
                StarshipServer.logFatal("bootstrap.config file is unreadable. The server start cannot continue.");
                Thread.Sleep(5000);
                Environment.Exit(6);
            }

            return null;
        }
    }
}
