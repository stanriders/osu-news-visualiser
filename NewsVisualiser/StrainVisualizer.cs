// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NewsVisualiser.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Graphics;

namespace NewsVisualiser
{
    public partial class StrainVisualizer : Container
    {
        private readonly bool withDistance;
        private readonly List<Bindable<bool>> graphToggles = new List<Bindable<bool>>();

        public readonly Bindable<int> TimeUntilFirstStrain = new Bindable<int>();

        private Container graphsContainer = null!;
        private FillFlowContainer legendContainer = null!;

        private ColourInfo[] skillColours = [];

        [Resolved]
        private OverlayColourProvider? colourProvider { get; set; }

        private DifficultyCalculator? difficultyCalculator { get; set; } = null!;

        private const int strain_length = 400;

        public StrainVisualizer(IWorkingBeatmap beatmap, bool withDistance)
        {
            this.withDistance = withDistance;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var ruleset = new OsuRuleset();
            difficultyCalculator = new ExtendedOsuDifficultyCalculator(ruleset.RulesetInfo, beatmap);
            difficultyCalculator.Calculate();
        }

        private float graphAlpha;

        private void updateGraphs()
        {
            graphsContainer.Clear();

            if (difficultyCalculator is not IExtendedDifficultyCalculator extendedDifficultyCalculator)
                return;

            var skills = extendedDifficultyCalculator.GetSkills();
            skills = [skills.First(x => x is Aim), skills.OfType<Speed>().First(x => x.WithDistance == withDistance)];

            if (skills.Length == 0)
            {
                legendContainer.Clear();
                graphToggles.Clear();
                return;
            }

            graphAlpha = Math.Min(1.5f / skills.Length, 0.9f);
            var strainLists = getStrainLists(skills);
            addStrainBars(skills, strainLists);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            var redColorProvider = new OverlayColourProvider(OverlayColourScheme.Red);
            var blueColorProvider = new OverlayColourProvider(OverlayColourScheme.Blue);
            skillColours = new ColourInfo[]
            {
                blueColorProvider.Colour3,
                redColorProvider.Colour1,
                blueColorProvider.Colour2,
            };

            Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 16,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Padding = new MarginPadding(10),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(1),
                        Children = new Drawable[]
                        {
                            graphsContainer = new Container
                            {
                                Height = 150,
                                RelativeSizeAxes = Axes.X,
                                //Alpha = 0.8f,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Colour = Color4.White,
                                Alpha = 0.5f,
                                Height = 3,
                                EdgeSmoothness = Vector2.One,
                            },
                        }
                    }
                }
            });

            updateGraphs();
        }

        private void addStrainBars(Skill[] skills, List<Strain[]> strainLists)
        {
            double strainMaxValue = strainLists.SelectMany(x => x).MaxBy(x => x.Difficulty)!.Difficulty;

            for (int i = 0; i < skills.Length; i++)
            {
                var strainGraph = new StrainBarGraph
                {
                    RelativeSizeAxes = Axes.Both,
                    MaxValue = (float)strainMaxValue
                };
                strainGraph.CreateBars(strainLists[i]);

                graphsContainer.AddRange(new Drawable[]
                {
                    new Container()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 1f,
                        Colour = skillColours[i % skillColours.Length],
                        Child = strainGraph
                    }
                });
            }
        }

        private List<Strain[]> getStrainLists(Skill[] skills)
        {
            var strainLists = new List<Strain[]>();

            foreach (var skill in skills)
            {
                switch (skill)
                {
                    case Aim aim:
                        if (!aim.IncludeSliders)
                            strainLists.Add(getZeroStrainList(aim));
                        else
                            strainLists.Add(getStrainList(skill));
                        break;

                    /*case Reading reading:
                        strainLists.Add(getZeroStrainList(reading));
                        break;*/
                    
                    case StrainSkill strainSkill:
                        strainLists.Add(getStrainSkillStrainList(strainSkill));
                        break;

                    default:
                        strainLists.Add(getStrainList(skill));
                        break;
                }
            }

            return strainLists;
        }

        private Strain[] getStrainSkillStrainList(StrainSkill strainSkill)
        {
            double[] strains = strainSkill.GetCurrentStrainPeaks().ToArray();

            var skillStrainList = new List<Strain>();

            for (int i = 0; i < strains.Length; i++)
            {
                double strain = strains[i];
                skillStrainList.Add(new Strain
                {
                    Difficulty = strain,
                    StartTime = strain_length * i, // todo: use actual strain length
                    EndTime = (strain_length * i) + strain_length
                });
            }

            return skillStrainList.ToArray();
        }

        private Strain[] getStrainList(Skill skill)
        {
            var difficultyObjects = (difficultyCalculator as IExtendedDifficultyCalculator)!.GetDifficultyHitObjects();

            var difficulties = skill.GetObjectDifficulties();

            var skillStrainList = new List<Strain>();

            for (int i = 0; i < difficulties.Count - 1; i++)
            {
                double strain = difficulties[i];
                var difficultyObject = difficultyObjects[i];
                var nextDifficultyObject = i < difficulties.Count - 1 ? difficultyObjects[i + 1] : null;

                double startTime = difficultyObject.StartTime;
                double endTime = difficultyObject.EndTime;

                if (nextDifficultyObject != null)
                {
                    // cap length to object_length + strain_length to make map breaks display 0 difficulty instead of the last-object-before-break difficulty
                    endTime = Math.Min(endTime + strain_length, nextDifficultyObject.StartTime);
                }

                skillStrainList.Add(new Strain
                {
                    Difficulty = strain,
                    StartTime = startTime,
                    EndTime = endTime
                });

                // add blank bars between objects to make the graph consistent timescale-wise
                if (nextDifficultyObject != null && nextDifficultyObject.StartTime - endTime > 0)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = nextDifficultyObject.StartTime
                    });
                }

                // add blank strain_length bar in the end to make the object difficulties graph consistent with strain-based graphs
                if (nextDifficultyObject == null)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = endTime + strain_length
                    });
                }
            }

            return skillStrainList.ToArray();
        }

        private Strain[] getZeroStrainList(Skill skill)
        {
            var difficultyObjects = (difficultyCalculator as IExtendedDifficultyCalculator)!.GetDifficultyHitObjects();

            var difficulties = skill.GetObjectDifficulties();

            var skillStrainList = new List<Strain>();

            for (int i = 0; i < difficulties.Count - 1; i++)
            {
                double strain = difficulties[i];
                var difficultyObject = difficultyObjects[i];
                var nextDifficultyObject = i < difficulties.Count - 1 ? difficultyObjects[i + 1] : null;

                double startTime = difficultyObject.StartTime;
                double endTime = difficultyObject.EndTime;

                if (nextDifficultyObject != null)
                {
                    // cap length to object_length + strain_length to make map breaks display 0 difficulty instead of the last-object-before-break difficulty
                    endTime = Math.Min(endTime + strain_length, nextDifficultyObject.StartTime);
                }

                skillStrainList.Add(new Strain
                {
                    Difficulty = 0,
                    StartTime = startTime,
                    EndTime = endTime
                });

                // add blank bars between objects to make the graph consistent timescale-wise
                if (nextDifficultyObject != null && nextDifficultyObject.StartTime - endTime > 0)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = nextDifficultyObject.StartTime
                    });
                }

                // add blank strain_length bar in the end to make the object difficulties graph consistent with strain-based graphs
                if (nextDifficultyObject == null)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = endTime + strain_length
                    });
                }
            }

            return skillStrainList.ToArray();
        }
    }

    public partial class StrainBarGraph : FillFlowContainer<Bar>
    {
        /// <summary>
        /// Manually sets the max value, if null <see cref="Enumerable.Max(IEnumerable{float})"/> is instead used
        /// </summary>
        public float? MaxValue { get; set; }

        public void CreateBars(Strain[] values)
        {
            Clear();

            double maxLength = MaxValue ?? values.MaxBy(x => x.Difficulty)!.Difficulty;
            double totalWidth = values.Sum(x => x.Length);

            foreach (Strain val in values)
            {
                double length = 0;
                if (maxLength != 0)
                    length = val.Difficulty / maxLength;

                float size = (float)(val.Length / totalWidth);

                Add(new Bar
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(size, 1),
                    Length = (float)length,
                    Direction = BarDirection.BottomToTop,
                    AccentColour = Colour4.White.Opacity(0.8f)
                });
            }
        }
    }

    public class Strain
    {
        public double Difficulty { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Length => EndTime - StartTime;
    }
}
