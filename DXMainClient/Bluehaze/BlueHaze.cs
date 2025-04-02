using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Bluehaze
{
    internal class BlueHaze
    {
        public static PlayerProfile PlayerProfile;
        public static TacticalGallery TacticalGallery;

        static BlueHaze()
        {
            PlayerProfile = PlayerProfile.LoadDefaultProfile();

            TacticalGallery = new TacticalGallery();
            TacticalGallery.LoadFromIni();
        }
    }
}
