// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Presentation.Tests;

public class TestAnonymousCommand
{
    private class BaseClass { }
    private class DerivedClass : BaseClass { }

    [Fact]
    public void TestCanExecuteWithIncompatibleTypeShouldReturnFalseNotThrow()
    {
        var serviceProvider = new ViewModelServiceProvider();

        var stringCommand = new AnonymousCommand<string>(serviceProvider, _ => { });
        Assert.True(stringCommand.CanExecute("valid"));
        // Comportamento esperado: retornar false para tipos incompatíveis
        Assert.False(stringCommand.CanExecute(new object()));
        Assert.False(stringCommand.CanExecute(123));

        var intCommand = new AnonymousCommand<int>(serviceProvider, _ => { });
        Assert.True(intCommand.CanExecute(42));
        Assert.False(intCommand.CanExecute("not an int"));
        Assert.False(intCommand.CanExecute(new object()));

        var customCommand = new AnonymousCommand<BaseClass>(serviceProvider, _ => { });
        Assert.True(customCommand.CanExecute(new BaseClass()));
        Assert.True(customCommand.CanExecute(new DerivedClass()));
        Assert.False(customCommand.CanExecute(new object()));
        Assert.False(customCommand.CanExecute("incompatible"));
    }

    [Fact]
    public void TestAnonymousTaskCommandCanExecuteWithIncompatibleTypeShouldReturnFalse()
    {
        var serviceProvider = new ViewModelServiceProvider();

        var taskCommand = new AnonymousTaskCommand<string>(serviceProvider, async _ => await System.Threading.Tasks.Task.CompletedTask);
        Assert.True(taskCommand.CanExecute("valid"));
        Assert.False(taskCommand.CanExecute(new object()));
        Assert.False(taskCommand.CanExecute(123));
    }
}
