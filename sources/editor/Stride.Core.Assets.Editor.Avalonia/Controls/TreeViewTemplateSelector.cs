// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Stride.Core.Assets.Presentation.ViewModels;
using AssetMountPointViewModel = Stride.Core.Assets.Editor.ViewModel.AssetMountPointViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.Controls
{
    public class TreeViewTemplateSelector : IDataTemplate
    {
        public IDataTemplate? AssetMountPointTemplate { get; set; }

        public IDataTemplate? DirectoryTemplate { get; set; }

        public IDataTemplate? PackageTemplate { get; set; }

        public IDataTemplate? DependencyCategoryTemplate { get; set; }

        public IDataTemplate? ProjectTemplate { get; set; }

        public IDataTemplate? ProjectCodeTemplate { get; set; }

        public IDataTemplate? PackageReferenceTemplate { get; set; }

        public IDataTemplate? SelectTemplate(object? item)
        {
            if (item == null)
            {
                return null;
            }
            if (item is ProjectViewModel)
            {
                return ProjectTemplate;
            }
            if (item is PackageViewModel)
            {
                return PackageTemplate;
            }
            if (item is DirectoryViewModel)
            {
                return DirectoryTemplate;
            }
            if (item is AssetMountPointViewModel)
            {
                return AssetMountPointTemplate;
            }
            if (item is DependencyCategoryViewModel)
            {
                return DependencyCategoryTemplate;
            }
            if (item is ProjectCodeViewModel)
            {
                return ProjectCodeTemplate;
            }
            if (item is PackageReferenceViewModel)
            {
                return PackageReferenceTemplate;
            }
            throw new ArgumentException("The type of item is unsupported.");
        }

        public Control? Build(object? data)
        {
            var template = SelectTemplate(data);
            return template?.Build(data);
        }

        public bool Match(object? data)
        {
            return data is ProjectViewModel
                || data is PackageViewModel
                || data is DirectoryViewModel
                || data is AssetMountPointViewModel
                || data is DependencyCategoryViewModel
                || data is ProjectCodeViewModel
                || data is PackageReferenceViewModel;
        }
    }
}
