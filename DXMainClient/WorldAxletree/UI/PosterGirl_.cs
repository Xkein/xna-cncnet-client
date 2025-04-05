using ClientCore;
using ClientGUI;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.WorldAxletree.UI
{
    class Posterhandle : XNAControl
    {
        static private List<PosterGirl> posterGirls = new List<PosterGirl>();
        static private List<PosterGirl_Event> posterGirl_Events = new List<PosterGirl_Event>();
        static private int currentEventIndex = -1;
        static private int lastNormalIndex = -1;
        static private int idleRate = -1;
        static private double oldTime = -1;
        static private bool overFirstIdle;
        static public Posterhandle posterHandle;
        static public RegInformation regInformation = new RegInformation();
        static public IniFile iniFile;
        static public List<PosterGirl_Helpers> globalHelperList;
        static public List<XNAContextMenu> allContextMenuList = new List<XNAContextMenu>();
        static private bool needContextMenu = false;
        static public AnimationType touchAnimType;
        static private List<List<PosterGirl_Helpers>> lastGroup = new List<List<PosterGirl_Helpers>>();
        static private List<PosterGirl_Tips> tipsList = new List<PosterGirl_Tips>();

        const int RenderTargetWidth = 960;
        const int RenderTargetHeight = 1024;

        static public List<PosterGirl_Helpers> LastGroup
        {
            get
            {
                if (lastGroup.Count > 0)
                {
                    List<PosterGirl_Helpers> group = lastGroup[lastGroup.Count - 1];
                    lastGroup.RemoveAt(lastGroup.Count - 1);
                    return group;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    lastGroup.Clear();
                }
                else
                {
                    lastGroup.Add(value);
                }
            }
        }

        #region RegInformation
        public struct RegInformation
        {
            public class RegInt
            {
                private int value;
                public string name;
                public RegInt(string myname)
                {
                    name = myname;
                    value = -1;
                }
                public static implicit operator int(RegInt me)
                {
                    return me.value;
                }
                public static RegInt operator +(RegInt me, int value)
                {
                    RegistryKey key;
                    key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                    key.SetValue(me.name, value);
                    me.value = value;
                    key.Close();
                    return me;
                }
            }
            public void ReadKeys()
            {
                currentGirlIndex = new RegInt("PosterGirlIndex");

                RegistryKey key;
                key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                int? index = (int?)key.GetValue(currentGirlIndex.name);

                if (index == null)
                {
                    index = iniFile.GetIntValue("General", "DefaultGirlIndex", 0);
                }
                currentGirlIndex += (int)index;
                key.Close();
            }
            public RegInt currentGirlIndex;
        }
        #endregion

        static public int GetRandomNum(int min, int max)
        {
            //return a value belongs to [min,max]
            return new Random().Next(min, max + 1);
        }
        public Posterhandle(WindowManager windowManager) : base(windowManager)
        {
        }
        static public PosterGirl_Event GetCurEvent()
        {
            return posterGirl_Events.Count > 0 && currentEventIndex >= 0 ? posterGirl_Events[currentEventIndex] : null;
        }
        static public PosterGirl GetCurGirl()
        {
            return posterGirls.Count > 0 ? posterGirls[regInformation.currentGirlIndex] : null;
        }
        public override void Initialize()
        {
            Name = "PosterGirl";
            string Poster_nullStr = string.Empty;
#if XNA
            iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "posterhandlexna.ini"));
#else
            iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "posterhandle.ini"));
#endif
            string value;

            //load events
            for (int i = 0; ; i++)
            {
                value = iniFile.GetStringValue("Events", i.ToString(), Poster_nullStr);
                if (value != Poster_nullStr)
                {
                    PosterGirl_Event postergirlEvent = new PosterGirl_Event(value);
                    posterGirl_Events.Add(postergirlEvent);
                }
                else break;
            }
            posterGirl_Events.Capacity = posterGirl_Events.Count;

            //load girls
            for (int i = 0; ; i++)
            {
                value = iniFile.GetStringValue("PosterGirls", i.ToString(), Poster_nullStr);
                if (value != Poster_nullStr)
                {
                    PosterGirl postergirl = new PosterGirl(value);
                    posterGirls.Add(postergirl);
                }
                else break;
            }
            posterGirls.Capacity = posterGirls.Count;

            PosterGirl_Helpers.Load("GeneralHelpers", out globalHelperList);

            touchAnimType = new AnimationType("TouchedAnimation", iniFile);


            regInformation.ReadKeys();

            foreach (PosterGirl pGirl in posterGirls)
            {
                pGirl.GetSomething();
            }
            foreach (List<PosterGirl_Helpers> helperList in PosterGirl_Helpers.allHelperlist)
            {
                foreach (PosterGirl_Helpers helper in helperList)
                {
                    helper.GetSomething();
                }
            }
            foreach (PosterGirl_EventFilter pFilter in PosterGirl_EventFilter.filterList)
            {
                pFilter.GetSomething();
            }

            PosterGirl girl = GetCurGirl();
            if (girl != null)
            {
                ClientRectangle = girl.touchArea;
                girl.Appear();
                idleRate = GetRandomNum(girl.rate_probability[0], girl.rate_probability[1]);
            }

            foreach (PosterGirl_Event pEvent in posterGirl_Events)
            {
                pEvent.GetSomething();
            }

            LeftClick += On_LeftClick;
            RightClick += On_RightClick;
            base.Initialize();

            GetAttributes(iniFile);

            overFirstIdle = false;

        }
        public void On_LeftClick(object sender, EventArgs e)
        {
            GetCurGirl().SelectEvent(Cursor.Location);
        }
        public void On_RightClick(object sender, EventArgs e)
        {
            if (!GetCurEvent().normal) return;

            XNAContextMenu contextMenu = GetCurGirl().contextMenu;

            contextMenu.ClearItems();
            foreach (PosterGirl_Helpers helper in GetCurGirl().extraHelperList)
            {
                contextMenu.AddItem(helper);
            }
            foreach (PosterGirl_Helpers helper in globalHelperList)
            {
                contextMenu.AddItem(helper);
            }

            contextMenu.Open(Cursor.Location - contextMenu.Parent.GetWindowPoint());
        }
        public void GirlChange(int index)
        {
            if (index < 0 || index >= posterGirls.Count)
            {
                throw new Exception("GirlIndex invalid");
            }
            GetCurGirl().Disappear();
            regInformation.currentGirlIndex += index;

            overFirstIdle = false;
            idleRate = GetRandomNum(GetCurGirl().rate_probability[0], GetCurGirl().rate_probability[1]);
            ClientRectangle = GetCurGirl().touchArea;
            GetCurGirl().Appear();
        }
        public static void EventChange(int index, bool appear)
        {
            if (index < 0 || index >= posterGirl_Events.Count)
            {
                throw new Exception("EventIndex invalid");
            }

            currentEventIndex = index;
            PosterGirl_Event curEvent = GetCurEvent();
            if (!curEvent.normal)
            {
                curEvent.filter?.HandleCounter(ref GetCurGirl().touchEvents);
            }
            curEvent.Speak(appear);
            bool showSpeakingStr = true;
            if (!appear && curEvent.normal)
            {
                showSpeakingStr = false;
            }
            if (showSpeakingStr)
            {
                string sayString = curEvent.speakWhat;
                if (!string.IsNullOrEmpty(sayString))
                {
                    PosterGirl_Tips tips = GetCurGirl().speakingTips;
                    tips.SetDuration(curEvent.speakWhat_duration);
                    tips.SetText(sayString, curEvent.speakWhat_color);
                    tips.Enable();
                }
            }
        }
        public override void Update(GameTime gameTime)
        {
            if (ProgramConstants.IsInGame == false)
            {
                GetCurEvent()?.Update(gameTime);

                PosterGirl girl = GetCurGirl();
                if (girl != null)
                {
                    girl.Update(gameTime);

                    PosterGirl_Tips tips = girl.speakingTips;
                    tips.Update(gameTime);
                }
            }
            base.Update(gameTime);

            if (needContextMenu)
            {
                XNAContextMenu contextMenu = GetCurGirl().contextMenu;
                contextMenu.Open(new Point(contextMenu.X, contextMenu.Y));
                needContextMenu = false;
            }
        }
        public override void Draw(GameTime gameTime)
        {
            Renderer.EndDraw();
            Renderer.BeginDraw();
            GetCurEvent()?.Draw();


            DrawChildren(gameTime);
        }
        public static void ContextMenu_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            XNAContextMenu contextMenu = GetCurGirl().contextMenu;
            PosterGirl_Helpers selectedHelper = (PosterGirl_Helpers)contextMenu.Items[e.ItemIndex];
            if (selectedHelper.clickSound != null)
            {
                selectedHelper.clickSound.Play();
            }
            selectedHelper.SelectEvent();

            List<PosterGirl_Helpers> group = null;

            if (selectedHelper.nextGroup != null)
            {
                group = selectedHelper.nextGroup;
                LastGroup = contextMenu.Items.Cast<PosterGirl_Helpers>().ToList();
                //LastGroup = contextMenu.Items.ConvertAll(helper => helper as PosterGirl_Helpers);
            }
            else if (selectedHelper.isBack)
            {
                group = LastGroup;
            }
            else
            {
                LastGroup = null;
                if (!selectedHelper.showInLabel)
                {
                    var msgBox = new XNAMessageBox(posterHandle.WindowManager, selectedHelper.Text,
                       selectedHelper.content, XNAMessageBoxButtons.OK);
                    msgBox.Show();
                }
                else
                {
                    PosterGirl_Tips tips = GetCurGirl().speakingTips;
                    tips.SetDuration(-1);
                    tips.SetText(selectedHelper.content, selectedHelper.color);
                    tips.Enable();
                }
            }

            if (group != null)
            {
                contextMenu.ClearItems();
                foreach (PosterGirl_Helpers helper in group)
                {
                    contextMenu.AddItem(helper);
                }
                int x = contextMenu.ClientRectangle.X;
                int y = contextMenu.ClientRectangle.Y;

                if (x + contextMenu.ClientRectangle.Width > posterHandle.WindowManager.RenderResolutionX)
                    x -= contextMenu.ClientRectangle.Width;

                if (y + contextMenu.ClientRectangle.Height > posterHandle.WindowManager.RenderResolutionY)
                    y = posterHandle.GetWindowRectangle().Height - contextMenu.ClientRectangle.Height;

                contextMenu.ClientRectangle = new Rectangle(x, y, contextMenu.ClientRectangle.Width, contextMenu.ClientRectangle.Height);
                needContextMenu = true;
            }
        }

        #region PosterGirl Helper
        public class PosterGirl_Helpers : XNAContextMenuItem
        {
            static public List<List<PosterGirl_Helpers>> allHelperlist = new List<List<PosterGirl_Helpers>>();
            static public List<string> listNames = new List<string>();
            public List<PosterGirl_Helpers> nextGroup;
            //public string description; now it is Text
            public SoundEffect clickSound;
            public Color color;
            public string content;
            public string name;
            public int[] clickEvents;
            public bool isBack;
            public bool showInLabel;
            private PosterGirl_Helpers(string name) : base()
            {
                string Poster_nullStr = string.Empty;
                this.name = name;

                isBack = false;

                string colorStr = iniFile.GetStringValue(name, "Color", "255,255,255");
                TextColor = AssetLoader.GetColorFromString(colorStr);
                colorStr = iniFile.GetStringValue(name, "ContentColor", "255,255,255");
                color = AssetLoader.GetColorFromString(colorStr);

                string textureStr = iniFile.GetStringValue(name, "Texture", Poster_nullStr);
                if (textureStr != string.Empty)
                {
                    Texture = AssetLoader.LoadTexture("PosterResource\\" + textureStr);
                }

                content = iniFile.GetStringValue(name, "Content", Poster_nullStr);
                Text = iniFile.GetStringValue(name, "Description", Poster_nullStr);

                content = content.Replace("@", Environment.NewLine);

                string nextGroupStr = iniFile.GetStringValue(name, "NextGroup", Poster_nullStr);
                Load(nextGroupStr, out nextGroup);

                string clickSoundStr = iniFile.GetStringValue(name, "ClickSound", Poster_nullStr);
                if (clickSoundStr != Poster_nullStr)
                {
                    clickSound = AssetLoader.LoadSound("PosterResource\\" + clickSoundStr);
                }

                showInLabel = iniFile.GetBooleanValue(name, "ShowInLabel", false);

            }

            public void GetSomething()
            {
                PosterGirl_EventFilter.LoadEvents(name, "ClickEvents", out clickEvents);
            }
            public void SelectEvent()
            {
                if (clickEvents == null)
                {
                    return;
                }
                if (!GetCurEvent().normal)
                {
                    return;
                }
                int clickEvent = clickEvents[GetRandomNum(0, clickEvents.Length - 1)];
                //EventChange(clickEvent, false);
                GetCurEvent().nextEvent = clickEvent;
            }
            public static void Load(string section, out List<PosterGirl_Helpers> list)
            {
                string Poster_nullStr = string.Empty;

                if (section == Poster_nullStr)
                {
                    list = null;
                    return;
                }

                list = allHelperlist.Find(helperlist => listNames[allHelperlist.IndexOf(helperlist)] == section);
                if (list != null)
                {
                    return;
                }
                /*
                foreach (string name in listNames)
                {
                    if(name == section)
                    {
                        list = allHelperlist[listNames.IndexOf(name)];
                        return;
                    }
                }
                */
                list = new List<PosterGirl_Helpers>();
                string value;

                value = iniFile.GetStringValue(section, "Back", Poster_nullStr);
                if (value != Poster_nullStr)
                {
                    PosterGirl_Helpers goBackHelper = new PosterGirl_Helpers(value)
                    {
                        isBack = true
                    };
                    list.Add(goBackHelper);
                }

                for (int i = 0; ; i++)
                {
                    value = iniFile.GetStringValue(section, i.ToString(), Poster_nullStr);
                    if (value != Poster_nullStr)
                    {
                        PosterGirl_Helpers helper = new PosterGirl_Helpers(value);
                        list.Add(helper);
                    }
                    else break;
                }
                list.Capacity = list.Count;

                allHelperlist.Add(list);
                listNames.Add(section);
            }
        }
        #endregion

        #region PosterGirl_Tips
        public class PosterGirl_Tips
        {
            private const string tipsDefaultSection = "SpeakingTipsDefault";
            private string name;
            private XNAPanel panel;
            private XNALabel label;
            private int duration = -1;
            private int startTime = -1;

            public void SetText(string text, Color color)
            {
                label.Text = text;
                label.RemapColor = color;
            }

            // Set how long it show, duration = -1 means show forever
            public void SetDuration(int duration)
            {
                this.duration = duration;
                startTime = -1;
            }
            static public PosterGirl_Tips GetTips(PosterGirl girl)
            {
                PosterGirl_Tips tips;
                tips = tipsList.Find(t => t.name == girl.name);
                return tips ?? new PosterGirl_Tips(girl.name);
            }
            private PosterGirl_Tips() : this(tipsDefaultSection) { }
            private PosterGirl_Tips(string section)
            {
                name = iniFile.GetStringValue(section, "SpeakingBubble", tipsDefaultSection);

                panel = new XNAPanel(posterHandle.WindowManager)
                {
                    Name = name
                };
                posterHandle.AddChild(panel);
                label = new XNALabel(posterHandle.WindowManager)
                {
                    Name = name
                };
                posterHandle.AddChild(label);

                Disable();
            }
            public void Disable()
            {
                panel.Disable();
                label.Disable();
            }
            public void Enable()
            {
                panel.Enable();
                label.Enable();
            }

            public void Update(GameTime gameTime)
            {
                if (startTime == -1)
                {
                    startTime = (int)gameTime.TotalGameTime.TotalMilliseconds;
                }
                else if (duration != -1 && (int)gameTime.TotalGameTime.TotalMilliseconds - startTime >= duration)
                {
                    Disable();
                    duration = -1;
                }

            }
        }
        #endregion

        #region PosterGirl
        public class PosterGirl
        {
            private int[] appearEvents;
            public int[] touchEvents;
            private int[] disappearEvents;
            private int[] idleEvents;
            public string name;
            public XNAContextMenu contextMenu;
            public Rectangle touchArea;
            public List<PosterGirl_Helpers> extraHelperList;
            public PosterGirl_Tips speakingTips;
            public int[] rate_probability;

            public PosterGirl(string name)
            {
                this.name = name;
                string Poster_nullStr = string.Empty;

                PosterGirl_EventFilter.LoadEvents(name, "AppearEvents", out appearEvents);
                PosterGirl_EventFilter.LoadEvents(name, "TouchEvents", out touchEvents);
                PosterGirl_EventFilter.LoadEvents(name, "DisappearEvents", out disappearEvents);
                PosterGirl_EventFilter.LoadEvents(name, "IdleEvents", out idleEvents);

                string[] touchAreaStr = iniFile.GetStringValue(name, "GirlRectangle", "0,0,0,0").Split(',');
                touchArea = new Rectangle(Convert.ToInt32(touchAreaStr[0]), Convert.ToInt32(touchAreaStr[1]),
                    Convert.ToInt32(touchAreaStr[2]), Convert.ToInt32(touchAreaStr[3]));

                string helperStr = iniFile.GetStringValue(name, "ExtraHelperList", Poster_nullStr);
                PosterGirl_Helpers.Load(helperStr, out extraHelperList);

                string[] probabilityStr = iniFile.GetStringValue(name, "IdleProbability", "0,0").Split(',');
                rate_probability = new int[] { Convert.ToInt32(probabilityStr[0]), Convert.ToInt32(probabilityStr[1]) };
            }
            public void GetSomething()
            {
                string Poster_nullStr = string.Empty;

                string contextMenuStr = iniFile.GetStringValue(name, "ContextMenu", "ContextMenuDefault");
                foreach (XNAContextMenu contextMenu in allContextMenuList)
                {
                    if (contextMenu.Name == contextMenuStr)
                    {
                        this.contextMenu = contextMenu;
                    }
                }
                if (contextMenu == null)
                {
                    contextMenu = new XNAContextMenu(posterHandle.WindowManager)
                    {
                        Name = contextMenuStr,
                        Enabled = false,
                        Visible = false,
                        ClientRectangle = new Rectangle(0, 0, 150, 2)
                    };
                    contextMenu.OptionSelected += ContextMenu_OptionSelected;
                    posterHandle.AddChild(contextMenu);
                    allContextMenuList.Add(contextMenu);
                }
                speakingTips = PosterGirl_Tips.GetTips(this);
            }
            public void Appear()
            {
                if (appearEvents == null)
                {
                    return;
                }

                int raV = GetRandomNum(0, appearEvents.Length - 1);
                Posterhandle.EventChange(appearEvents[raV], true);
            }
            public void Disappear()
            {
                if (disappearEvents == null)
                {
                    return;
                }
                int raV = GetRandomNum(0, disappearEvents.Length - 1);
                speakingTips.Disable();
                Posterhandle.EventChange(disappearEvents[raV], false);
            }
            public void SelectEvent(Point point)
            {
                if (!Posterhandle.GetCurEvent().normal) return;

                if (touchEvents == null)
                {
                    return;
                }

                List<int> allowIndexs = new List<int>();
                Rectangle area;
                foreach (int idx in touchEvents)
                {
                    area = (Posterhandle.posterGirl_Events[idx]).area;
                    if ((point.X >= area.X && point.X <= area.X + area.Width) &&
                        (point.Y >= area.Y && point.Y <= area.Y + area.Height))
                    {
                        allowIndexs.Add(idx);
                    }
                }
                if (allowIndexs.Count == 0) return;
                int nextEvent = allowIndexs[GetRandomNum(0, allowIndexs.Count - 1)];
                GetCurEvent().nextEvent = nextEvent;
            }
            public void Update(GameTime gameTime)
            {
                if (!overFirstIdle)
                {
                    overFirstIdle = true;
                    oldTime = gameTime.TotalGameTime.TotalMilliseconds;
                    idleRate = GetRandomNum(GetCurGirl().rate_probability[0], GetCurGirl().rate_probability[1]);
                }
                else if (rate_probability[1] > 0 && idleEvents.Length > 0 && lastNormalIndex == -1)
                {
                    double delta = gameTime.TotalGameTime.TotalMilliseconds - oldTime;
                    if (delta >= idleRate)
                    {
                        //oldTime = gameTime.TotalGameTime.TotalMilliseconds;
                        idleRate = GetRandomNum(GetCurGirl().rate_probability[0], GetCurGirl().rate_probability[1]);
                        lastNormalIndex = currentEventIndex;
                        EventChange(idleEvents[GetRandomNum(0, idleEvents.Length - 1)], false);
                    }
                }
            }
        }
        #endregion

        #region PosterGirl_EventFilter
        public class PosterGirl_EventFilter
        {
            public static List<PosterGirl_EventFilter> filterList = new List<PosterGirl_EventFilter>();
            private string name;
            private int[] additions;
            private int[] diminutions;
            private int handleCount;
            private int handleCounter;
            static public PosterGirl_EventFilter GetFilter(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;

                PosterGirl_EventFilter filter;
                filter = filterList.Find(f => f.name == name);
                return filter ?? new PosterGirl_EventFilter(name);
            }
            private PosterGirl_EventFilter(string name)
            {
                string Poster_nullStr = string.Empty;
                this.name = name;
                handleCounter = handleCount = iniFile.GetIntValue(name, "Count", 1);
                filterList.Add(this);
            }
            public void GetSomething()
            {
                LoadEvents(name, "Additions", out additions);
                LoadEvents(name, "Diminutions", out diminutions);
            }
            static public void LoadEvents(string name, string key, out int[] events)
            {
                string Poster_nullStr = string.Empty;
                string[] eventsStrs = iniFile.GetStringValue(name, key, Poster_nullStr).Split(',');

                if (eventsStrs[0] != Poster_nullStr)
                {
                    events = new int[eventsStrs.Length];
                    for (int j = 0; j < events.Length; j++)
                    {
                        events[j] = -1;
                    }

                    for (int j = 0; j < eventsStrs.Length; j++)
                    {
                        for (int i = 0; i < Posterhandle.posterGirl_Events.Count; i++)
                        {
                            if ((Posterhandle.posterGirl_Events[i]).name == eventsStrs[j])
                            {
                                events[j] = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    events = null;
                }
            }
            public void HandleCounter(ref int[] events)
            {
                if (--handleCounter == 0)
                {
                    handleCounter = handleCount;
                    var searchResult = from i in events where !(from o in diminutions select o).Contains(i) select i;
                    events = searchResult.ToArray();
                    events = events.Union(additions).ToArray();
                }
            }

        }


        #endregion

        #region PosterGirl Event
        public class PosterGirl_Event
        {
            public Rectangle area;
            public Rectangle drawRectangle;
            public List<SoundEffect> voices;
            public int nextEvent;
            public List<Texture2D> animations;
            public string name;
            public bool normal;
            public bool disappear;
            public string speakWhat;
            public int speakWhat_duration;
            public Color speakWhat_color;
            public PosterGirl_EventFilter filter;

            public Point frameSize;
            public Point sheetSize;
            public int frameRate;
            // dynamic
            public int currentAnimIndex = 0;

            public Point currentFrame = new Point(0, 0);
            public int msecond = -1;

            public PosterGirl_Event(string name)
            {
                string Poster_nullStr = string.Empty;
                this.name = name;

                nextEvent = -1;
                voices = new List<SoundEffect>();
                animations = new List<Texture2D>();

                string[] areaStr = iniFile.GetStringValue(name, "Area", Poster_nullStr).Split(',');
                if (areaStr[0] != Poster_nullStr)
                {
                    area = new Rectangle(Convert.ToInt32(areaStr[0]), Convert.ToInt32(areaStr[1]),
                        Convert.ToInt32(areaStr[2]), Convert.ToInt32(areaStr[3]));
                }

                string[] drawRectangleStr = iniFile.GetStringValue(name, "DrawRectangle", Poster_nullStr).Split(',');
                if (drawRectangleStr[0] != Poster_nullStr)
                {
                    drawRectangle = new Rectangle(Convert.ToInt32(drawRectangleStr[0]), Convert.ToInt32(drawRectangleStr[1]),
                        Convert.ToInt32(drawRectangleStr[2]), Convert.ToInt32(drawRectangleStr[3]));
                }
                else
                {
                    drawRectangle = Rectangle.Empty;
                }

                string[] voicesStr = iniFile.GetStringValue(name, "Voices", Poster_nullStr).Split(',');
                SoundEffect soundEffect;
                foreach (string voiceStr in voicesStr)
                {
                    if (voiceStr != Poster_nullStr)
                    {
                        soundEffect = AssetLoader.LoadSound("PosterResource\\" + voiceStr);
                        voices.Add(soundEffect);
                    }
                }
                voices.Capacity = voices.Count;

                string[] animsStr = iniFile.GetStringValue(name, "Animations", Poster_nullStr).Split(',');
                Texture2D animation;
                foreach (string animStr in animsStr)
                {
                    if (animStr != Poster_nullStr)
                    {
                        animation = AssetLoader.LoadTexture("PosterResource\\" + animStr);
                        //Logger.Log(animStr + "::" + animation.Width.ToString() + "," + animation.Height.ToString());
                        animations.Add(animation);
                    }
                }
                animations.Capacity = animations.Count;

                normal = iniFile.GetBooleanValue(name, "Normal", false);
                disappear = iniFile.GetBooleanValue(name, "Disappear", false);

                string[] frameSizeStr = iniFile.GetStringValue(name, "FrameSize", "0,0").Split(',');
                frameSize = new Point(Convert.ToInt32(frameSizeStr[0]), Convert.ToInt32(frameSizeStr[1]));
                if (frameSize == new Point(0, 0))
                {
                    frameSize = new Point((animations[0]).Width, (animations[0]).Height);
                }
                string[] sheetSizeStr = iniFile.GetStringValue(name, "SheetSize", "1,1").Split(',');
                sheetSize = new Point(Convert.ToInt32(sheetSizeStr[0]), Convert.ToInt32(sheetSizeStr[1]));

                frameRate = iniFile.GetIntValue(name, "FrameRate", 20);

                speakWhat = iniFile.GetStringValue(name, "SayString", Poster_nullStr);
                speakWhat = speakWhat.Replace("@", Environment.NewLine);

                string colorStr = iniFile.GetStringValue(name, "SayString.Color", "255,255,255");
                speakWhat_color = AssetLoader.GetColorFromString(colorStr);

                speakWhat_duration = iniFile.GetIntValue(name, "SayString.Duration", sheetSize.X * sheetSize.Y * frameRate);

                filter = PosterGirl_EventFilter.GetFilter(iniFile.GetStringValue(name, "Filter", Poster_nullStr));
            }
            public void GetSomething()
            {
                string Poster_nullStr = string.Empty;

                string nextEventName = iniFile.GetStringValue(name, "NextEvent", Poster_nullStr);
                if (!string.IsNullOrEmpty(nextEventName))
                {
                    for (int i = 0; i < Posterhandle.posterGirl_Events.Count; i++)
                    {
                        if ((Posterhandle.posterGirl_Events[i]).name == nextEventName)
                        {
                            nextEvent = i;
                            break;
                        }
                    }
                }
                Rectangle clientRectangle = posterHandle.ClientRectangle;
                area.X += clientRectangle.X;
                area.Y += clientRectangle.Y;

                if (drawRectangle == Rectangle.Empty)
                {
                    drawRectangle = posterHandle.ClientRectangle;
                }
            }
            public void Draw()
            {
                if (currentAnimIndex >= 0)
                {
                    Rectangle rectangle = new Rectangle(currentFrame.X * frameSize.X,
                        currentFrame.Y * frameSize.Y, frameSize.X, frameSize.Y);

                    Renderer.DrawTexture(animations[currentAnimIndex],
                        rectangle,
                        drawRectangle,
                        Color.White);
                }

            }
            public void Speak(bool appear)
            {
                if ((appear || !normal) && voices.Count > 0)
                {
                    voices[GetRandomNum(0, voices.Count - 1)]?.Play();
                }
            }
            public void Update(GameTime gameTime)
            {
                if (normal && nextEvent != -1)
                {
                    ;
                }
                else if (msecond < 0)
                {
                    msecond = (int)gameTime.TotalGameTime.TotalMilliseconds;
                    return;
                }
                else if ((int)gameTime.TotalGameTime.TotalMilliseconds - msecond >= frameRate)
                {
                    msecond = (int)gameTime.TotalGameTime.TotalMilliseconds;
                }
                else return;

                //System.Diagnostics.Debug.WriteLineIf(currentEventIndex == 2, ((int)gameTime.TotalGameTime.TotalMilliseconds - msecond).ToString());
                if (++currentFrame.X >= sheetSize.X)
                {
                    currentFrame.X = 0;
                    if (++currentFrame.Y >= sheetSize.Y)
                    {
                        currentFrame.Y = 0;
                        if (++currentAnimIndex >= animations.Count)
                        {
                            currentAnimIndex = 0;
                            msecond = -1;

                            if (normal && nextEvent == -1)
                            {
                                if (lastNormalIndex != -1)
                                {
                                    Posterhandle.EventChange(lastNormalIndex, false);
                                    oldTime = gameTime.TotalGameTime.TotalMilliseconds;
                                    lastNormalIndex = -1;
                                }
                            }
                            else if (disappear)
                            {
                                posterHandle.ClientRectangle = GetCurGirl().touchArea;
                                GetCurGirl().Appear();
                            }
                            else
                            {
                                Posterhandle.EventChange(nextEvent, false);
                                oldTime = gameTime.TotalGameTime.TotalMilliseconds;
                                if (normal)
                                {
                                    nextEvent = -1;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

    }


}
