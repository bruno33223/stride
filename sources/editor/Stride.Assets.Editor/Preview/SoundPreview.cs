// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Assets.Media;
using Stride.Audio;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Media;

namespace Stride.Assets.Editor.Preview
{
    [AssetPreview<SoundAsset>]
    public class SoundPreview : BuildAssetPreview<SoundAsset>
    {
        private IDispatcherService dispatcher;
        private Sound sound;
        private SoundInstance instance;
        private TimeSpan startTime = TimeSpan.Zero;
        private bool isLooping;

        public Action<bool, bool, TimeSpan, TimeSpan> UpdateViewModelTime { get; set; }

        public bool IsPlaying { get; private set; }

        public bool IsLooping
        {
            get => instance?.IsLooping ?? isLooping;
            set { isLooping = value; if (instance != null) instance.IsLooping = value; }
        }

        private volatile bool loaded;

        public void ProvideDispatcher(IDispatcherService dispatcherService)
        {
            dispatcher = dispatcherService;
        }

        public void SetLoop(bool value) => IsLooping = value;

        protected bool VerifyAssetConsistency()
        {
            // Get absolute path of asset source on disk
            var assetDirectory = (string)AssetItem.Location.GetParent();
            var assetSource = UPath.Combine(assetDirectory, Asset.Source ?? "");

            // Check that the source file exist on the disk.
            return File.Exists(assetSource);
        }

        protected override async Task Initialize()
        {
            await base.Initialize();
            await Task.Run(() => UpdatePlaybackTime());
        }

        protected override async Task<bool> PrepareContent()
        {
            loaded = false;

            startTime = TimeSpan.Zero;

            if (sound != null)
            {
                instance?.Stop();
                instance?.Dispose();
                UnloadAsset(sound);
            }

            await base.PrepareContent();

            sound = LoadAsset<Sound>(AssetItem.Location);
            if (sound == null)
                return false;
            instance = sound.CreateInstance(null, true);
            instance.IsLooping = isLooping;
            instance.SetRange(new PlayRange(TimeSpan.Zero, TimeSpan.Zero));

            loaded = true;
            return true;
        }

        protected override void UnloadContent()
        {
            if (!loaded)
                return;

            UpdateViewModelTime?.Invoke(false, false, TimeSpan.Zero, TimeSpan.Zero);
            instance?.Stop();
            instance?.Dispose();
            instance = null;
            UnloadAsset(sound);

            loaded = false;
        }

        private async void UpdatePlaybackTime()
        {
            try
            {
                while (IsRunning)
                {
                    await Task.Delay(50);
                    if (UpdateViewModelTime != null)
                    {
                        var update = () =>
                        {
                            IsPlaying = loaded && instance?.PlayState == PlayState.Playing;
                            var pos = loaded && instance != null ? instance.Position + startTime : TimeSpan.Zero;
                            var len = loaded && sound != null ? sound.TotalLength : TimeSpan.Zero;
                            UpdateViewModelTime(loaded, IsPlaying, pos, len);
                        };
                        if (dispatcher != null) await dispatcher.InvokeAsync(update);
                        else update();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                //Cool!
            }
        }

        public void SetCurrentTime(TimeSpan value)
        {
            var wasPlaying = instance?.PlayState == PlayState.Playing;
            instance?.Stop();
            startTime = value;
            instance?.SetRange(new PlayRange(value, sound?.TotalLength ?? TimeSpan.Zero));
            if (wasPlaying)
                instance?.Play();
        }

        public void Play()
        {
            instance?.Play();
        }

        public void Pause()
        {
            instance?.Pause();
        }

        public void SetMasterVolume(double value)
        {
            if (instance != null) instance.Volume = (float)value;
        }
    }
}
