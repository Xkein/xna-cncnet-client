using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DTAClient.WorldAxletree.UI
{
    class PosterManager : XNAControl
    {
        #region ImportLive2DFunction
        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetWindowHandle(IntPtr hWnd);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_InitManager();

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_Render();

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_OnTouchesBegan(float px, float py);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_OnTouchesMoved(float px, float py);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_OnTouchesEnded(float px, float py);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetRenderScale(float x, float y);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetRenderSize(int width, int height);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_OpenScene(IntPtr dir, IntPtr name);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_Release();

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetReceiverFunction(IntPtr receiverFunction);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_StartRandomMotion(IntPtr group, int priority);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetExpression(IntPtr expressionID);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetRenderDelay(double delay);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetBackBufferFormat(SurfaceFormat format);

        [DllImport("Live2DManager.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void L2DManager_SetShowFPS(bool flag);
        #endregion

        delegate void ReceiverFunctionProc(IntPtr pData);

        static int RenderTargetWidth = 960;
        static int RenderTargetHeight = 1024;

        static public readonly string ResourcePath = "PosterResource\\";

        static PosterManager instance = null;
        List<PosterGirl> posterGirls = new List<PosterGirl>();
        ReceiverFunctionProc receiverFunctionProc;
        Texture2D l2dTexture;
        Texture2D L2dTexture
        {
            get
            {
                if (l2dTexture == null)
                {
                    l2dTexture = new Texture2D(GraphicsDevice, RenderTargetWidth, RenderTargetHeight, false,
#if XNA
                        SurfaceFormat.Color
#else
                        SurfaceFormat.Bgra32
#endif
                    );
                }
                return l2dTexture;
            }
            set
            {
                l2dTexture = value;
            }
        }
        int[] l2dData;

        int currentGirlIndex = -1;
        int nextGirlIndex = -1;
        int currentGirlOutTimer = 0;
        double renderDelay = 0.0;
        PosterManagerDropDownItem selectingItem;
        XNAContextMenu curChildMenu = null;
        AnimationType touchAnimType;
        Random random = new Random();
        SpeakingBubble bubble;
        AnimationManager animationManager;

        float voiceVolume = 1.0f;
        PosterGirl GetCurGirl()
        {
            return currentGirlIndex >= 0 && posterGirls.Count > 0 ? posterGirls[currentGirlIndex] : null;
        }

        static public Rectangle IniGetRectangleValue(IniFile iniFile, string section, string key)
        {
            string[] str = iniFile.GetStringValue(section, key, "0,0,0,0").Split(',');
            return new Rectangle(int.Parse(str[0]), int.Parse(str[1]),
                 int.Parse(str[2]), int.Parse(str[3]));
        }

        static public PosterManager GetInstance(WindowManager windowManager)
        {
            if (instance == null)
                instance = new PosterManager(windowManager);
            return instance;
        }

        PosterManager(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = "PosterGirlManager";

            IniFile iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), Name + ".ini"));
            LoadINI(iniFile);

            if (posterGirls.Count > 0)
            {
#if GL
                L2DManager_SetWindowHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
#else
                L2DManager_SetWindowHandle(WindowManager.GetWindowHandle());
#endif
                L2DManager_SetRenderSize(RenderTargetWidth, RenderTargetHeight);
                l2dData = new int[RenderTargetWidth * RenderTargetHeight];

                L2DManager_SetShowFPS(iniFile.GetBooleanValue(Name, "ShowFPS", false));

                L2DManager_InitManager();
                //L2DManager_SetBackBufferFormat((SurfaceFormat)32);
                receiverFunctionProc += ReceiverFunction;
                L2DManager_SetReceiverFunction(Marshal.GetFunctionPointerForDelegate(receiverFunctionProc));

                LeftClick += On_LeftClick;
                RightClick += On_RightClick;

                ChangeGirl(0);
            }
            base.Initialize();
            GetAttributes(iniFile);
        }

        private void LoadINI(IniFile iniFile)
        {
            string value;

            var posterGirlStrs = iniFile.GetSectionKeys("PosterGirls");
            if (posterGirlStrs != null)
            {
                string posterGirlGroup = string.Empty;
                foreach (var key in posterGirlStrs)
                {
                    value = iniFile.GetStringValue("PosterGirls", key, string.Empty);
                    if (value != string.Empty)
                    {
                        posterGirls.Add(new PosterGirl(WindowManager, iniFile, value));
                        posterGirlGroup += value + " ";
                    }
                }


                selectingItem = new PosterManagerDropDownItem(WindowManager, iniFile, "SelectingItem");
                selectingItem.LoadNextGroup(WindowManager, iniFile, posterGirlGroup.Trim().Replace(" ", ","));
            }

            posterGirls.ForEach(girl =>
            {
                girl.Menu.AddItem(selectingItem);
                girl.Menu.OptionSelected += ContextMenu_OptionSelected;
                AddChild(girl.Menu);

                Action<XNAContextMenuItem> action = null;
                action = item =>
                {
                    XNAContextMenu nextMenu = ((PosterManagerDropDownItem)item).NextMenu;
                    if (nextMenu != null)
                    {
                        AddChild(nextMenu);
                        nextMenu.Items.ForEach(action);
                    }
                };

                girl.Menu.Items.ForEach(action);
            });

            RenderTargetWidth = iniFile.GetIntValue(Name, "RenderTargetWidth", 960);
            RenderTargetHeight = iniFile.GetIntValue(Name, "RenderTargetHeight", 1024);

            renderDelay = iniFile.GetDoubleValue(Name,
#if XNA
                    "RenderDelayXNA", 0.05
#else
                    "RenderDelay", 0.02
#endif
                );

            animationManager = new AnimationManager(WindowManager);
            AddChild(animationManager);
            animationManager.LoadAnimations(iniFile);

            touchAnimType = new AnimationType("TouchedAnimation", iniFile);

            bubble = new SpeakingBubble(WindowManager, iniFile, "SpeakingBubble");
        }

        public override void Kill()
        {
            if (posterGirls.Count > 0)
            {
                posterGirls.Clear();
                L2DManager_Release();
            }
            base.Kill();
        }

#if XNA
        //unsafe
#endif
        static void ReceiverFunction(IntPtr pData)
        {
            var buffer = instance.l2dData;
            int length = buffer.Length;
            Marshal.Copy(pData, buffer, 0, buffer.Length);

#if XNA
            for (int i = 0; i < length; i++)
            {
                var tmp = buffer[i];
                buffer[i] = (int)((tmp & 0xFF00FF00u) | ((tmp >> 16) & 0x000000FFu) | ((tmp << 16) & 0x00FF0000u));
            }
            /*
            Color* data = (Color*)pData.ToPointer();
            Color* pBuffer = (Color*)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
            for (int i = 0; i < length; i++)
            {
                pBuffer->R = data->B;
                pBuffer->B = data->R;
                data++;
                pBuffer++;
            }*/
#endif
            instance.L2dTexture.SetData(buffer);
            /*
#if XNA
            Color* data = (Color*)pData.ToPointer();
            for (int i = 0; i < length; i++)
            {
                // slow
                colors[i] = new Color(data->B, data->G, data->R, data->A);
#else
            colors[i] = *data;
#endif
                data++;
            }
            instance.L2dTexture.SetData(colors);
            */
        }
        void ChangeGirl(int index)
        {
            PosterGirl girl = GetCurGirl();
            if (girl != null)
            {
                girl.Disappear();
                currentGirlOutTimer = girl.OutTime;
            }
            nextGirlIndex = index;
        }
        public override void Draw(GameTime gameTime)
        {
            PosterGirl girl = GetCurGirl();
            if (girl != null)
            {
                L2DManager_SetRenderDelay(ProgramConstants.IsInGame ? 0.25 : renderDelay);
                L2DManager_Render();
                Renderer.DrawTexture(L2dTexture, ClientRectangle, Color.White);
            }
            DrawChildren(gameTime);
        }
        public override void Update(GameTime gameTime)
        {
            PosterGirl girl = null;
            if (nextGirlIndex >= 0)
            {
                if (currentGirlOutTimer <= 0)
                {
                    girl = posterGirls[nextGirlIndex];

                    IntPtr dirBuf = Marshal.StringToHGlobalAnsi(ProgramConstants.GetBaseResourcePath() + ResourcePath + girl.Name + "\\");
                    IntPtr nameBuf = Marshal.StringToHGlobalAnsi(girl.ModelName);
                    L2DManager_OpenScene(dirBuf, nameBuf);
                    Marshal.FreeHGlobal(dirBuf);
                    Marshal.FreeHGlobal(nameBuf);

                    girl.Appear();
                    ClientRectangle = girl.GirlRectangle;
                    L2DManager_SetRenderScale(girl.ScaleX, girl.ScaleY);

                    currentGirlIndex = nextGirlIndex;
                    nextGirlIndex = -1;
                }
                else
                {
                    currentGirlOutTimer--;
                }
            }

            girl = GetCurGirl();
            if (girl != null)
            {
                float px, py;
                GetRelativePointToCenter(out px, out py);
                L2DManager_OnTouchesMoved(px, py);

                var menu = girl.Menu;
                int index = GetItemIndexOnCursor(menu);
                if (index > 0)
                {
                    var nextChildMenu = ((PosterManagerDropDownItem)menu.Items[index]).NextMenu;
                    if (curChildMenu != nextChildMenu)
                    {
                        curChildMenu?.Disable();
                        curChildMenu = nextChildMenu;
                        if (curChildMenu != null)
                        {
                            curChildMenu = nextChildMenu;
                            ShowMenu(curChildMenu);
                        }
                    }
                }
                bubble.Update(gameTime);
            }
            base.Update(gameTime);
        }
        static int GetItemIndexOnCursor(XNAContextMenu menu)
        {
            if (menu.Visible)
            {
                Point p = menu.GetCursorPoint();

                Rectangle displayRectangle = menu.GetWindowRectangle();

                if (p.X < 0 || p.X > menu.ClientRectangle.Width ||
                    p.Y > menu.ClientRectangle.Height ||
                    p.Y < 0)
                {
                    return -1;
                }

                int y = p.Y;
                int itemIndex = y / menu.ItemHeight;

                if (itemIndex < menu.Items.Count && itemIndex > -1)
                {
                    return itemIndex;
                }
            }
            return -1;
        }

        void GetRelativePointToCenter(out float px, out float py)
        {
            var point = GetCursorPoint();
            px = point.X;
            py = point.Y;
            PosterGirl posterGirl = GetCurGirl();
            if (posterGirl != null)
            {
                var center = GetCurGirl().Center;
                px = point.X - center.X;
                py = point.Y - center.Y;
            }
            if (px < 0.0f)
            {
                px = 0.0f;
            }
            if (py < 0.0f)
            {
                py = 0.0f;
            }
        }
        public void On_LeftClick(object sender, EventArgs e)
        {
            //GetCurGirl().SelectEvent(Cursor.Location);
            float px, py;
            GetRelativePointToCenter(out px, out py);
            L2DManager_OnTouchesEnded(px, py);

            PosterGirl posterGirl = GetCurGirl();
            if (posterGirl != null)
            {
                posterGirl.Menu.Disable();
                curChildMenu?.Disable();
                curChildMenu = null;

                var tabEvent = posterGirl.GetTabEvent(GetCursorPoint());
                if (tabEvent != null)
                {
                    InvokeEvent(tabEvent, true);

                    Animation touchAnim = new Animation(animationManager, touchAnimType)
                    {
                        location = Cursor.Location
                    };
                }
            }

        }
        public void On_RightClick(object sender, EventArgs e)
        {
            PosterGirl posterGirl = GetCurGirl();
            if (posterGirl != null)
            {
                XNAContextMenu contextMenu = posterGirl.Menu;
                ShowMenu(contextMenu);
            }
        }

        private void ShowMenu(XNAContextMenu contextMenu)
        {
            contextMenu.Open(new Point(Cursor.Location.X - contextMenu.Parent.GetWindowPoint().X, Cursor.Location.Y - contextMenu.Parent.GetWindowPoint().Y));
        }

        public static void ContextMenu_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            var menu = (XNAContextMenu)sender;
            var item = (PosterManagerDropDownItem)menu.Items[e.ItemIndex];

            InvokeEvent(item, false);

            instance.curChildMenu?.Disable();
            instance.curChildMenu = null;
        }

        static void InvokeEvent(PosterManagerDropDownItem item, bool tab)
        {
            var menu = item.NextMenu;
            if (menu != null)
            {
                if (tab)
                {
                    InvokeEvent((PosterManagerDropDownItem)menu.Items[instance.random.Next(0, menu.Items.Count)], true);
                }
                return;
            }

            int girlIndex = instance.posterGirls.FindIndex(girl => girl.Name == item.Name);
            if (girlIndex >= 0)
            {
                instance.ChangeGirl(girlIndex);
                return;
            }

            if (item.StartRandomMotion != string.Empty)
            {
                IntPtr groupBuf = Marshal.StringToHGlobalAnsi(item.StartRandomMotion);
                L2DManager_StartRandomMotion(groupBuf, 2);
                Marshal.FreeHGlobal(groupBuf);
            }

            if (item.VoiceInstances.Count > 0)
            {
                var inst = item.VoiceInstances[instance.random.Next(0, item.VoiceInstances.Count)];
                inst.Volume = PosterManager.instance.voiceVolume;
                inst.Play();
            }

            if (item.SayString != string.Empty)
            {
                instance.bubble.SetText(item.SayString, item.SayStringColor);
                instance.bubble.SetDuration(item.SayStringDuration);
                instance.bubble.Enable();
            }

        }
    }


    class PosterGirl
    {
        public int OutTime { get; set; }
        public string Name { get; set; }
        public string ModelName { get; set; }
        public Rectangle GirlRectangle { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public Point Center { get; set; }
        public XNAContextMenu Menu { get; set; }

        List<PosterManagerDropDownItem> events = new List<PosterManagerDropDownItem>();

        public PosterGirl(WindowManager windowManager, IniFile iniFile, string name)
        {
            Name = name;
            ModelName = iniFile.GetStringValue(name, "ModelName", string.Empty);
            OutTime = iniFile.GetIntValue(name, "OutTime", 120);
            GirlRectangle = PosterManager.IniGetRectangleValue(iniFile, name, "GirlRectangle");


            string[] strs = iniFile.GetStringValue(name, "RenderScale", "1.0, 1.0").Split(',');
            ScaleX = (float)Convert.ToDouble(strs[0]);
            ScaleY = (float)Convert.ToDouble(strs[1]);

            strs = iniFile.GetStringValue(name, "Center", "0, 0").Split(',');
            Center = new Point(Convert.ToInt32(strs[0]), Convert.ToInt32(strs[1]));

            if (Center == Point.Zero)
            {
                Center = new Point(GirlRectangle.Width / 2, GirlRectangle.Height / 2);
            }

            Menu = new XNAContextMenu(windowManager)
            {
                Name = name + "Menu",
                Enabled = false,
                Visible = false,
                ClientRectangle = new Rectangle(0, 0, 150, 2)
            };

            string str = iniFile.GetStringValue(name, "Group", string.Empty);
            if (str != string.Empty)
            {
                string[] spilt = str.Replace(" ", "").Split(',');
                foreach (var itemName in spilt)
                {
                    Menu.AddItem(new PosterManagerDropDownItem(windowManager, iniFile, itemName));
                }
            }

            str = iniFile.GetStringValue(name, "Events", string.Empty);
            if (str != string.Empty)
            {
                string[] spilt = str.Replace(" ", "").Split(',');
                foreach (var itemName in spilt)
                {
                    events.Add(new PosterManagerDropDownItem(windowManager, iniFile, itemName));
                }
            }
        }

        public void Appear()
        {
            IntPtr groupBuf = Marshal.StringToHGlobalAnsi("In");
            PosterManager.L2DManager_StartRandomMotion(groupBuf, 2);
            Marshal.FreeHGlobal(groupBuf);
        }

        public void Disappear()
        {
            IntPtr groupBuf = Marshal.StringToHGlobalAnsi("Out");
            PosterManager.L2DManager_StartRandomMotion(groupBuf, 2);
            Marshal.FreeHGlobal(groupBuf);
        }

        public PosterManagerDropDownItem GetTabEvent(Point point)
        {
            return events.Find(item => item.TabInRange(point));
        }
    }

    class PosterManagerDropDownItem : XNAContextMenuItem
    {
        public XNAContextMenu NextMenu { get; set; }
        public string Name { get; set; }
        public string StartRandomMotion { get; set; }
        public Rectangle Area { get; set; }
        public int SayStringDuration { get; set; }
        public Color SayStringColor { get; set; }
        public string SayString { get; set; }
        public List<SoundEffect> Voices { get; set; } = new List<SoundEffect>();
        public List<SoundEffectInstance> VoiceInstances { get; set; } = new List<SoundEffectInstance>();

        public PosterManagerDropDownItem(WindowManager windowManager, IniFile iniFile, string name) : base()
        {
            Name = name;

            string str = iniFile.GetStringValue(name, "Color", "255,255,255");
            TextColor = AssetLoader.GetColorFromString(str);

            str = iniFile.GetStringValue(name, "Texture", string.Empty);
            if (str != string.Empty)
            {
                Texture = AssetLoader.LoadTexture(PosterManager.ResourcePath + str);
            }

            Text = iniFile.GetStringValue(name, "Description", string.Empty);

            str = iniFile.GetStringValue(name, "SayString.Color", "255,255,255");
            SayStringColor = AssetLoader.GetColorFromString(str);

            SayString = iniFile.GetStringValue(name, "SayString", string.Empty);
            SayString = SayString.Replace("@", Environment.NewLine);

            SayStringDuration = iniFile.GetIntValue(name, "SayString.Duration", 3000);

            str = iniFile.GetStringValue(name, "NextGroup", string.Empty);
            LoadNextGroup(windowManager, iniFile, str);

            StartRandomMotion = iniFile.GetStringValue(name, "StartRandomMotion", string.Empty);

            str = iniFile.GetStringValue(name, "Area", "0,0,0,0");
            string[] split = str.Split(',');
            Area = new Rectangle(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]),
                Convert.ToInt32(split[2]), Convert.ToInt32(split[3]));




            str = iniFile.GetStringValue(name, "Voices", string.Empty);
            if (str != string.Empty)
            {
                split = str.Replace(" ", "").Split(',');
                foreach (var itemName in split)
                {
                    SoundEffect soundEffect = AssetLoader.LoadSound(PosterManager.ResourcePath + itemName);
                    Voices.Add(soundEffect);
                    VoiceInstances.Add(soundEffect.CreateInstance());
                }
            }
        }

        ~PosterManagerDropDownItem()
        {
            Voices.ForEach(voice => voice.Dispose());
        }

        public void LoadNextGroup(WindowManager windowManager, IniFile iniFile, string group)
        {
            if (group != string.Empty)
            {
                NextMenu = new XNAContextMenu(windowManager)
                {
                    Name = this.Name + "Group",
                    Enabled = false,
                    Visible = false,
                    ClientRectangle = new Rectangle(0, 0, 150, 2)
                };
                string[] spilt = group.Replace(" ", "").Split(',');
                foreach (var itemName in spilt)
                {
                    NextMenu.AddItem(new PosterManagerDropDownItem(windowManager, iniFile, itemName));
                }
                NextMenu.OptionSelected += PosterManager.ContextMenu_OptionSelected;
            }
        }

        public bool TabInRange(Point point)
        {
            return Area.Contains(point);
        }
    }
    public class SpeakingBubble
    {
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
        public SpeakingBubble(WindowManager windowManager, IniFile iniFile, string section)
        {

            panel = new XNAPanel(windowManager)
            {
                Name = section
            };
            label = new XNALabel(windowManager)
            {
                Name = section
            };

            PosterManager.GetInstance(windowManager).AddChild(panel);
            PosterManager.GetInstance(windowManager).AddChild(label);

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
}