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
using osu.Framework.Timing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Skinning;
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
            var difficulty = new BeatmapDifficulty { ApproachRate = 8, CircleSize = 1 };

            var hitcircle = new HitCircle
            {
                ComboIndex = 2,
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(200, 100),
                StartTime = 100
            };
            hitcircle.ApplyDefaults(new ControlPointInfo(), difficulty);

            var slider = new Slider
            {
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(350, 240),
                ComboIndex = 2,
                IndexInCurrentCombo = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(new Vector2(0, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(-110, 0)),
                    new PathControlPoint(new Vector2(-200, 20))
                }),
                StartTime = 200
            };
            slider.ApplyDefaults(new ControlPointInfo(), difficulty);

            var secondSlider = new Slider
            {
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(400, 400),
                ComboIndex = 2,
                IndexInCurrentCombo = 2,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(new Vector2(0, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(-110, 0)),
                    new PathControlPoint(new Vector2(-200, 20))
                }),
                StartTime = 300
            };
            secondSlider.ApplyDefaults(new ControlPointInfo(), difficulty);

            var hitcircle2 = new HitCircle
            {
                ComboIndex = 3,
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(650, 100),
                StartTime = 100
            };
            hitcircle2.ApplyDefaults(new ControlPointInfo(), difficulty);

            var slider2 = new Slider
            {
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(800, 250),
                ComboIndex = 3,
                IndexInCurrentCombo = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(new Vector2(0, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(-110, 0)),
                    new PathControlPoint(new Vector2(-200, 20))
                }),
                StartTime = 200
            };
            slider2.ApplyDefaults(new ControlPointInfo(), difficulty);

            var secondSlider2 = new Slider
            {
                HitWindows = new OsuHitWindows(),
                Position = new Vector2(850, 400),
                ComboIndex = 3,
                IndexInCurrentCombo = 2,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(new Vector2(0, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(-110, 0)),
                    new PathControlPoint(new Vector2(-200, 20)),
                }),
                StartTime = 300
            };
            secondSlider2.ApplyDefaults(new ControlPointInfo(), difficulty);

            var manualClock = new ManualClock
            {
                IsRunning = false,
                CurrentTime = 0
            };

            return new Drawable[]
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
                            Padding = new MarginPadding() {Horizontal = 14, Vertical = 10},
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
                                            Colour = Colour4.FromHex("0000FF")
                                        },
                                        new OsuSpriteText
                                        {
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                            Text = "Old"
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
                                            Colour = Colour4.FromHex("ff0000")
                                        },
                                        new OsuSpriteText
                                        {
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Font = OsuFont.Inter.With(size: 24, fixedWidth: false),
                                            Text = "New"
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                new RulesetSkinProvidingContainer(new OsuRuleset(), new OsuBeatmap(), null)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 600,
                    Clock = new FramedClock(manualClock),
                    Children = new Drawable[]
                    {
                        new DrawableHitCircle(hitcircle),
                        new DrawableSlider(slider),
                        new DrawableSlider(secondSlider),
                        new DrawableHitCircle(hitcircle2),
                        new DrawableSlider(slider2),
                        new DrawableSlider(secondSlider2),

                        new LineDrawable(hitcircle.Position, slider.Position),
                        //new LineDrawable(slider.Position, slider.Position),
                        new LineDrawable(slider.Position, secondSlider.Position),
                        new LineDrawable(secondSlider.Position, secondSlider.EndPosition),

                        new LineDrawable(hitcircle2.Position, slider2.Position),
                        new LineDrawable(slider2.Position, slider2.EndPosition),
                        new LineDrawable(slider2.EndPosition, secondSlider2.Position),
                        new LineDrawable(secondSlider2.Position, secondSlider2.EndPosition),
                    }
                }
            };
        }
    }
}
