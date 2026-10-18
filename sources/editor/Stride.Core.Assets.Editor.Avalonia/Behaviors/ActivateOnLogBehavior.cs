// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    /// <summary>
    /// The base class for a behavior that allows to activate the associated object when an observable collection changes.
    /// </summary>
    /// <typeparam name="T">The type the behavior can be attached to.</typeparam>
    public abstract class ActivateOnCollectionChangedBehavior<T> : Behavior<T> where T : AvaloniaObject
    {
        public static readonly StyledProperty<INotifyCollectionChanged?> CollectionProperty =
            AvaloniaProperty.Register<ActivateOnCollectionChangedBehavior<T>, INotifyCollectionChanged?>(nameof(Collection));

        static ActivateOnCollectionChangedBehavior()
        {
            CollectionProperty.Changed.AddClassHandler<ActivateOnCollectionChangedBehavior<T>, INotifyCollectionChanged?>((b, e) =>
                b.OnCollectionPropertyChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault()));
        }

        public INotifyCollectionChanged? Collection
        {
            get => GetValue(CollectionProperty);
            set => SetValue(CollectionProperty, value);
        }

        private void OnCollectionPropertyChanged(INotifyCollectionChanged? oldValue, INotifyCollectionChanged? newValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= OnCollectionChanged;

            if (newValue != null)
                newValue.CollectionChanged += OnCollectionChanged;
        }

        protected override void OnDetaching()
        {
            if (Collection != null)
                Collection.CollectionChanged -= OnCollectionChanged;
            base.OnDetaching();
        }

        protected abstract void Activate();

        protected virtual bool MatchChange(NotifyCollectionChangedEventArgs e) => true;

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (MatchChange(e))
            {
                Activate();
            }
        }
    }

    /// <summary>
    /// The base class for a behavior that allows to activate the associated object when an observable collection of <see cref="ILogMessage"/>
    /// receives a new message which has an equal or greater level comparing to the <see cref="MinimumLevel"/> of this behavior.
    /// </summary>
    /// <typeparam name="T">The type of control associated to this behavior.</typeparam>
    public abstract class ActivateOnLogBehavior<T> : ActivateOnCollectionChangedBehavior<T> where T : AvaloniaObject
    {
        private bool selectionDone;

        public static readonly StyledProperty<LogMessageType> MinimumLevelProperty =
            AvaloniaProperty.Register<ActivateOnLogBehavior<T>, LogMessageType>(nameof(MinimumLevel), LogMessageType.Debug);

        public LogMessageType MinimumLevel
        {
            get => GetValue(MinimumLevelProperty);
            set => SetValue(MinimumLevelProperty, value);
        }

        protected override bool MatchChange(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                selectionDone = false;
            }
            if (e.Action == NotifyCollectionChangedAction.Add && !selectionDone && e.NewItems != null)
            {
                if (e.NewItems.OfType<ILogMessage>().Any(x => x.Type >= MinimumLevel || x.IsAtLeast(MinimumLevel)))
                {
                    selectionDone = true;
                }
            }
            return selectionDone;
        }
    }

    /// <summary>
    /// An implementation of the <see cref="ActivateOnLogBehavior{T}"/> for the <see cref="TabItem"/> control.
    /// </summary>
    public class TabItemActivateOnLogBehavior : ActivateOnLogBehavior<TabItem>
    {
        protected override void Activate()
        {
            if (AssociatedObject != null)
                AssociatedObject.IsSelected = true;
        }
    }
}
