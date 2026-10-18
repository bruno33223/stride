// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Markup.Xaml.Templates;
using Stride.Core.Presentation.Avalonia.Views;
using Xunit;

namespace Stride.Core.Presentation.Tests;

public class TestTemplateProviderSelector
{
    private class DummyTemplateProvider : TemplateProviderBase
    {
        private readonly string name;

        [SetsRequiredMembers]
        public DummyTemplateProvider(string name)
        {
            this.name = name;
            Template = new DataTemplate();
        }

        public override string Name => name;

        public override bool Match(object obj) => true;
    }

    [Fact]
    public void TestRegisterDuplicateTemplateProviderShouldBeIdempotentAndNotThrow()
    {
        var selector = new TemplateProviderSelector();
        var provider = new DummyTemplateProvider("ProviderA");

        // Primeira chamada deve registrar com sucesso
        selector.RegisterTemplateProvider(provider);
        Assert.True(selector.Match(new object()));

        // Segunda chamada com o mesmo provedor deve ser idempotente e não lançar InvalidOperationException
        selector.RegisterTemplateProvider(provider);
        Assert.True(selector.Match(new object()));

        // Registro de provedor substituto com o mesmo nome deve substituir/ignorar sem lançar exceção
        var replacementProvider = new DummyTemplateProvider("ProviderA");
        selector.RegisterTemplateProvider(replacementProvider);
        Assert.True(selector.Match(new object()));

        // Remoção deve funcionar corretamente com o provedor registrado
        selector.UnregisterTemplateProvider(replacementProvider);
        Assert.False(selector.Match(new object()));
    }
}
