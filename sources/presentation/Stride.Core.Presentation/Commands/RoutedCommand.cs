// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Windows.Input;

namespace Stride.Core.Presentation.Commands;

/// <summary>
/// A command that represents a routed or named command, compatible with <see cref="ICommandBase"/>.
/// </summary>
public class RoutedCommand : ICommandBase
{
    private bool isEnabled = true;
    private readonly Action<object?>? executeAction;
    private readonly Func<object?, bool>? canExecuteFunc;

    public string Name { get; }

    public Type? OwnerType { get; }

    public RoutedCommand(string name)
        : this(name, null, null, null)
    {
    }

    public RoutedCommand(string name, Type? ownerType)
        : this(name, ownerType, null, null)
    {
    }

    public RoutedCommand(string name, Type? ownerType, Action<object?>? executeAction, Func<object?, bool>? canExecuteFunc = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OwnerType = ownerType;
        this.executeAction = executeAction;
        this.canExecuteFunc = canExecuteFunc;
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled != value)
            {
                isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (!isEnabled)
            return false;

        return canExecuteFunc?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        executeAction?.Invoke(parameter);
    }

    public void Execute()
    {
        Execute(null);
    }
}
