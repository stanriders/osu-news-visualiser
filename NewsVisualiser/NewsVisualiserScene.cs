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
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.UI;
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
            var modContainerSpacing = 150;
            var modContainerSpacingY = 10;
            var headingFont = OsuFont.GetFont(size: 34f, weight: FontWeight.Bold);

            return new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(25),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Difficulty Reduction",
                                    Font = headingFont,
                                },
                                new Box
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 6 },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 3,
                                    Colour = colours.ForModType(ModType.DifficultyReduction)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(modContainerSpacing, modContainerSpacingY),
                            Children = new Drawable[]
                            {
                                getModContainer(new OsuModEasy(), 0.5, 0.8, true),
                                getModContainer(new OsuModHalfTime(), 0.3, 0.55, true, 0.2, 0.83, 0.1, 0.5),
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Difficulty Increase",
                                    Font = headingFont,
                                },
                                new Box
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 6 },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 3,
                                    Colour = colours.ForModType(ModType.DifficultyIncrease)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(modContainerSpacing, modContainerSpacingY),
                            Children = new Drawable[]
                            {
                                getModContainer(new OsuModHardRock(), 1.06, 1.09),
                                getModContainer(new OsuModDoubleTime(), 1.1, 1.23, true, 1.0, 1.45, 1.0, 1.2),
                                getModContainer(new OsuModHidden(), 1.06, 1.04, true),
                                getModContainer(new OsuModTraceable(), 1.0, 1.02, true),
                                getModContainer(new OsuModFlashlight(), 1.12, 1.2, true),
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Automation",
                                    Font = headingFont,
                                },
                                new Box
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 6 },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 3,
                                    Colour = colours.ForModType(ModType.Automation)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(modContainerSpacing, modContainerSpacingY),
                            Children = new Drawable[]
                            {
                                getModContainer(new OsuModSpunOut(), 0.9, 0.95),
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Conversion",
                                    Font = headingFont,
                                },
                                new Box
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 6 },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 3,
                                    Colour = colours.ForModType(ModType.Conversion)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(modContainerSpacing, modContainerSpacingY),
                            Children = new Drawable[]
                            {
                                getModContainer(new OsuModTargetPractice(), 0.1, 0.01),
                                getModContainer(new OsuModClassic(), 0.96, 0.985, true),
                                getModContainer(new OsuModDifficultyAdjust(), 0.5, 1.0, true, 0.1, 1.0),
                                getModContainer(new OsuModRandom(), 1.0, 0.7),
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Fun",
                                    Font = headingFont,
                                },
                                new Box
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 6 },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 3,
                                    Colour = colours.ForModType(ModType.Fun)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(modContainerSpacing, modContainerSpacingY),
                            Children = new Drawable[]
                            {
                                getModContainer(new OsuModWiggle(), 1.0, 1.0, true),
                                getModContainer(new OsuModGrow(), 1.0, 1.0, true),
                                getModContainer(new OsuModDeflate(), 1.0, 1.0, true),
                                getModContainer(new ModWindUp(), 0.5, null, true),
                                getModContainer(new ModWindDown(), 0.5, null, true),
                                getModContainer(new OsuModApproachDifferent(), 1.0, 0.7),
                                getModContainer(new OsuModMagnetised(), 0.5, 0.4, true, 0.1, 0.7),
                                getModContainer(new OsuModRepel(), 1.0, 1.0, true),
                                getModContainer(new ModAdaptiveSpeed(), 0.5, 0.1),
                                getModContainer(new OsuModFreezeFrame(), 1.0, 1.0, true),
                                getModContainer(new OsuModSynesthesia(), 0.8, 0.99),
                                getModContainer(new OsuModDepth(), 1.0, 1.0, true)
                            }
                        },
                        new OsuSpriteText
                        {
                            Text = "* mod multiplier depends on other factors such as mod combinations or mod settings",
                            Font = OsuFont.GetFont(size: 28f, weight: FontWeight.SemiBold),
                        },
                    }
                }
            };
        }

        private FillFlowContainer getModContainer(IMod mod, double before, double? after, bool isntActuallyTrue = false,
                                                  double? lowRange = null, double? highRange = null,
                                                  double? lowBeforeRange = null, double? highBeforeRange = null)
        {
            double comparisonResult = after != null ? after.Value - before : 0.0;
            Colour4 comparisonColour;
            IconUsage icon;

            if (comparisonResult < 0)
            {
                comparisonColour = colours.Red1;
                icon = FontAwesome.Solid.ArrowDown;
            }
            else if (comparisonResult > 0)
            {
                comparisonColour = colours.Lime1;
                icon = FontAwesome.Solid.ArrowUp;
            }
            else
            {
                comparisonColour = colours.GrayD.Opacity(0);
                icon = FontAwesome.Solid.Minus;
            }

            var font = OsuFont.GetFont(size: 30f, weight: FontWeight.SemiBold);

            var hasRange = lowRange != null && highRange != null;
            var hasBeforeRange = lowBeforeRange != null && highBeforeRange != null;

            var range = !hasRange
                ? Empty()
                : new OsuSpriteText
                {
                    Font = OsuFont.GetFont(size: 28f, weight: FontWeight.Regular),
                    Text = $"{lowRange:0.0##}x - {highRange:0.0##}x",
                    UseFullGlyphHeight = true
                };

            var beforeRange = !hasBeforeRange
                ? Empty()
                : new OsuSpriteText
                {
                    Font = OsuFont.GetFont(size: 28f, weight: FontWeight.Regular),
                    Text = $"{lowBeforeRange:0.0##}x - {highBeforeRange:0.0##}x",
                    UseFullGlyphHeight = true
                };

            var spacing = hasRange ? new Vector2(10) : new Vector2(15);

            return new FillFlowContainer
            {
                Direction = FillDirection.Horizontal,
                AutoSizeAxes = Axes.Y,
                Width = 160,
                Margin = new MarginPadding
                {
                    Left = 5
                },
                Spacing = new Vector2(15),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding()
                        {
                            Top = 5
                        },
                        Children = new Drawable[]
                        {
                            new ModIcon(mod)
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Scale = new Vector2(1.0f),
                            },
                            new ModSwitchTiny(mod, true)
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Scale = new Vector2(1.0f),
                                Active = { Value = true }
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = spacing,
                        Margin = new MarginPadding()
                        {
                            Bottom = hasRange && hasBeforeRange ? 10 : 18
                        },
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Font = font,
                                        Text = $"Before: {before:0.0##}x",
                                        UseFullGlyphHeight = true
                                    },
                                    beforeRange
                                }
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8),
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Font = font,
                                                Text = $"After: {(after != null ? $"{after:0.0##}x" : "variable")}{(isntActuallyTrue ? "*" : "")}",
                                                UseFullGlyphHeight = true,
                                            },
                                            range
                                        }
                                    },
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(20),
                                        Colour = comparisonColour,
                                        Icon = icon,
                                        Alpha = after == null ? 0 : 1
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
