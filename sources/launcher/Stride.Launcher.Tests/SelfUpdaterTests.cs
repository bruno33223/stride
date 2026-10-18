using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Services;
using Stride.Launcher.Tests.Helpers;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class SelfUpdaterTests
{
    [Fact]
    public void IsSupportedPlatform_ReturnsFalse_OnLinux()
    {
        // On Linux/macOS, self-updater must be disabled to prevent overwriting native binaries
        // with Windows launcher NuGet packages.
        Assert.False(SelfUpdater.IsSupportedPlatform);
    }

    [Fact]
    public async Task SelfUpdate_CompletesImmediatelyWithoutAccessingStore_OnLinux()
    {
        var dispatcher = new FakeDispatcherService();
        var dialog = new FakeDialogService();
        var settings = new InMemoryLauncherSettings();
        var services = new ViewModelServiceProvider([dispatcher, dialog, settings]);

        // Passing null store must complete safely without NullReferenceException on non-Windows
        await SelfUpdater.SelfUpdate(services, null!);
    }
}
