// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetBuilderEngineIntegration
{
    [Fact]
    public void TestAssetBuildUnitPrioritiesAndComparison()
    {
        var id1 = new AssetBuildUnitIdentifier(Guid.NewGuid(), new AssetId(Guid.NewGuid()));
        var id2 = new AssetBuildUnitIdentifier(Guid.NewGuid(), new AssetId(Guid.NewGuid()));

        var unitA = new Stride.Editor.Build.AnonymousAssetBuildUnit(id1, () => new ListBuildStep())
        {
            PriorityMajor = Stride.Editor.Build.DefaultAssetBuilderPriorities.ScenePriority, // -10
            PriorityMinor = 0
        };

        var unitB = new Stride.Editor.Build.AnonymousAssetBuildUnit(id2, () => new ListBuildStep())
        {
            PriorityMajor = Stride.Editor.Build.DefaultAssetBuilderPriorities.PreviewPriority, // 10
            PriorityMinor = 5
        };

        var unitC = new Stride.Editor.Build.AnonymousAssetBuildUnit(id1, () => new ListBuildStep())
        {
            PriorityMajor = Stride.Editor.Build.DefaultAssetBuilderPriorities.PreviewPriority, // 10
            PriorityMinor = 10
        };

        Assert.True(unitA.CompareTo(unitB) < 0);
        Assert.True(unitB.CompareTo(unitA) > 0);
        Assert.True(unitB.CompareTo(unitC) < 0);
        Assert.True(unitC.CompareTo(unitB) > 0);
        Assert.Equal(0, unitB.CompareTo(unitB));

        // QueuePosition checks
        Assert.Equal(QueuePosition.First, (QueuePosition)0);
        Assert.Equal(QueuePosition.Last, (QueuePosition)1);
    }

    [Fact]
    public async Task TestAssetBuilderServiceLifecycleAndQueueOperations()
    {
        var tempBuildDir = Path.Combine(Path.GetTempPath(), "Stride_Test_BuildService_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempBuildDir);

        try
        {
            using var builderService = new AssetBuilderService(tempBuildDir);
            Assert.Equal(0, builderService.QueuedBuildUnitCount);

            bool stepExecuted = false;
            var buildStep = new ListBuildStep();
            var unit = new Stride.Editor.Build.AnonymousAssetBuildUnit(AssetBuildUnitIdentifier.Default, () =>
            {
                stepExecuted = true;
                return buildStep;
            })
            {
                PriorityMajor = 0,
                PriorityMinor = 0
            };

            var node = builderService.PushBuildUnit(unit);
            Assert.NotNull(node);

            var status = await unit.Wait();
            Assert.True(stepExecuted);
            Assert.Equal(ResultStatus.Successful, status);
            Assert.True(unit.Processed);
            Assert.True(unit.Succeeded);
            Assert.False(unit.Failed);
        }
        finally
        {
            if (Directory.Exists(tempBuildDir))
            {
                try
                {
                    Directory.Delete(tempBuildDir, true);
                }
                catch
                {
                    // Ignore cleanup lock issues on temporary test directory
                }
            }
        }
    }

    [Fact]
    public void TestPrecompiledAssetBuildUnitLifecycle()
    {
        var listStep = new ListBuildStep();
        var id = new AssetBuildUnitIdentifier(Guid.NewGuid(), new AssetId(Guid.NewGuid()));
        var precompiledUnit = new Stride.Editor.Build.PrecompiledAssetBuildUnit(id, listStep, mergeInCommonDatabase: false);

        Assert.Equal(id.ContextIdentifier, precompiledUnit.Identifier.ContextIdentifier);
        Assert.Equal(id.AssetIdentifier, precompiledUnit.Identifier.AssetIdentifier);

        var preparedStep = precompiledUnit.GetBuildStep();
        Assert.Same(listStep, preparedStep);
    }

    [Fact]
    public void TestAssetBuiltEventArgsContract()
    {
        var dummyItem = new AssetItem("Test/Location", new Stride.Assets.GameSettingsAsset());
        var log = new LoggerResult();
        var args = new AssetBuiltEventArgs(dummyItem, log);

        Assert.Same(dummyItem, args.AssetItem);
        Assert.Same(log, args.BuildLog);
    }

    [Fact]
    public void TestStudioBuildServicesParityAndTypeContracts()
    {
        // Must verify that all Slice-2 required classes exist in Stride.Assets.Editor.Build as specified in Milestone P25
        var assetsEditorAssembly = typeof(Stride.Assets.Editor.StrideEditorPlugin).Assembly;

        var expectedTypes = new[]
        {
            "Stride.Assets.Editor.Build.GameStudioBuilderService",
            "Stride.Assets.Editor.Build.GameStudioDatabase",
            "Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities",
            "Stride.Assets.Editor.Build.PrecompiledAssetBuildUnit",
            "Stride.Assets.Editor.Build.EditorGameBuildUnit",
            "Stride.Assets.Editor.Build.AnonymousAssetBuildUnit",
            "Stride.Assets.Editor.Build.IGameSettingsAccessor"
        };

        foreach (var typeName in expectedTypes)
        {
            var resolvedType = assetsEditorAssembly.GetType(typeName);
            Assert.True(resolvedType != null, $"Type {typeName} must be defined in Stride.Assets.Editor assembly for Milestone P25 Slice-2 parity.");
        }

        // Verify priorities in Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities
        var prioritiesType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities");
        Assert.NotNull(prioritiesType);
        var scenePriority = (int)prioritiesType.GetField("ScenePriority", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var previewPriority = (int)prioritiesType.GetField("PreviewPriority", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var thumbPriority = (int)prioritiesType.GetField("ThumbnailPriority", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        Assert.Equal(-10, scenePriority);
        Assert.Equal(10, previewPriority);
        Assert.Equal(20, thumbPriority);
    }

    [Fact]
    public void TestStrideAssetsEditorBuildUnitsDirectOperations()
    {
        // Verify direct execution of Stride.Assets.Editor.Build classes
        var id = new AssetBuildUnitIdentifier(Guid.NewGuid(), new AssetId(Guid.NewGuid()));
        bool anonymousCompiled = false;
        var anonUnit = new Stride.Assets.Editor.Build.AnonymousAssetBuildUnit(id, () =>
        {
            anonymousCompiled = true;
            return new ListBuildStep();
        });

        Assert.Equal(id.ContextIdentifier, anonUnit.Identifier.ContextIdentifier);
        Assert.Equal(id.AssetIdentifier, anonUnit.Identifier.AssetIdentifier);
        var step = anonUnit.GetBuildStep();
        Assert.NotNull(step);
        Assert.True(anonymousCompiled);

        var precompiledStep = new ListBuildStep();
        var precompiledUnit = new Stride.Assets.Editor.Build.PrecompiledAssetBuildUnit(id, precompiledStep, false);
        Assert.Same(precompiledStep, precompiledUnit.GetBuildStep());

        Assert.Equal(-10, Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities.ScenePriority);
        Assert.Equal(10, Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities.PreviewPriority);
        Assert.Equal(20, Stride.Assets.Editor.Build.DefaultAssetBuilderPriorities.ThumbnailPriority);
    }
}
