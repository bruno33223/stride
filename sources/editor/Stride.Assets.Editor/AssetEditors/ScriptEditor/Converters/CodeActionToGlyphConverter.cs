// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Copyright 2016 Eli Arbel
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Globalization;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn.CodeActions;
using RoslynPad.Roslyn.Completion;

namespace Stride.Assets.Editor.AssetEditors.ScriptEditor.Converters
{
    public sealed class CodeActionToGlyphConverter
    {
        public static readonly CodeActionToGlyphConverter Instance = new();

        public object? Convert(object? value, Type? targetType = null, object? parameter = null, CultureInfo? culture = null)
        {
            if (value is CodeAction codeAction)
            {
                var glyphNumber = codeAction.GetGlyph();
                return glyphNumber;
            }
            return null;
        }

        public object? ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
