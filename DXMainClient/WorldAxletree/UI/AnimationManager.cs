using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.WorldAxletree.UI
{
    public class AnimationManager : XNAControl
    {
        public AnimationManager(WindowManager windowManager) : base(windowManager)
        {
        }
        public List<Animation> AnimList { get; set; } = new List<Animation>();
        public bool Paused { get; set; } = false;

        public override void Draw(GameTime gameTime)
        {
            foreach (var anim in AnimList)
            {
                anim.Draw();
            }
        }
        public override void Update(GameTime gameTime)
        {
            if (Paused == false)
            {
                int count = AnimList.Count;
                for (int idx = 0; idx < count; idx++)
                {
                    Animation anim = AnimList[idx];
                    anim.Update(gameTime);
                    if (count != AnimList.Count)
                    {
                        count = AnimList.Count;
                        idx--;
                    }
                }
            }
        }

        public void LoadAnimations(IniFile iniFile)
        {
            var keys = iniFile.GetSectionKeys("Animations");
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    string value = iniFile.GetStringValue("Animations", key, string.Empty);
                    if (value != string.Empty)
                    {
                        var type = new AnimationType(value, iniFile);
                        _ = new Animation(this, type);
                    }
                }
            }
        }
    }

    public enum AnimationDisplayType
    {
        Sheet, Files
    }

    public class AnimationType
    {
        public Point sheetSize;
        public Point location;
        public int frameRate;
        public int looptimes;
        public Texture2D[] textures;
        public string name;
        public AnimationDisplayType displayType;
        public SoundEffect voice;
        public int frameBeforeStart;
        public AnimationType(string name, IniFile iniFile)
        {
            this.name = name;

            displayType = (AnimationDisplayType)Enum.Parse(typeof(AnimationDisplayType), iniFile.GetStringValue(name, "DisplayType", "Sheet"));

            string str = iniFile.GetStringValue(name, "Animation", string.Empty);
            switch (displayType)
            {
                case AnimationDisplayType.Files:
                    foreach (string searchPath in AssetLoader.AssetSearchPaths)
                    {
                        if (Directory.Exists(searchPath + str))
                        {
                            string[] fileNames = Directory.GetFiles(searchPath + str, "*.png");
                            textures = new Texture2D[fileNames.Length];
                            for (int i = 0; i < textures.Length; i++)
                            {
                                textures[i] = AssetLoader.LoadTexture(str + fileNames[i].Substring(fileNames[i].LastIndexOf('\\')));
                            }
                        }
                    }
                    break;
                case AnimationDisplayType.Sheet:
                    textures = new Texture2D[1];
                    textures[0] = AssetLoader.LoadTexture(str);
                    break;
            }

            str = iniFile.GetStringValue(name, "Voice", string.Empty);
            if (str != string.Empty)
            {
                voice = AssetLoader.LoadSound(str);
            }

            string[] sheetSizeStr = iniFile.GetStringValue(name, "SheetSize", "0,0").Split(',');
            sheetSize = new Point(int.Parse(sheetSizeStr[0]), int.Parse(sheetSizeStr[1]));

            string[] locationStr = iniFile.GetStringValue(name, "Location", "0,0").Split(',');
            location = new Point(int.Parse(locationStr[0]), int.Parse(locationStr[1]));

            frameRate = iniFile.GetIntValue(name, "FrameRate", 0);

            looptimes = iniFile.GetIntValue(name, "LoopTimes", 0);

            frameBeforeStart = iniFile.GetIntValue(name, "FrameBeforeStart", 0);
        }
    }
    public class Animation
    {
        private AnimationType type;
        private SoundEffectInstance voiceInstance;
        private int msecond = -1;
        private int frameBeforeStart;
        public Point currentFrame = new Point(0, 0);
        public Point location;
        public int playtimes = 1;
        AnimationManager manager;

        public Animation(AnimationManager manager, AnimationType type)
        {
            this.type = type;
            this.manager = manager;
            playtimes = type.looptimes + 1;
            location = type.location;
            manager.AnimList.Add(this);
            voiceInstance = type.voice?.CreateInstance();
            frameBeforeStart = type.frameBeforeStart;
        }

        Texture2D GetCurTexture()
        {
            if (type.displayType == AnimationDisplayType.Files)
            {
                return type.textures[currentFrame.X];
            }
            return type.textures[0];
        }
        Point GetCurFrameSize()
        {
            Point textureSize = new Point(GetCurTexture().Width, GetCurTexture().Height);
            if (type.displayType == AnimationDisplayType.Files)
            {
                return textureSize;
            }
            return new Point(textureSize.X / type.sheetSize.X, textureSize.Y / type.sheetSize.Y);
        }
        Rectangle GetCurRectangle()
        {
            Point frameSize = GetCurFrameSize();
            if (type.displayType == AnimationDisplayType.Files)
            {
                return new Rectangle(0, 0, frameSize.X, frameSize.Y);
            }
            return new Rectangle(currentFrame.X * frameSize.X, currentFrame.Y * frameSize.Y, frameSize.X, frameSize.Y);
        }

        public void Draw()
        {
            if (frameBeforeStart == 0)
            {
                Point frameSize = GetCurFrameSize();
                Rectangle rectangle = new Rectangle(location.X - frameSize.X / 2, location.Y - frameSize.Y / 2, frameSize.X, frameSize.Y);
                Renderer.DrawTexture(GetCurTexture(),
                    GetCurRectangle(),
                    rectangle,
                    Color.White);
            }
        }
        public void Draw(Point point)
        {
            if (frameBeforeStart == 0)
            {
                Point frameSize = GetCurFrameSize();
                Rectangle rectangle = new Rectangle(point.X - frameSize.X / 2, point.Y - frameSize.Y / 2, frameSize.X, frameSize.Y);
                Renderer.DrawTexture(GetCurTexture(),
                    GetCurRectangle(),
                    rectangle,
                    Color.White);
            }
        }
        public void Draw(Rectangle rectangle)
        {
            if (frameBeforeStart == 0)
            {
                Renderer.DrawTexture(GetCurTexture(),
            GetCurRectangle(),
            rectangle,
            Color.White);
            }
        }
        public void Update(GameTime gameTime)
        {
            if (frameBeforeStart > 0)
            {
                frameBeforeStart--;
            }
            if (frameBeforeStart > 0)
            {
                return;
            }

            if (msecond < 0)
            {
                msecond = (int)gameTime.TotalGameTime.TotalMilliseconds;
                voiceInstance?.Play();
                return;
            }
            else if ((int)gameTime.TotalGameTime.TotalMilliseconds - msecond > type.frameRate)
            {
                msecond = (int)gameTime.TotalGameTime.TotalMilliseconds;
            }
            else return;


            if (type.displayType == AnimationDisplayType.Files)
            {
                if (++currentFrame.X >= type.textures.Length)
                {
                    currentFrame.X = 0;
                    playtimes--;
                    voiceInstance?.Play();
                }
            }
            else
            {
                if (++currentFrame.X >= type.sheetSize.X)
                {
                    currentFrame.X = 0;
                    if (++currentFrame.Y >= type.sheetSize.Y)
                    {
                        currentFrame.Y = 0;
                        playtimes--;
                        voiceInstance?.Play();
                    }
                }
            }

            if (playtimes == 0)
            {
                manager.AnimList.Remove(this);
            }
        }
    }

}
