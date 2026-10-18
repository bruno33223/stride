// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.ViewModels;

[DebuggerDisplay("Folder = {" + nameof(Name) + "}")]
public sealed class EntityFolderViewModel : EntityHierarchyItemViewModel
{
    public const char FolderSeparator = '/';
    internal const StringComparison FolderCase = StringComparison.OrdinalIgnoreCase;
    private string name;

    public EntityFolderViewModel(EntityHierarchyViewModel asset, string name, IEnumerable<EntityDesign> entities)
        : base(asset, entities)
    {
        this.name = name;
        Id = new AbsoluteId(Asset.Id, Guid.NewGuid());
        RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
    }

    public override AbsoluteId Id { get; }

    [NotNull]
    public string Path => CombinePath((Parent as EntityFolderViewModel)?.Path, Name);

    public override string? Name
    {
        get => name;
        set => SetValue(ref name, value ?? string.Empty);
    }

    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <summary>
    /// Deletes this folder. The folder must not contain any entity before being deleted.
    /// </summary>
    public void Delete()
    {
        if (InnerSubEntities.Any()) throw new InvalidOperationException("The folder being deleted is not empty.");
        if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
        Parent.RemoveItem(this);
    }

    [NotNull]
    public ICommandBase RenameCommand { get; }

    [NotNull]
    public static string CombinePath(string? leftPath, string? rightPath)
    {
        if (string.IsNullOrEmpty(leftPath))
            return rightPath?.Trim(FolderSeparator) ?? string.Empty;
        if (string.IsNullOrEmpty(rightPath))
            return leftPath.Trim(FolderSeparator);
        return (leftPath.TrimEnd(FolderSeparator) + FolderSeparator + rightPath.TrimStart(FolderSeparator)).Trim(FolderSeparator);
    }

    public override EntityHierarchyElementViewModel Owner
    {
        get
        {
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
            return Parent.Owner;
        }
    }

    public override int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection)
    {
        var indexInFolder = 0;
        var i = 0;
        foreach (var entity in entityCollection)
        {
            if (i++ == indexInEntity)
                break;

            if (string.Equals(entity.Folder, Path, FolderCase))
                ++indexInFolder;
        }
        return indexInFolder;
    }

    public override GraphNodePath GetNodePath()
    {
        return Parent?.GetNodePath() ?? new GraphNodePath(GetNode());
    }
}
