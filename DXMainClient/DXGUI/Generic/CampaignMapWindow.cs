using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using DTAClient.Domain;
using System.IO;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignMapWindow : CampaignSelector
    {
        class CampaignMapSelector : XNAClientButton
        {
            public CampaignMapSelector(WindowManager windowManager, Mission mission)
                : base(windowManager)
            {
                Name = "CampaignSelector";

                Mission = mission;
            }
            public Texture2D MissionThumbnail { get; private set; }
            public Texture2D MissionDetailBackground { get; private set; }
            public Texture2D MissionDetailBackgroundHard { get; private set; }
            public Mission Mission { get; private set; }

            public override void Initialize()
            {
                if (!string.IsNullOrEmpty(Mission.DetailBackgroundName))
                {
                    MissionDetailBackground = AssetLoader.LoadTexture(RESOURCE_PATH + Mission.DetailBackgroundName);
                    MissionDetailBackgroundHard = AssetLoader.LoadTexture(RESOURCE_PATH + "h" + Mission.DetailBackgroundName);
                }
                MissionThumbnail = AssetLoader.LoadTexture(RESOURCE_PATH + Mission.ThumbnailName);

                base.Initialize();
                SetPosition(Mission.Location);
            }

            public void SetPosition(Point p)
            {
                int width = ClientRectangle.Width;
                int height = ClientRectangle.Height;

                ClientRectangle = new Rectangle(p.X - width / 2,
                    p.Y - height / 2,
                    width, height);
            }
        }

        private static List<CampaignMapSelector> campaignMapSelectors = new List<CampaignMapSelector>();
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;
        private const string RESOURCE_PATH = "CampaignResource\\";

        public CampaignMapWindow(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager, discordHandler)
        {
        }

        private List<Mission> UnavailableMissions { get; set; } = new List<Mission>();
        private List<XNAClientButton> BattleAreas { get; set; } = new List<XNAClientButton>();
        private List<string> Times { get; set; } = new List<string>();
        private List<int> SelectedSides { get; set; } = new List<int>();
        private XNATrackbar trbTimeSelector;
        private IniFile mapOptionsIni;

        private CampaignMapSelector CurSelector { get; set; }

        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;
        private XNALabel lblDifficultyLevel;
        private XNALabel lblEasy;
        private XNALabel lblNormal;
        private XNALabel lblHard;

        private Mission MissionSelected
        {
            get
            {
                return lbCampaignList.SelectedIndex >= 0 ? Missions[lbCampaignList.SelectedIndex] : null;
            }
            set
            {
                if (value == null)
                {
                    lbCampaignList.SelectedIndex = -1;
                    return;
                }
                lbCampaignList.SelectedIndex = Missions.FindIndex(m => m == value);
            }
        }

        private struct MissionDetailWindow
        {
            public XNAPanel pMissionDetailPanel;
            public XNAPanel tMissionThumbnail;
            public XNAClientCheckBox chkDifficultyChecker;
            public bool useDifficultyChecker;
            public void Enable()
            {
                pMissionDetailPanel.Enable();
            }
            public void Disable()
            {
                pMissionDetailPanel.Disable();
            }
            public void AddChild(XNAControl control)
            {
                pMissionDetailPanel.AddChild(control);
            }
        };

        private struct BattleAreaWindow
        {
            public XNAPanel pBackgroundPanel;
            public XNAClientButton btnCancel;

            public void Enable()
            {
                pBackgroundPanel.Enable();
            }
            public void Disable()
            {
                pBackgroundPanel.Disable();
            }
            public void AddChild(XNAControl control)
            {
                pBackgroundPanel.AddChild(control);
            }
        };

        MissionDetailWindow detailWindow;
        BattleAreaWindow battleAreaWindow;

        public override void Initialize()
        {
            Name = "CampaignMapSelector";
            BackgroundTexture = AssetLoader.LoadTexture(RESOURCE_PATH + "worldmapbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);

            //mapOptionsIni = new IniFile(ProgramConstants.GetResourcePath() + Name + ".ini");


            // init battle area window
            {
                var pBackgroundPanel = new XNAPanel(WindowManager);
                pBackgroundPanel.Name = "pBattleAreaWindow";
                pBackgroundPanel.ClientRectangle = ClientRectangle;
                pBackgroundPanel.LeftClick += CampaignMap_LeftClick;
                pBackgroundPanel.RightClick += CampaignMap_LeftClick;
                pBackgroundPanel.Disable();

                battleAreaWindow.pBackgroundPanel = pBackgroundPanel;

                var btnBattleAreaCancel = new XNAClientButton(WindowManager);
                btnBattleAreaCancel.Name = "btnBattleAreaCancel";
                btnBattleAreaCancel.ClientRectangle = new Rectangle(pBackgroundPanel.Width - 145,
                pBackgroundPanel.Height - 30, 133, 23);
                btnBattleAreaCancel.Text = "Cancel";
                btnBattleAreaCancel.LeftClick += BtnBattleAreaCancel_LeftClick;

                battleAreaWindow.btnCancel = btnBattleAreaCancel;

                battleAreaWindow.AddChild(btnBattleAreaCancel);
            }

            // init mission detail window
            {
                var pMissionDetailPanel = new XNAPanel(WindowManager);
                pMissionDetailPanel.Name = "pMissionDetailWindow";
                pMissionDetailPanel.ClientRectangle = new Rectangle(0, 0, 390, 500);
                pMissionDetailPanel.Tag = new Point(0, 0);
                pMissionDetailPanel.Disable();

                detailWindow.pMissionDetailPanel = pMissionDetailPanel;

                var tMissionThumbnail = new XNAPanel(WindowManager);
                tMissionThumbnail.Name = "tMissionThumbnail";
                tMissionThumbnail.ClientRectangle = new Rectangle(10, 10, 370, 300);
                tMissionThumbnail.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

                detailWindow.tMissionThumbnail = tMissionThumbnail;

                var chkDifficultyChecker = new XNAClientCheckBox(WindowManager);
                chkDifficultyChecker.Name = "chkDifficultyChecker";
                chkDifficultyChecker.ClientRectangle = new Rectangle(tMissionThumbnail.ClientRectangle.Center.X,
                    tMissionThumbnail.ClientRectangle.Bottom + 10, 170, 50);
                chkDifficultyChecker.CheckedChanged += ChkDifficultyChecker_CheckedChanged;
                detailWindow.chkDifficultyChecker = chkDifficultyChecker;

                detailWindow.AddChild(chkDifficultyChecker);
                detailWindow.AddChild(tMissionThumbnail);

                // add detailWindow to battleAreaWindow
                battleAreaWindow.AddChild(pMissionDetailPanel);
            }

            trbTimeSelector = new XNATrackbar(WindowManager);
            trbTimeSelector.Name = "trbTimeSelector";
            trbTimeSelector.ClientRectangle = new Rectangle(ClientRectangle.Width / 2,
                ClientRectangle.Height - 90,
                ClientRectangle.Width / 2, 30);
            trbTimeSelector.MinValue = 0;
            trbTimeSelector.MaxValue = 2;
            trbTimeSelector.ValueChanged += TrbTimeSelector_ValueChanged;
            //trbTimeSelector.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), trbTimeSelector.Width, trbTimeSelector.Height);
            trbTimeSelector.ButtonTexture = AssetLoader.LoadTextureUncached(RESOURCE_PATH + "trackbarButton_time.png");

            // add to two control
            //battleAreaWindow.AddChild(trbTimeSelector); // TODO
            AddChild(trbTimeSelector);

            AddChild(battleAreaWindow.pBackgroundPanel);
            base.Initialize();

            AddMapSelectors();

            lbCampaignList = Children.First(c => c.Name == "lbCampaignList") as XNAListBox;
            btnLaunch = Children.First(c => c.Name == "btnLaunch") as XNAClientButton;
            tbMissionDescription = Children.First(c => c.Name == "tbMissionDescription") as XNATextBlock;
            trbDifficultySelector = Children.First(c => c.Name == "trbDifficultySelector") as XNATrackbar;
            lblDifficultyLevel = Children.First(c => c.Name == "lblDifficultyLevel") as XNALabel;
            lblEasy = Children.First(c => c.Name == "lblEasy") as XNALabel;
            lblNormal = Children.First(c => c.Name == "lblNormal") as XNALabel;
            lblHard = Children.First(c => c.Name == "lblHard") as XNALabel;

            // move them to end
            RemoveChild(battleAreaWindow.pBackgroundPanel);
            AddChild(battleAreaWindow.pBackgroundPanel);

            // remove them from main window
            RemoveChild(tbMissionDescription);
            RemoveChild(lblDifficultyLevel);
            RemoveChild(btnLaunch);
            RemoveChild(trbDifficultySelector);
            RemoveChild(lblEasy);
            RemoveChild(lblNormal);
            RemoveChild(lblHard);

            detailWindow.AddChild(btnLaunch);
            detailWindow.AddChild(trbDifficultySelector);
            detailWindow.AddChild(tbMissionDescription);
            detailWindow.AddChild(lblDifficultyLevel);

            RefreshSelector();
        }

        private void TrbTimeSelector_ValueChanged(object sender, EventArgs e)
        {
            RefreshSelector();
        }

        private void ChkDifficultyChecker_CheckedChanged(object sender, EventArgs e)
        {
            var ChkDifficultyChecker = (XNAClientCheckBox)sender;
            trbDifficultySelector.Value = ChkDifficultyChecker.Checked ? trbDifficultySelector.MaxValue : trbDifficultySelector.MinValue;
            detailWindow.pMissionDetailPanel.BackgroundTexture =
                detailWindow.chkDifficultyChecker.Checked ? CurSelector.MissionDetailBackgroundHard : CurSelector.MissionDetailBackground;
        }

        private void BtnSideButton_CheckedChanged(object sender, EventArgs e)
        {
            var btnSideButton = (XNAClientCheckBox)sender;
            if (btnSideButton.Checked)
            {
                if ((int)btnSideButton.Tag == -1)
                {
                    ShowExtraMission = true;
                }
                SelectedSides.Add((int)btnSideButton.Tag);
            }
            else
            {
                if ((int)btnSideButton.Tag == -1)
                {
                    ShowExtraMission = false;
                }
                SelectedSides.Remove((int)btnSideButton.Tag);
            }
            RefreshSelector();
        }

        string CurBattleAreaName { get; set; } = string.Empty;
        bool ShowExtraMission { get; set; } = false;

        private void BattleArea_LeftClick(object sender, EventArgs e)
        {
            var BattleArea = (XNAClientButton)sender;
            CurBattleAreaName = BattleArea.Name;
            battleAreaWindow.pBackgroundPanel.BackgroundTexture = (Texture2D)BattleArea.Tag;

            battleAreaWindow.Enable();
            RefreshSelector();
        }

        private void CampaignMap_LeftClick(object sender, EventArgs e)
        {
            CurSelector = null;
            detailWindow.Disable();
        }

        private void Selector_LeftClick(object sender, EventArgs e)
        {
            var selector = (CampaignMapSelector)sender;
            MissionSelected = selector.Mission;
            var pMissionDetailPanel = detailWindow.pMissionDetailPanel;

            Point relativeOffset = (Point)pMissionDetailPanel.Tag;
            pMissionDetailPanel.X = selector.X + relativeOffset.X;
            pMissionDetailPanel.Y = selector.Y + relativeOffset.Y;
            if (pMissionDetailPanel.X + pMissionDetailPanel.Width > ClientRectangle.Width)
            {
                pMissionDetailPanel.X = ClientRectangle.Width - pMissionDetailPanel.Width;
            }
            if (pMissionDetailPanel.Y + pMissionDetailPanel.Height > ClientRectangle.Height)
            {
                pMissionDetailPanel.Y = ClientRectangle.Height - pMissionDetailPanel.Height;
            }

            Mission mission = selector.Mission;

            tbMissionDescription.Text = mission.GUIDescription;
            detailWindow.tMissionThumbnail.BackgroundTexture = selector.MissionThumbnail;
            if (selector.MissionDetailBackground != null)
            {
                pMissionDetailPanel.BackgroundTexture = detailWindow.chkDifficultyChecker.Checked ? selector.MissionDetailBackgroundHard : selector.MissionDetailBackground;
            }

            CurSelector = selector;

            btnLaunch.AllowClick = CurSelector.Mission.Enabled;
            detailWindow.Enable();
        }
        private void RefreshSelector()
        {
            for (int idx = 0; idx < campaignMapSelectors.Count; idx++)
            {
                CampaignMapSelector selector = campaignMapSelectors[idx];
                Mission mission = selector.Mission;
                if (mission.Time == Times[trbTimeSelector.Value]
                    && mission.BattleAreaName == CurBattleAreaName
                    && SelectedSides.Contains(mission.Side)
                    && (!mission.IsExtra || ShowExtraMission))
                {
                    selector.Enable();
                }
                else
                {
                    selector.Disable();
                }
            }

            CurSelector = null;

            detailWindow.Disable();
        }
        private void BtnBattleAreaCancel_LeftClick(object sender, EventArgs e)
        {
            detailWindow.Disable();
            battleAreaWindow.Disable();
        }

        private void AddMapSelectors()
        {
            foreach (var mission in Missions)
            {
                if (mission.Available())
                {
                    CampaignMapSelector selector = new CampaignMapSelector(WindowManager, mission);
                    selector.GetAttributes(mapOptionsIni);
                    selector.LeftClick += Selector_LeftClick;
                    campaignMapSelectors.Add(selector);
                    // add them to battle area window
                    battleAreaWindow.AddChild(selector);
                }
                else
                {
                    UnavailableMissions.Add(mission);
                }

            }
        }


        protected override void SetAttributesFromIni()
        {
            Name = "CampaignMapSelector"; // override campaignSelector
            base.SetAttributesFromIni();
        }

        protected override void GetINIAttributes(IniFile iniFile)
        {
            mapOptionsIni = iniFile;

            detailWindow.useDifficultyChecker = iniFile.GetBooleanValue(Name, "UseDifficultyChecker", false);

            string[] sBattleAreaNames = iniFile.GetStringValue(Name, "BattleArea", string.Empty).Split(',');
            foreach (string sBattleAreaName in sBattleAreaNames)
            {
                if (string.IsNullOrEmpty(sBattleAreaName) == false)
                {
                    var BattleArea = new XNAClientButton(WindowManager);
                    BattleArea.Name = sBattleAreaName;
                    BattleArea.LeftClick += BattleArea_LeftClick;
                    BattleArea.Tag = AssetLoader.LoadTexture(iniFile.GetStringValue(sBattleAreaName, "BattleAreaTexture", string.Empty));
                    BattleAreas.Add(BattleArea);
                    AddChild(BattleArea);
                }
            }

            string[] sTimes = iniFile.GetStringValue(Name, "Times", "present").Split(',');
            foreach (string time in sTimes)
            {
                if (string.IsNullOrEmpty(time) == false)
                {
                    Times.Add(time);
                }
            }

            trbTimeSelector.MaxValue = Times.Count - 1;

            var pMissionDetailPanel = detailWindow.pMissionDetailPanel;
            string[] sRelativeOffset = iniFile.GetStringValue(pMissionDetailPanel.Name, "RelativeOffset", "0,0").Split(',');
            pMissionDetailPanel.Tag = new Point(Convert.ToInt32(sRelativeOffset[0]), Convert.ToInt32(sRelativeOffset[1]));


            string[] sSideButtons = iniFile.GetStringValue(Name, "SideButtons", string.Empty).Split(',');
            foreach (string sSideButton in sSideButtons)
            {
                if (string.IsNullOrEmpty(sSideButton) == false)
                {
                    var btnSideButton = new XNAClientCheckBox(WindowManager);
                    btnSideButton.Name = sSideButton;
                    btnSideButton.Tag = iniFile.GetIntValue(sSideButton, "Side", -1);
                    btnSideButton.ClientRectangle = new Rectangle(12, ClientRectangle.Height / 2, 33, 46);
                    btnSideButton.CheckedChanged += BtnSideButton_CheckedChanged;

                    // add to two control
                    //battleAreaWindow.AddChild(btnSideButton);
                    AddChild(btnSideButton);
                }
            }

            base.GetINIAttributes(iniFile);
        }

    }
}
