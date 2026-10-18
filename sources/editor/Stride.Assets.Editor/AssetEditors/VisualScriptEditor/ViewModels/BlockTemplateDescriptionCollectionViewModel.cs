// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Assets.Scripts;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;

namespace Stride.Assets.Editor.ViewModels;

public interface IBlockFactory
{
    string Name { get; }

    string Category { get; }

    Task<Block?> Create();
}

public class BlockFromType : IBlockFactory
{
    public BlockFromType(Type blockType)
    {
        if (!typeof(Block).IsAssignableFrom(blockType))
            throw new ArgumentException($"The given type does not derive from {nameof(Block)}");

        Type = blockType;
        Category = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(Type)?.Category ?? "Misc";
    }

    public string Name => Type.Name;

    public Type Type { get; }

    public string Category { get; }

    public virtual Task<Block?> Create()
    {
        return Task.FromResult((Block?)Activator.CreateInstance(Type));
    }
}

public class BlockFromMethod : BlockFromType
{
    private static readonly SymbolDisplayFormat UnqualifiedNameOnlyFormat =
        new SymbolDisplayFormat();

    private static readonly SymbolDisplayFormat QualifiedNameOnlyFormat =
        new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    private readonly VisualScriptEditorViewModel editor;

    public BlockFromMethod(VisualScriptEditorViewModel editor) : base(typeof(MethodCallBlock))
    {
        this.editor = editor;
    }

    public override async Task<Block?> Create()
    {
        var methodSymbol = await editor.AskUserForSymbol() as IMethodSymbol;
        if (methodSymbol == null)
            return null;

        var result = (MethodCallBlock?)await base.Create();
        if (result == null)
            return null;

        result.IsMemberCall = !methodSymbol.IsStatic || methodSymbol.IsExtensionMethod;
        result.MethodName = methodSymbol.ToDisplayString(result.IsMemberCall ? UnqualifiedNameOnlyFormat : QualifiedNameOnlyFormat);

        // Generate default input/output slots
        result.GenerateSlots(result.Slots, new SlotGeneratorContext());

        // Add "this" as slot (if not static)
        if (!methodSymbol.IsStatic)
            result.Slots.Add(new Slot(SlotDirection.Input, SlotKind.Value, "this", type: methodSymbol.ContainingType?.ToDisplayString(QualifiedNameOnlyFormat)));

        // Add parameters as slot
        foreach (var parameter in methodSymbol.Parameters)
            result.Slots.Add(new Slot(SlotDirection.Input, SlotKind.Value, parameter.Name, type: parameter.Type?.ToDisplayString(QualifiedNameOnlyFormat)));

        // Add return slot (if not void)
        if (!methodSymbol.ReturnsVoid)
            result.Slots.Add(new Slot(SlotDirection.Output, SlotKind.Value, type: methodSymbol.ReturnType?.ToDisplayString(QualifiedNameOnlyFormat)));

        return result;
    }
}

public class BlockTemplateDescriptionViewModel : DispatcherViewModel
{
    public BlockTemplateDescriptionViewModel(IViewModelServiceProvider serviceProvider, IBlockFactory blockFactory)
        : base(serviceProvider)
    {
        BlockFactory = blockFactory;
    }

    public IBlockFactory BlockFactory { get; }

    public string Name => BlockFactory.Name;

    public string Category => BlockFactory.Category;
}

public class BlockTemplateGroupViewModel : DispatcherViewModel
{
    public BlockTemplateGroupViewModel(IViewModelServiceProvider serviceProvider, string name)
        : base(serviceProvider)
    {
        Name = name;
    }

    public string Name { get; }

    public ObservableList<BlockTemplateDescriptionViewModel> Templates { get; } = new();
}

public class BlockTemplateDescriptionCollectionViewModel : DispatcherViewModel
{
    public BlockTemplateDescriptionCollectionViewModel(VisualScriptEditorViewModel editor)
        : base(editor.ServiceProvider)
    {
        // Create a template for each non-abstract block type
        foreach (var blockFactory in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes())
            .Where(t => !t.IsAbstract && typeof(Block).IsAssignableFrom(t) && t != typeof(MethodCallBlock) && t.GetConstructor(Type.EmptyTypes) != null)
            .Where(t => TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ObsoleteAttribute>(t) == null)
            .Where(t => TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<NonInstantiableAttribute>(t) == null)
            .Select(t => new BlockFromType(t)))
        {
            RegisterBlockTemplate(blockFactory);
        }

        // Use BlockFromMethod for MethodCallBlock
        RegisterBlockTemplate(new BlockFromMethod(editor));
    }

    public ObservableList<BlockTemplateGroupViewModel> Groups { get; } = new();

    public ObservableList<BlockTemplateDescriptionViewModel> AllTemplates { get; } = new();

    private void RegisterBlockTemplate(IBlockFactory blockFactory)
    {
        var templateVm = new BlockTemplateDescriptionViewModel(ServiceProvider, blockFactory);
        AllTemplates.Add(templateVm);

        var group = Groups.FirstOrDefault(g => g.Name == blockFactory.Category);
        if (group == null)
        {
            group = new BlockTemplateGroupViewModel(ServiceProvider, blockFactory.Category);
            Groups.Add(group);
        }
        group.Templates.Add(templateVm);
    }
}
