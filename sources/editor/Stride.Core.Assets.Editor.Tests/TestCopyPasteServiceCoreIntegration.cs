// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Yaml;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestCopyPasteServiceCoreIntegration
{
    [DataContract]
    public class CoreTestAsset : Asset
    {
        public string Title { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool PostProcessed { get; set; }
    }

    private class TestCopyProcessor : ICopyProcessor
    {
        public bool Accept(Type dataType) => dataType == typeof(CoreTestAsset);

        public bool Process(ref object data, AttachedYamlAssetMetadata metadata)
        {
            if (data is CoreTestAsset asset)
            {
                asset.Title = $"Processed_{asset.Title}";
                return true;
            }
            return false;
        }
    }

    private class TestPasteProcessor : PasteProcessorBase
    {
        public bool Processed { get; private set; }

        public override bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType)
        {
            return pastedDataType == typeof(CoreTestAsset);
        }

        public override bool ProcessDeserializedData(
            AssetPropertyGraphContainer graphContainer,
            object targetRootObject,
            Type targetMemberType,
            ref object data,
            bool isRootDataObjectReference,
            AssetId? sourceId,
            YamlAssetMetadata<OverrideType> overrides,
            YamlAssetPath basePath)
        {
            Processed = true;
            return true;
        }
    }

    private class TestAssetPostPasteProcessor : AssetPostPasteProcessorBase<CoreTestAsset>
    {
        protected override void PostPasteDeserialization(CoreTestAsset asset)
        {
            asset.PostProcessed = true;
        }
    }

    private class InMemoryClipboardService : IClipboardService
    {
        private string? clipboardText;

        public Task<string?> GetTextAsync() => Task.FromResult(clipboardText);

        public Task SetTextAsync(string? text)
        {
            clipboardText = text;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            clipboardText = null;
            return Task.CompletedTask;
        }
    }

    private readonly AssetPropertyGraphContainer propertyGraphContainer;
    private readonly ICopyPasteService copyPasteService;

    public TestCopyPasteServiceCoreIntegration()
    {
        propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        copyPasteService = new CopyPasteService(propertyGraphContainer);
    }

    [Fact]
    public void TestArgumentValidation()
    {
        // 1. CopyFromAssets null arguments validation
        Assert.Throws<ArgumentNullException>(() => copyPasteService.CopyFromAssets(null!, typeof(string)));
        Assert.Throws<ArgumentNullException>(() => copyPasteService.CopyFromAssets(new (AssetPropertyGraph, AssetId?, object, bool)[0], null!));

        // 2. CanPaste null arguments validation
        Assert.Throws<ArgumentNullException>(() => copyPasteService.CanPaste("some text", null!, typeof(string)));

        // 3. DeserializeCopiedData null arguments validation
        Assert.Throws<ArgumentNullException>(() => copyPasteService.DeserializeCopiedData("some text", null!, typeof(string)));

        // 4. RegisterProcessor null arguments validation
        Assert.Throws<ArgumentNullException>(() => copyPasteService.RegisterProcessor((ICopyProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => copyPasteService.RegisterProcessor((IPasteProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => copyPasteService.RegisterProcessor((IAssetPostPasteProcessor)null!));

        // 5. UnregisterProcessor null arguments validation
        Assert.Throws<ArgumentNullException>(() => copyPasteService.UnregisterProcessor((ICopyProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => copyPasteService.UnregisterProcessor((IPasteProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => copyPasteService.UnregisterProcessor((IAssetPostPasteProcessor)null!));
    }

    [Fact]
    public void TestProcessorRegistrationAndUnregistration()
    {
        var copyProc = new TestCopyProcessor();
        var pasteProc = new TestPasteProcessor();
        var postProc = new TestAssetPostPasteProcessor();

        // Registration
        copyPasteService.RegisterProcessor(copyProc);
        copyPasteService.RegisterProcessor(pasteProc);
        copyPasteService.RegisterProcessor(postProc);

        Assert.Contains(postProc, copyPasteService.PostProcessors);

        // Unregistration
        copyPasteService.UnregisterProcessor(copyProc);
        copyPasteService.UnregisterProcessor(pasteProc);
        copyPasteService.UnregisterProcessor(postProc);

        Assert.DoesNotContain(postProc, copyPasteService.PostProcessors);
    }

    [Fact]
    public void TestCopyFromAssetAndSerialization()
    {
        var asset = new CoreTestAsset { Title = "Initial", Value = 42 };
        var assetItem = new AssetItem("test/path", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
        propertyGraphContainer.RegisterGraph(graph);

        var serialized = copyPasteService.CopyFromAsset(graph, asset.Id, asset, false);
        Assert.NotNull(serialized);
        Assert.Contains("CopyPasteData", serialized);
        Assert.Contains("Initial", serialized);
        Assert.Contains("42", serialized);
    }

    [Fact]
    public void TestCopyProcessorPipeline()
    {
        var copyProc = new TestCopyProcessor();
        copyPasteService.RegisterProcessor(copyProc);

        var asset = new CoreTestAsset { Title = "Original", Value = 100 };
        var assetItem = new AssetItem("test/proc", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
        propertyGraphContainer.RegisterGraph(graph);

        var serialized = copyPasteService.CopyFromAsset(graph, asset.Id, asset, false);
        Assert.NotNull(serialized);
        // Copy processor should have modified the serialized clone
        Assert.Contains("Processed_Original", serialized);
        // Original asset must remain untouched
        Assert.Equal("Original", asset.Title);

        copyPasteService.UnregisterProcessor(copyProc);
    }

    [Fact]
    public void TestPasteProcessorAndPostProcessorExecution()
    {
        var pasteProc = new TestPasteProcessor();
        var postProc = new TestAssetPostPasteProcessor();
        copyPasteService.RegisterProcessor(pasteProc);
        copyPasteService.RegisterProcessor(postProc);

        var asset = new CoreTestAsset { Title = "Target", Value = 10 };
        var assetItem = new AssetItem("test/target", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
        propertyGraphContainer.RegisterGraph(graph);

        var sourceAsset = new CoreTestAsset { Title = "Source", Value = 20 };
        var sourceItem = new AssetItem("test/source", sourceAsset);
        var sourceGraph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, sourceItem, null);
        propertyGraphContainer.RegisterGraph(sourceGraph);

        var serialized = copyPasteService.CopyFromAsset(sourceGraph, sourceAsset.Id, sourceAsset, false);
        Assert.NotNull(serialized);

        Assert.True(copyPasteService.CanPaste(serialized, typeof(CoreTestAsset), typeof(CoreTestAsset)));

        var pasteResult = copyPasteService.DeserializeCopiedData(serialized, asset, typeof(CoreTestAsset));
        Assert.NotNull(pasteResult);
        Assert.Single(pasteResult.Items);
        Assert.True(pasteProc.Processed);

        // Run post-paste processor on the result item
        if (pasteResult.Items[0].Data is CoreTestAsset deserializedAsset)
        {
            var matchingPostProcessor = copyPasteService.PostProcessors.FirstOrDefault(p => p.Accept(deserializedAsset.GetType()));
            Assert.NotNull(matchingPostProcessor);
            matchingPostProcessor.PostPasteDeserialization(deserializedAsset);
            Assert.True(deserializedAsset.PostProcessed);
        }
        else
        {
            Assert.Fail("Deserialized item is not CoreTestAsset");
        }
    }

    [Fact]
    public async Task TestClipboardIntegrationWithMockService()
    {
        var clipboard = new InMemoryClipboardService();
        var pasteProc = new TestPasteProcessor();
        copyPasteService.RegisterProcessor(pasteProc);

        var asset = new CoreTestAsset { Title = "ClipboardAsset", Value = 999 };
        var assetItem = new AssetItem("test/clipboard", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
        propertyGraphContainer.RegisterGraph(graph);

        var serialized = copyPasteService.CopyFromAsset(graph, asset.Id, asset, false);
        Assert.NotNull(serialized);

        // Put in clipboard
        await clipboard.SetTextAsync(serialized);

        // Read from clipboard
        var clipboardContent = await clipboard.GetTextAsync();
        Assert.Equal(serialized, clipboardContent);

        // Validate CanPaste from clipboard text
        Assert.True(copyPasteService.CanPaste(clipboardContent, typeof(CoreTestAsset), typeof(CoreTestAsset)));

        // Deserialize from clipboard text
        var result = copyPasteService.DeserializeCopiedData(clipboardContent!, asset, typeof(CoreTestAsset));
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var itemData = Assert.IsType<CoreTestAsset>(result.Items[0].Data);
        Assert.Equal(999, itemData.Value);
        Assert.Equal("ClipboardAsset", itemData.Title);
    }
}
