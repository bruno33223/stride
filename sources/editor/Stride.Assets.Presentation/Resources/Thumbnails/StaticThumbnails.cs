// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace Stride.Assets.Presentation.Resources.Thumbnails;

/// <summary>
/// Static thumbnail resource provider for engine and editor assets.
/// </summary>
public static class StaticThumbnails
{
    private static readonly Assembly ThisAssembly = typeof(StaticThumbnails).Assembly;

    private static byte[] LoadResource(string resourceName)
    {
        var fullName = $"Stride.Assets.Presentation.Resources.Thumbnails.{resourceName}";
        using var stream = ThisAssembly.GetManifestResourceStream(fullName);
        if (stream == null)
            throw new InvalidOperationException($"Thumbnail resource not found: {fullName}");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static readonly Lazy<byte[]> lazyAnimationThumbnail = new(() => LoadResource("AnimationThumbnail.png"));
    private static readonly Lazy<byte[]> lazyGameSettingsThumbnail = new(() => LoadResource("GameSettingsThumbnail.png"));
    private static readonly Lazy<byte[]> lazyGraphicsCompositorThumbnail = new(() => LoadResource("GraphicsCompositorThumbnail.png"));
    private static readonly Lazy<byte[]> lazySceneThumbnail = new(() => LoadResource("SceneThumbnail.png"));
    private static readonly Lazy<byte[]> lazyScriptSourceFileThumbnail = new(() => LoadResource("ScriptSourceFileThumbnail.png"));
    private static readonly Lazy<byte[]> lazySkeletonThumbnail = new(() => LoadResource("SkeletonThumbnail.png"));
    private static readonly Lazy<byte[]> lazySkyboxThumbnail = new(() => LoadResource("SkyboxThumbnail.png"));
    private static readonly Lazy<byte[]> lazySoundThumbnail = new(() => LoadResource("SoundThumbnail.png"));
    private static readonly Lazy<byte[]> lazyUILibraryThumbnail = new(() => LoadResource("UILibraryThumbnail.png"));
    private static readonly Lazy<byte[]> lazyVideoThumbnail = new(() => LoadResource("VideoThumbnail.png"));

    public static byte[] AnimationThumbnail => lazyAnimationThumbnail.Value;

    public static byte[] GameSettingsThumbnail => lazyGameSettingsThumbnail.Value;

    public static byte[] GraphicsCompositorThumbnail => lazyGraphicsCompositorThumbnail.Value;

    public static byte[] SceneThumbnail => lazySceneThumbnail.Value;

    public static byte[] ScriptSourceFileThumbnail => lazyScriptSourceFileThumbnail.Value;

    public static byte[] SkeletonThumbnail => lazySkeletonThumbnail.Value;

    public static byte[] SkyboxThumbnail => lazySkyboxThumbnail.Value;

    public static byte[] SoundThumbnail => lazySoundThumbnail.Value;

    public static byte[] UILibraryThumbnail => lazyUILibraryThumbnail.Value;

    public static byte[] VideoThumbnail => lazyVideoThumbnail.Value;
}
