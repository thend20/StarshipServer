using com.goodstuff.Starship.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace com.goodstuff.Starship.Permissions
{
    /// <summary>
    /// Serializable objects used to store territory claims.
    /// </summary>
    public class TerritoryClaim
    {
        public WorldCoordinate world;
        public string uuid;
        public string ownerName;
        public List<string> allowed;

        public TerritoryClaim(WorldCoordinate world, string uuid, string ownerName, List<string> allowed)
        {
            this.world = world;
            this.uuid = uuid;
            this.allowed = allowed;
            this.ownerName = ownerName;
        }
    }

    /// <summary>
    /// Static methods for loading claim objects
    /// </summary>
    public class Claims
    {
        public const int DEFAULT_MAX_OWNED_WORLDS = 1;
        private static Dictionary<WorldCoordinate, TerritoryClaim> claimCache = new Dictionary<WorldCoordinate, TerritoryClaim>();
        private static readonly object cacheLock = new object();
        private static void addToCache(TerritoryClaim claim) { lock (cacheLock) claimCache[claim.world] = claim; }

        internal static string ClaimsPath { get { return Path.Combine(StarshipServer.SavePath, "claims"); } }

        public static void SetupClaims()
        {
            if (!Directory.Exists(ClaimsPath))
                Directory.CreateDirectory(ClaimsPath);
        }

        /// <summary>
        /// Returns a boolean value indicating whether the given world is claims
        /// </summary>
        public static bool IsClaimed(WorldCoordinate world)
        {
            return File.Exists(Claims.PathToClaim(world));
        }

        public static bool CanUserBuild(string uuid, WorldCoordinate world)
        {
            TerritoryClaim claim;
            if (TryGetClaim(world, out claim))
                return claim.uuid == uuid || claim.allowed.Contains(uuid);
            else
                return true;
        }

        /// <summary>
        /// Returns false if the given world is not claimed, or true with the territory data
        /// assigned to the claim output variable.
        /// </summary>
        /// <param name="world">The world claim to retrive</param>
        /// <param name="claim">The variable to store the TerritoryClaim</param>
        /// <returns>Boolean indicating if a claim was found</returns>
        public static bool TryGetClaim(WorldCoordinate world, out TerritoryClaim claim)
        {
            lock (cacheLock)
            {
                if (claimCache.TryGetValue(world, out claim))
                {
                    return true;
                }
                else if (File.Exists(PathToClaim(world)))
                {
                    try
                    {
                        StarshipServer.logDebug("Claims", world.ToString() + " not found in cache, loading persistant data...");
                        claim = Read(PathToClaim(world), world.ToString());
                        if (claim != null)
                            addToCache(claim);
                        return claim != null;
                    }
                    catch (Exception)
                    {
                        StarshipServer.logError("Territory claim data for world " + world.ToString() + " is corrupted. The file " + Path.Combine(ClaimsPath, world.ToString().ToLower() + ".json") + " should be repaired or deleted.");
                        claim = null;
                        return false;
                    }
                }
                else
                {
                    StarshipServer.logDebug("Claims", world.ToString() + " not found in cache and no persistant data found.");
                    claim = null;
                    return false;
                }
            }
        }

        public static bool SaveClaim(WorldCoordinate world, PlayerData player, List<string> allowed)
        {
            if (allowed == null)
                allowed = new List<string>();
            var claim = new TerritoryClaim(world, player.uuid, player.name, allowed);
            if (Claims.SaveClaim(claim))
            {
                player.claimedSystems.Add(world);
                Users.SaveUser(player);
                return true;
            }
            else
                return false;
        }

        public static bool SaveClaim(TerritoryClaim claim)
        {
            try
            {
                Write(PathToClaim(claim), claim);
                addToCache(claim);
                StarshipServer.logInfo("Saved persistant territory claims for " + claim.world.ToString());
                return true;
            }
            catch (Exception e)
            {
                StarshipServer.logException("Unable to save territory claim file for " + claim.world.ToString() + "\n" + e.ToString() + ": " + e.Message + '\n' + e.StackTrace);
            }
            return false;
        }

        public static bool ReleaseClaim(WorldCoordinate world, string uuid)
        {
            TerritoryClaim claim;
            if (TryGetClaim(world, out claim))
            {
                if (claim.uuid == uuid)
                {
                    try
                    {
                        File.Delete(PathToClaim(claim));
                        lock (cacheLock)
                            claimCache.Remove(world);
                        return true;
                    }
                    catch (Exception e)
                    {
                        StarshipServer.logException("Unable to delete persistant territory claim.\n" + e.ToString() + ": " + e.Message);
                    }
                }
            }
            return false;
        }

        private static TerritoryClaim Read(string path, string world)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var claim = Claims.Read(fs);
                StarshipServer.logInfo("Loaded persistant territory claim for: " + claim.world.ToString());
                return claim;
            }
        }

        private static TerritoryClaim Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<TerritoryClaim>(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                StarshipServer.logException("Exception while loading persistant territory claim data, aborting.\n" + e.ToString() + ": " + e.Message);
                return null;
            }
        }

        private static void Write(string path, TerritoryClaim claim)
        {
            StarshipServer.logDebug("Claims", "Saving claim to file: " + path);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs, claim);
            }
        }

        private static void Write(Stream stream, TerritoryClaim claim)
        {
            string serialized = JsonConvert.SerializeObject(claim, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(serialized);
            }
        }

        private static string PathToClaim(TerritoryClaim claim)
        {
            return PathToClaim(claim.world.ToString());
        }

        private static string PathToClaim(WorldCoordinate coord)
        {
            return PathToClaim(coord.ToString());
        }

        private static string PathToClaim(string worldCoordString)
        {
            return Path.Combine(ClaimsPath, worldCoordString.ToLower().Replace(':', '_') + ".json");
        }
    }
}
