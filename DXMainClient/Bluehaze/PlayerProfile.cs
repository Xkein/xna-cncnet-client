using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;

using Newtonsoft.Json;

using Rampastring.Tools;

namespace DTAClient.Bluehaze
{
    enum TacticalGalleryItemStatus
    {
        Unknown,
        Known,
        Unlocked,
    }
    internal class PlayerProfile
    {
        public string ProfileId;
        public string PlayerName;
        private Dictionary<string, TacticalGalleryItemStatus> _tacticalGalleryStatus;

        public TacticalGalleryItemStatus GetTacticalGalleryItemStatus(TacticalGalleryItem item)
        {
            if (_tacticalGalleryStatus.TryGetValue(item.Name, out TacticalGalleryItemStatus status))
            {
                return status;
            }

            return TacticalGalleryItemStatus.Unknown;
        }

        public static PlayerProfile LoadDefaultProfile()
        {
            string defaultProfileId = "Default";
            return LoadProfile(defaultProfileId);
        }

        public static PlayerProfile LoadProfile(string profileId)
        {
            FileInfo fileInfo = SafePath.GetFile(ProgramConstants.GamePath, "Saved Games/Profile", profileId+".prof");
            if (!fileInfo.Exists)
                return null;
            return JsonSerializer.CreateDefault().Deserialize<PlayerProfile>(new JsonTextReader(new StreamReader(fileInfo.FullName)));
        }

        public void Save()
        {

        }

        public static PlayerProfile CreateProfile()
        {
            PlayerProfile profile = new PlayerProfile();

            return profile;
        }

        public static void DeleteProfile(string profileId)
        {

        }
    }
}
