// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views
{
    public partial class EntityPickerWindow : Window, INotifyPropertyChanged, IEntityPickerDialog
    {
        private EntityHierarchyItemViewModelWrapper? selectedEntity;
        private DialogResult modalResult = DialogResult.Cancel;

        public class EntityHierarchyItemViewModelWrapper : IPickedEntity
        {
            private readonly bool matchesCriteria;
            private readonly EntityViewModel? entity;

            public EntityHierarchyItemViewModelWrapper(EntityHierarchyItemViewModel entityHierarchyItem, Func<EntityHierarchyItemViewModel, bool> filter, Type? componentType)
            {
                EntityHierarchyItem = entityHierarchyItem;
                entity = entityHierarchyItem as EntityViewModel;
                if (entity != null)
                {
                    if (componentType != null)
                    {
                        Components = new List<Tuple<int, EntityComponent>>();
                        for (var i = 0; i < entity.AssetSideEntity.Components.Count; i++)
                        {
                            var component = entity.AssetSideEntity.Components[i];
                            if (componentType.IsInstanceOfType(component))
                            {
                                Components.Add(Tuple.Create(i, component));
                            }
                        }
                        SelectedComponent = Components.Count > 0 ? Components.First() : null;
                    }
                    matchesCriteria = componentType == null || (Components != null && Components.Count > 0);
                }
                foreach (var subEntity in entityHierarchyItem.Children.Where(filter).Select(x => new EntityHierarchyItemViewModelWrapper(x, filter, componentType)).Where(x => x.MatchesCriteria))
                {
                    Content.Add(subEntity);
                }
            }

            public EntityHierarchyItemViewModel EntityHierarchyItem { get; }

            public string? Name => EntityHierarchyItem.Name;

            public EntityViewModel? Entity => entity;

            EntityViewModel IPickedEntity.Entity => entity!;

            public List<EntityHierarchyItemViewModelWrapper> Content { get; } = new List<EntityHierarchyItemViewModelWrapper>();

            public List<Tuple<int, EntityComponent>>? Components { get; }

            public Tuple<int, EntityComponent>? SelectedComponent { get; set; }

            public bool MatchesCriteria => matchesCriteria || Content.Any(x => x.MatchesCriteria);

            int IPickedEntity.ComponentIndex => SelectedComponent?.Item1 ?? -1;
        }

        public EntityPickerWindow()
        {
            InitializeComponent();
        }

        public EntityPickerWindow([NotNull] EntityHierarchyEditorViewModel editor, Type? targetType)
            : this()
        {
            ArgumentNullException.ThrowIfNull(editor);

            if (targetType != null && (typeof(EntityComponent).IsAssignableFrom(targetType) || targetType.IsAbstract))
            {
                ComponentType = targetType;
                Width *= 2;
            }
            DataContext = this;
            Loaded += (s, e) => OnLoaded(editor);
        }

        public ObservableList<EntityHierarchyItemViewModelWrapper> SceneContent { get; } = new ObservableList<EntityHierarchyItemViewModelWrapper>();

        public Func<EntityHierarchyItemViewModel, bool> Filter { get; set; } = x => true;

        public EntityHierarchyItemViewModelWrapper? SelectedWrapper => selectedEntity;

        public IPickedEntity SelectedEntity
        {
            get => selectedEntity!;
            set
            {
                selectedEntity = (EntityHierarchyItemViewModelWrapper?)value;
                OnPropertyChanged(nameof(SelectedEntity), nameof(SelectedWrapper), nameof(SelectionValid));
            }
        }

        public bool SelectionValid => IsSelectionValid();

        public Type? ComponentType { get; }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                foreach (var name in propertyNames)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public async Task<DialogResult> ShowModal()
        {
            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var owner = desktop?.MainWindow;

            if (owner is not null && owner != this && owner.IsVisible)
            {
                await ShowDialog(owner);
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                Closed += (_, _) => tcs.TrySetResult(true);
                Show();
                await tcs.Task;
            }

            return modalResult;
        }

        public void RequestClose(DialogResult result)
        {
            modalResult = result;
            if (modalResult != DialogResult.Ok)
            {
                selectedEntity = null;
            }
            Close();
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (SelectionValid)
            {
                RequestClose(DialogResult.Ok);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            RequestClose(DialogResult.Cancel);
        }

        private bool IsSelectionValid()
        {
            if (selectedEntity == null)
                return false;

            if (ComponentType != null)
            {
                if (selectedEntity.SelectedComponent == null)
                    return false;

                return ComponentType.IsAssignableFrom(selectedEntity.SelectedComponent.Item2.GetType());
            }

            return true;
        }

        public void OnLoaded([NotNull] EntityHierarchyEditorViewModel editor)
        {
            var items = editor.HierarchyRoot.Children.Where(Filter)
                .Select(x => new EntityHierarchyItemViewModelWrapper(x, Filter, ComponentType))
                .Where(x => x.MatchesCriteria);
            SceneContent.Clear();
            SceneContent.AddRange(items);
        }
    }
}
