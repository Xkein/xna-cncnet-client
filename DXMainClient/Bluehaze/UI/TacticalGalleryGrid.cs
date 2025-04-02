using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.Bluehaze.UI
{
    internal class TacticalGalleryGrid : XNAMultiColumnListBox
    {
        public TacticalGalleryGrid(WindowManager windowManager) : base(windowManager)
        {
        }

        public void SetItemsBySide(string side)
        {
            if(!BlueHaze.TacticalGallery.SideItems.TryGetValue(side, out List<TacticalGalleryItem> items))
            {
                throw new ArgumentException($"could not set items by side <{side ?? "null"}>", nameof(side));
            }

            ClearItems();


        }
    }
}
