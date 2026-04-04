// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using NewsVisualiser.Components;
using NewsVisualiser.Configuration;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using Notification = osu.Game.Overlays.Notifications.Notification;

namespace NewsVisualiser
{
    public partial class NewsVisualiserGame : OsuGameBase
    {
        private Bindable<WindowMode> windowMode = null!;
        private DependencyContainer dependencies = null!;

        // This overwrites OsuGameBase's SelectedMods to make sure it can't tweak mods when we don't want it to
        [Cached]
        [Cached(typeof(IBindable<IReadOnlyList<Mod>>))]
        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; } = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        protected override IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults() => new Dictionary<FrameworkSetting, object>
        {
            { FrameworkSetting.VolumeUniversal, 0.0d },
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            var apiConfig = new SettingsManager(Storage);
            dependencies.CacheAs(apiConfig);
            dependencies.CacheAs(new APIManager(apiConfig));

            Ruleset.Value = new OsuRuleset().RulesetInfo;

            var dialogOverlay = new DialogOverlay();
            dependencies.CacheAs(dialogOverlay);

            var notificationDisplay = new NotificationDisplay();
            dependencies.CacheAs(notificationDisplay);

            var notificationOverlay = new EmptyNotificationOverlay();
            dependencies.CacheAs<INotificationOverlay>(notificationOverlay);

            var screenshotManager = new ScreenshotManager();
            dependencies.CacheAs(screenshotManager);

            AddRange(new Drawable[]
            {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new NewsVisualiserScene()
                    }
                },
                dialogOverlay,
                notificationDisplay,
                screenshotManager
            });
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            host.Window.CursorState |= CursorState.Hidden;

            foreach (var handler in host.AvailableInputHandlers)
            {
                if (handler is MouseHandler mouseHandler && mouseHandler.UseRelativeMode.Value)
                {
                    mouseHandler.UseRelativeMode.Value = false;
                }

                if (handler is OpenTabletDriverHandler tabletInputHandler && tabletInputHandler.IsActive)
                {
                    tabletInputHandler.Enabled.Value = false;
                }
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var size = frameworkConfig.GetBindable<Size>(FrameworkSetting.WindowedSize);
            size.Value = new Size(1000, 1000);

            windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            windowMode.Value = WindowMode.Windowed;

            var y = frameworkConfig.GetBindable<double>(FrameworkSetting.WindowedPositionY);
            y.Value = 0;

            var hideCursor = LocalConfig.GetBindable<bool>(OsuSetting.ScreenshotCaptureMenuCursor);
            hideCursor.Value = false;

            var format = LocalConfig.GetBindable<ScreenshotFormat>(OsuSetting.ScreenshotFormat);
            format.Value = ScreenshotFormat.Png;

            LocalConfig.Save();
        }

        public partial class EmptyNotificationOverlay : INotificationOverlay
        {
            public void Post(Notification notification)
            {
            }

            public void Hide()
            {
            }

            public IBindable<int> UnreadCount => new Bindable<int>(0);
            public IEnumerable<Notification> AllNotifications => [];
        }
    }
}
