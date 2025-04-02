using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;

using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader,
            Random random
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
            this.random = random;
        }

        private static readonly object locker = new object();

        private MapLoader mapLoader;

        private Random random;

        private PrivateMessagingPanel privateMessagingPanel;

        private bool visibleSpriteCursor;

        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        private List<string> randomTextures;

#if !GL
        private VideoPlayer videoPlayer;
        private bool videoStopped;
        private bool renderAfterVideoStopped;
#endif
        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";
            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            SelectLoadingScreens();

            base.Initialize();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
            {
                updaterInitTask = new Task(InitUpdater);
                updaterInitTask.Start();
            }

            mapLoadTask = mapLoader.LoadMapsAsync();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }

            LoadLogo();
        }

        protected override void GetINIAttributes(IniFile iniFile)
        {
            base.GetINIAttributes(iniFile);

            randomTextures = iniFile.GetStringListValue(Name, "RandomBackgroundTextures", string.Empty).ToList();

            if (randomTextures.Count == 0)
                return;

            BackgroundTexture = AssetLoader.LoadTexture(randomTextures[random.Next(randomTextures.Count)]);
        }

        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? 
                "N/A" : Updater.GameVersion;

            MainMenu mainMenu = serviceProvider.GetService<MainMenu>();

            WindowManager.AddAndInitializeControl(mainMenu);
            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;

#if !GL
            videoPlayer?.Dispose();
            videoPlayer = null;
#endif
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

#if !GL
            if (renderAfterVideoStopped)
            {
                videoPlayer.Stop();
                videoPlayer?.Dispose();
                videoPlayer = null;
            }
            else
            {
                if (videoPlayer?.State == MediaState.Playing)
                {
                    if (Keyboard.IsKeyHeldDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                    {
                        videoStopped = true;
                    }
                }
                else if(videoPlayer?.State == MediaState.Stopped)
                {
                    videoStopped = true;
                }
                return;
            }
#endif

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

#if !GL
            if (videoPlayer?.State == MediaState.Playing && !videoStopped)
            {
                Renderer.DrawTexture(videoPlayer.GetTexture(), ClientRectangle, Color.White);
            }
            if (videoStopped)
            {
                renderAfterVideoStopped = true;
            }
#endif
        }

        private void SelectLoadingScreens()
        {
            string dir = Path.Combine(ProgramConstants.GetBaseResourcePath(), "LoadingScreens");
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir);
                string file = Path.GetFileName(files[new System.Random().Next(files.Length)]);
                BackgroundTexture = AssetLoader.LoadTexture(Path.Combine(ProgramConstants.GetBaseResourcePath(), "LoadingScreens", file));
            }
            else
            {
                BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");
            } 
        }

        private void LoadLogo()
        {
#if !GL
            Video video = Game.Content.Load<Video>(Path.Combine(ProgramConstants.GetBaseResourcePath(), "WALogo"));
            videoPlayer = new VideoPlayer();
            videoPlayer.Play(video);
#endif
        }
    }
}
