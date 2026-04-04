// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using NewsVisualiser.Components;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK;

namespace NewsVisualiser
{
    [Cached]
    public partial class NewsVisualiserScene : PopoverContainer
    {
        private ToolbarRulesetSelector rulesetSelector = null!;

        public const float CONTROL_AREA_HEIGHT = 45;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; } = null!;

        private Container contentContainer = null!;
        private Container controls = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; } = null!;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        public NewsVisualiserScene()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ScreenshotManager screenshotManager)
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            controls = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = CONTROL_AREA_HEIGHT,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        Colour = OsuColour.Gray(0.1f),
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new FormButton.Button
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Width = 200,
                                        Text = "Save screenshot",
                                        Action = async void () =>
                                        {
                                            controls.Alpha = 0;
                                            await Task.Delay(100).ConfigureAwait(false);
                                            await screenshotManager.TakeScreenshotAsync().ConfigureAwait(false);
                                            controls.Alpha = 1;
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.TopRight,
                                        Direction = FillDirection.Horizontal,
                                        RelativeSizeAxes = Axes.Y,
                                        AutoSizeAxes = Axes.X,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            rulesetSelector = new ToolbarRulesetSelector(),
                                            new SettingsButton()
                                        }
                                    },
                                },
                            }
                        },
                        new Drawable[]
                        {
                            new ScalingContainer(ScalingMode.Everything)
                            {
                                Depth = 1,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4Extensions.FromHex("24222a")
                                    },
                                    contentContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Padding = new MarginPadding(10)
                                    }
                                }
                            }
                        }
                    }
                }
            };

            if (RuntimeInfo.IsDesktop)
            {
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(() =>
                {
                    contentContainer.Clear(true);
                    contentContainer.AddRange(createContent());
                });
            }

            contentContainer.AddRange(createContent());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }

        private Drawable[] createContent()
        {
            return new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = "Content",
                }
            };
        }
    }
}
