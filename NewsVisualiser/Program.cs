// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework;
using osu.Framework.Platform;

namespace NewsVisualiser
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            using DesktopGameHost host = Host.GetSuitableDesktopHost("NewsVisualiser", new HostOptions
            {
                PortableInstallation = true,
                BypassCompositor = false,
                FriendlyGameName = "News Visualiser"
            });

            using var game = new NewsVisualiserGame();

            host.Run(game);
        }
    }
}
