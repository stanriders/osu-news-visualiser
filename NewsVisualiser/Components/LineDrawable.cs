// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace NewsVisualiser.Components
{
    /// <summary>
    /// Simple line with rounded ends
    /// </summary>
    public partial class LineDrawable : Container
    {
        public LineDrawable(Vector2 start, Vector2 end)
        {
            Origin = Anchor.CentreLeft;
            Masking = true;
            CornerRadius = 4;

            var line = new Line(start, end);
            Position = line.StartPoint;
            Width = line.Rho + 1;
            Height = 8;
            Rotation = float.RadiansToDegrees(line.Theta);

            Child = new Box
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                EdgeSmoothness = Vector2.One,
                Colour = Colour4.White
            };
        }
    }
}
