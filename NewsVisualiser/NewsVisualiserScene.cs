// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
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
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK;
using SixLabors.ImageSharp;

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

        private ExtendedBufferedContainer contentContainer = null!;

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

        private Storage storage = null!;

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
            this.storage = storage.GetStorageForDirectory(@"screenshots");

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
                            new Container
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
                                            contentContainer.RenderToImage = true;
                                            await Task.Delay(100).ConfigureAwait(false);

                                            using var screenshotStream = getScreenshotStream();
                                            await contentContainer.Image.SaveAsPngAsync(screenshotStream).ConfigureAwait(false);
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
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = contentContainer = new ExtendedBufferedContainer
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

        private Stream? getScreenshotStream()
        {
            DateTime dt = DateTime.Now;

            string withoutIndex = $"news_{dt:yyyy-MM-dd_HH-mm-ss}.png";
            if (!storage.Exists(withoutIndex))
                return storage.GetStream(withoutIndex, FileAccess.Write, FileMode.Create);

            for (ulong i = 1; i < ulong.MaxValue; i++)
            {
                string indexedName = $"news_{dt:yyyy-MM-dd_HH-mm-ss}-{i}.png";
                if (!storage.Exists(indexedName))
                    return storage.GetStream(indexedName, FileAccess.Write, FileMode.Create);
            }

            return null;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }

        private Drawable[] createContent()
        {
            var beatmap = ProcessorWorkingBeatmap.FromFileOrId("5195256");

            var redColorProvider = new OverlayColourProvider(OverlayColourScheme.Red);
            var blueColorProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

            return new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Children = new[]
                            {
                                new OsuSpriteText()
                                {
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    Text = $"Sidetracked Day [{beatmap.BeatmapInfo.DifficultyName}]",
                                    Font = OsuFont.Default.With(size: 24, weight: FontWeight.SemiBold),
                                },
                                new OsuSpriteText()
                                {
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    Text = "by wuk",
                                    Font = OsuFont.Default.With(size: 22, weight: FontWeight.Regular),
                                },
                            }
                        },
                        new OsuSpriteText
                        {
                            Text = "mapset by sytho",
                            Font = OsuFont.Default.With(size: 20),
                        },
                    }
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(12),
                    Children = new Drawable[]
                    {
                        new Container()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    AutoSizeAxes = Axes.Both,
                                    Masking = true,
                                    CornerRadius = 6,
                                    Children = new Drawable[]
                                    {
                                        new Box()
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.FromHex("3d3946aa")
                                        },
                                        new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Padding = new MarginPadding() { Horizontal = 14, Vertical = 10 },
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(8),
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer()
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(12),
                                                    Children = new Drawable[]
                                                    {
                                                        new Circle()
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Width = 16,
                                                            Height = 16,
                                                            Colour = blueColorProvider.Colour3
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                                            Text = "Aim"
                                                        }
                                                    }
                                                },
                                                new FillFlowContainer()
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(12),
                                                    Children = new Drawable[]
                                                    {
                                                        new Circle()
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Width = 16,
                                                            Height = 16,
                                                            Colour = redColorProvider.Colour1
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                                            Text = "Speed (before)"
                                                        }
                                                    }
                                                },
                                            }
                                        }
                                    },
                                },
                                new StrainVisualizer(beatmap, true)
                                {
                                    Margin = new MarginPadding() { Top = 120 }
                                },
                            }
                        },
                        new Container()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    AutoSizeAxes = Axes.Both,
                                    Masking = true,
                                    CornerRadius = 6,
                                    Children = new Drawable[]
                                    {
                                        new Box()
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.FromHex("3d3946aa")
                                        },
                                        new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Padding = new MarginPadding() { Horizontal = 14, Vertical = 10 },
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(8),
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer()
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(12),
                                                    Children = new Drawable[]
                                                    {
                                                        new Circle()
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Width = 16,
                                                            Height = 16,
                                                            Colour = blueColorProvider.Colour3
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                                            Text = "Aim"
                                                        }
                                                    }
                                                },
                                                new FillFlowContainer()
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(12),
                                                    Children = new Drawable[]
                                                    {
                                                        new Circle()
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Width = 16,
                                                            Height = 16,
                                                            Colour = redColorProvider.Colour1
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Origin = Anchor.CentreLeft,
                                                            Anchor = Anchor.CentreLeft,
                                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                                            Text = "Speed (after)"
                                                        }
                                                    }
                                                },
                                            }
                                        }
                                    },
                                },
                                new StrainVisualizer(beatmap, false)
                                {
                                    Margin = new MarginPadding() { Top = 120 }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
