// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors
{
    public static class SymbolExtensions
    {
        public static string? GetFullNamespace(this ISymbol symbol)
        {
            if (string.IsNullOrEmpty(symbol.ContainingNamespace?.Name))
            {
                return null;
            }

            // get the rest of the full namespace string
            var restOfResult = symbol.ContainingNamespace.GetFullNamespace();

            var result = symbol.ContainingNamespace.Name;

            if (restOfResult != null)
                // if restOfResult is not null, append it after a period
                result = restOfResult + '.' + result;

            return result;
        }
    }

    public class EntityComponentSourceCodeResolver : IEntityComponentSourceCodeResolver
    {
        private object? watcher;
        private readonly Dictionary<string, List<Type>> typesForPath = new Dictionary<string, List<Type>>();

        public EntityComponentSourceCodeResolver()
        {
        }

        public Compilation? LatestCompilation { get; private set; }

        public event EventHandler? LatestCompilationChanged;

        public async Task Initialize(SessionViewModel session, object watcher, CancellationToken token)
        {
            this.watcher = watcher;
            if (watcher == null)
                return;

            var assembliesProp = watcher.GetType().GetProperty("TrackedAssemblies");
            if (assembliesProp?.GetValue(watcher) is System.Collections.IEnumerable assemblies)
            {
                foreach (var trackedAssembly in assemblies)
                {
                    var projProp = trackedAssembly.GetType().GetProperty("Project");
                    if (projProp?.GetValue(trackedAssembly) is Project project)
                    {
                        await AnalyzeProject(session, project, token);
                    }
                }
            }
        }

        public async Task AnalyzeProject(SessionViewModel session, Project project, CancellationToken token)
        {
            //Handle Scripts discovery
            var gameProjectCompilation = await project.GetCompilationAsync(token);
            if (gameProjectCompilation == null)
            {
                return;
            }

            // Update latest compilation
            LatestCompilation = gameProjectCompilation;
            LatestCompilationChanged?.Invoke(this, EventArgs.Empty);

            var assemblyFullName = gameProjectCompilation.Assembly.Name;

            var strideComponentType = typeof(EntityComponent);

            var symbols = gameProjectCompilation.GetSymbolsWithName(x => true, SymbolFilter.Type).Cast<ITypeSymbol>().ToList();
            if (!symbols.Any())
            {
                return;
            }

            var assembly = AssemblyRegistry.FindAll().FirstOrDefault(x => x.GetName().Name == assemblyFullName);
            if (assembly == null)
            {
                return;
            }

            var types = assembly.GetTypes();

            var nonScriptsPaths = new HashSet<string>();
            var typesDict = new Dictionary<string, List<Type>>();

            foreach (var symbol in symbols)
            {
                var realType = types.FirstOrDefault(x => x.Name == symbol.Name && x.Namespace == symbol.GetFullNamespace());

                if (realType == null || !strideComponentType.IsAssignableFrom(realType))
                {
                    foreach (var location in symbol.Locations)
                    {
                        if (location.SourceTree?.FilePath != null)
                        {
                            nonScriptsPaths.Add(location.SourceTree.FilePath);
                        }
                    }
                    continue;
                }

                //find the script paths, (could be multiple in the case of partial)
                foreach (var location in symbol.Locations)
                {
                    if (location.SourceTree?.FilePath == null)
                    {
                        continue;
                    }

                    var csPath = new UFile(location.SourceTree.FilePath); // Normalize directory separator

                    if (!typesDict.ContainsKey(csPath))
                    {
                        typesDict.Add(csPath, new List<Type> { realType });
                    }
                    else
                    {
                        typesDict[csPath].Add(realType);
                    }
                }
            }

            lock (this)
            {
                // Ensure files that were previously components but not anymore are properly cleaned up
                foreach (var nonScriptsPath in nonScriptsPaths)
                {
                    typesForPath.Remove(nonScriptsPath);
                }

                foreach (var x in typesDict)
                {
                    typesForPath[x.Key] = x.Value;
                }

                // Clean up paths that do not exist anymore
                var toRemove = typesForPath.Where(x => !File.Exists(x.Key)).Select(x => x.Key).ToList();
                typesForPath.RemoveWhere(x => toRemove.Contains(x.Key));
            } 
        }

        public async void Updater(SessionViewModel session, CancellationToken token)
        {
            if (watcher == null)
                return;

            var batchChangeProp = watcher.GetType().GetProperty("BatchChange");
            if (batchChangeProp?.GetValue(watcher) is IAsyncEnumerable<object> batchChange)
            {
                try
                {
                    await foreach (var listObj in batchChange.WithCancellation(token))
                    {
                        if (listObj is System.Collections.IEnumerable list)
                        {
                            foreach (var change in list)
                            {
                                var projProp = change.GetType().GetProperty("Project");
                                if (projProp?.GetValue(change) is Project project)
                                {
                                    var typeProp = change.GetType().GetProperty("ChangeType");
                                    var typeVal = typeProp?.GetValue(change)?.ToString();
                                    if (typeVal == "Binary")
                                        continue;

                                    await AnalyzeProject(session, project, token);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }

        public IEnumerable<Type> GetTypesFromSourceFile(UFile file)
        {
            if (file == null) return Enumerable.Empty<Type>();

            lock (this)
            {
                return typesForPath.TryGetValue(file, out var types) ? types : Enumerable.Empty<Type>();
            }
        }
    }
}
