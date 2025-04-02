using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Bluehaze
{
    class TacticalGalleryItem
    {
        public string Name;

    }
    class TacticalGallery
    {
        public List<TacticalGalleryItem> Items;
        public Dictionary<string, List<TacticalGalleryItem>> SideItems;

        public void LoadFromIni()
        {
            string[] sides = ClientConfiguration.Instance.Sides.Split(',').ToArray();
            IniFile ini = new IniFile(SafePath.CombineFilePath(BluehazeConstants.BluehazePath, "TacticalGallery.ini"));

        }
    }
}
