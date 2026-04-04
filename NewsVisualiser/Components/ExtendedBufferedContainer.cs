// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NewsVisualiser.Components
{
    /// <summary>
    /// Extended buffered container that allows you to export its contents as image.
    /// </summary>
    public partial class ExtendedBufferedContainer : BufferedContainer
    {
        public Image<Rgba32>? Image { get; set; }

        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(new[] { RenderBufferFormat.D16 }, pixelSnapping: true, clipToRootNode: false);

        public bool RenderToImage { get; set; }

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private void onRendered(IFrameBuffer frameBuffer)
        {
            if (!RenderToImage)
                return;

            host.DrawThread.Scheduler.Add(() =>
            {
                var image = renderer.ExtractFrameBufferData(frameBuffer);

                Schedule(() =>
                {
                    Image = image;
                });
            });

            RenderToImage = false;
        }

        protected override DrawNode CreateDrawNode()
        {
            var screenshotNode = new DrawableScreenshotterDrawNode(this, sharedData, onRendered);
            screenshotNode.ApplyState();

            return screenshotNode;
        }

        public class DrawableScreenshotterDrawNode : BufferedDrawNode, ICompositeDrawNode
        {
            private readonly Action<IFrameBuffer> onRendered;

            public DrawableScreenshotterDrawNode(ExtendedBufferedContainer source, BufferedDrawNodeSharedData sharedData, Action<IFrameBuffer> onRendered)
                : base(source, new CompositeDrawableDrawNode(source), sharedData)
            {
                this.onRendered = onRendered;
            }

            private new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;

            protected override void DrawContents(IRenderer renderer)
            {
                base.DrawContents(renderer);
                onRendered(SharedData.MainBuffer);
            }
        }
    }
}
