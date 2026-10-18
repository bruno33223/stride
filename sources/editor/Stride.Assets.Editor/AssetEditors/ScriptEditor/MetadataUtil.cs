// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Stride.Assets.Editor.AssetEditors.ScriptEditor;

public static class MetadataUtil
{
    public static string GetAssemblyPath(Assembly assembly) => Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".dll");

    public static IReadOnlyList<Type> LoadTypesByNamespaces(Assembly assembly, params string[] namespaces) =>
        LoadTypesBy(assembly, t => namespaces.Contains(t.Namespace));

    public static unsafe IReadOnlyList<Type> LoadTypesBy(Assembly assembly, Func<TypeInfo, bool> predicate)
    {
        if (!assembly.TryGetRawMetadata(out var metadata, out var length))
        {
            return Array.Empty<Type>();
        }

        var types = new List<Type>();

        MetadataReader reader = new(metadata, length);
        foreach (var typeDefHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeDefHandle);
            var typeInfo = new TypeInfo(reader.GetString(typeDef.Namespace), reader.GetString(typeDef.Name));
            if (predicate(typeInfo))
            {
                var type = assembly.GetType(typeInfo.FullName);
                if (type is not null)
                {
                    types.Add(type);
                }
            }
        }

        return types;
    }

    public record TypeInfo(string Namespace, string Name)
    {
        private string? _fullName;

        public string FullName => _fullName ??= $"{Namespace}.{Name}";
    }
}
