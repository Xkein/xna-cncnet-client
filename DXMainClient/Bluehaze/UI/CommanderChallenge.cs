using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.Bluehaze.UI
{
    internal class CommanderChallenge : GameLobbyBase
    {
        public CommanderChallenge(WindowManager windowManager, MapLoader mapLoader, TopBar topBar, DiscordHandler discordHandler)
            : base(windowManager, nameof(CommanderChallenge), mapLoader, false, discordHandler)
        {
            this._topBar = topBar;
        }

        private TopBar _topBar;

        private TacticalGalleryGrid _lbTacticalGallery;
        private XNAProgressBar _prgChallenge;

        public override void Initialize()
        {
            Name = nameof(CommanderChallenge);

            _lbTacticalGallery = FindChild<TacticalGalleryGrid>("lbTacticalGallery");
            _lbTacticalGallery.SelectedIndexChanged += LbTacticalGallery_SelectedIndexChanged;

            _prgChallenge = FindChild<XNAProgressBar>("prgChallenge");
            _prgChallenge.Maximum = 100;

            base.Initialize();

            ddGameModeMapFilter.Items.Clear();
            GameMode gm = GameModeMaps.GameModes.Find(gm => gm.Name == "Commander Challenge");
            ddGameModeMapFilter.AddItem(new XNADropDownItem
            {
                Text = gm.UIName,
                Tag = new GameModeMapFilter(() => GameModeMaps.Where(gmm => gmm.GameMode == gm).ToList())
            });

            WindowManager.CenterControlOnScreen(this);
        }

        private void LbTacticalGallery_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void AddNotice(string message, Color color)
        {
            XNAMessageBox.Show(WindowManager, "Message".L10N("Client:Main:MessageTitle"), message);
        }

        protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
        {
            if (false)
            {
                // 通关
                return 2;
            }
            return -1;
        }

        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            PlayerExtraOptionsPanel?.Disable();
            Disable();

            //_topBar.RemovePrimarySwitchable(this);
            ResetDiscordPresence();
        }

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null || Map == null || GameMode == null || !Initialized)
                return;

            int playerIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
            if (playerIndex >= MAX_PLAYER_COUNT || playerIndex < 0)
                return;

            XNAClientDropDown sideDropDown = ddPlayerSides[playerIndex];
            if (sideDropDown.SelectedItem == null)
                return;

            string side = (string)sideDropDown.SelectedItem.Tag;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "Setting Up";

            discordHandler.UpdatePresence(
                Map.UntranslatedName, GameMode.UntranslatedUIName, currentState, side, resetTimer);
        }

        protected override void UpdateMapPreviewBoxEnabledStatus()
        {
            MapPreviewBox.EnableContextMenu = !((Map != null && Map.ForceRandomStartLocations) || (GameMode != null && GameMode.ForceRandomStartLocations) || GetPlayerExtraOptions().IsForceRandomStarts);
            MapPreviewBox.EnableStartLocationSelection = MapPreviewBox.EnableContextMenu;
        }

        protected override bool AllowPlayerOptionsChange()
        {
            return true;
        }
    }
}
