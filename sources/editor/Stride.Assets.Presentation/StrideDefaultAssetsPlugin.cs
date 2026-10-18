// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetHighlighters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.ViewModel.CopyPasteProcessors;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.Preview.Views;
using Stride.Engine;
using Stride.Engine.Gizmos;

using IEntityFactory = Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories.IEntityFactory;

namespace Stride.Assets.Presentation;

public sealed class StrideDefaultAssetsPlugin : AssetsPlugin
{
    /// <summary>
    /// Comparer for component types.
    /// </summary>
    private class ComponentTypeComparer : EqualityComparer<Type>
    {
        public static new readonly ComponentTypeComparer Default = new();

        /// <summary>
        /// Compares two component types and returns <c>true</c> if the types match, i.e.:
        /// <list type="bullet">
        /// <item>both types are identical</item>
        /// <item>first type is a subclass of the second type (e.g. StartupScript is a subclass of ScriptComponent)</item>
        /// </list>
        /// </summary>
        public override bool Equals(Type? x, Type? y)
        {
            if (x is null || y is null)
                return false;

            return ReferenceEquals(x, y) || x.IsSubclassOf(y);
        }

        public override int GetHashCode(Type obj)
        {
            return 1; // must all match the same hash so that Equals is called
        }
    }

    private object? effectCompilerServerSession;

    private static readonly Dictionary<Type, Type> GizmoTypes = new();
    private static readonly Dictionary<Type, Type> AssetHighlighterTypes = new();

    public static IReadOnlyDictionary<Type, Type> GizmoTypeDictionary => GizmoTypes;

    public static IReadOnlyDictionary<Type, Type> AssetHighlighterTypesDictionary => AssetHighlighterTypes;

    public static List<EntityFactoryCategory> EntityFactoryCategories { get; private set; } = [];

    public static IReadOnlyList<(Type type, int order)> ComponentOrders { get; private set; } = new List<(Type, int)>();

    public List<PackageSettingsEntry> ProfileSettings { get; } = [];

    public Dictionary<Type, object> TypeImages { get; } = [];

    public StrideDefaultAssetsPlugin()
    {
        ProfileSettings.Add(new PackageSettingsEntry(GameUserSettings.Effect.EffectCompilation, TargetPackage.Executable));
        ProfileSettings.Add(new PackageSettingsEntry(GameUserSettings.Effect.RecordUsedEffects, TargetPackage.Executable));

        LoadDefaultTemplates();
    }

    public static void LoadDefaultTemplates()
    {
        // Load editor-internal templates (asset / script / project-modification .sdtpl files).
        // Currently hardcoded, this will need to change with plugin system.
        foreach (var packageInfo in new[] { new { Name = "Stride.Assets.Presentation", Version = StrideVersion.NuGetVersion }, new { Name = "Stride.SpriteStudio.Offline", Version = StrideVersion.NuGetVersion } })
        {
            try
            {
                var logger = new LoggerResult();
                var packageFile = PackageStore.Instance.GetPackageFileName(packageInfo.Name, new PackageVersionRange(new PackageVersion(packageInfo.Version)));
                if (packageFile is not null)
                {
                    var package = Package.Load(logger, packageFile.ToOSPath());
                    if (!logger.HasErrors && package is not null)
                    {
                        TemplateManager.RegisterPackage(package);
                    }
                }
            }
            catch
            {
                // Ignore package load issues during template discovery in test/unpacked environments
            }
        }

        // Project-creation templates ship across Stride.Templates.Games (bundled NewGame),
        // Stride.Templates.Games.Starters (genre starters), and Stride.Templates.Samples
        // (feature demos) — all dotnet new template packages (vanilla NuGet), not Stride .sdpkgs.
        // The bridge resolves each via PackageStore (auto-pack drops the .nupkg in dev mode),
        // installs into the in-process Microsoft.TemplateEngine bootstrapper, then exposes
        // each dotnet new template as a TemplateDotNetNewDescription so the existing template
        // list UI picks them up.
        try
        {
            var bridgeType = Type.GetType("Stride.Assets.Presentation.Templates.DotNetNewTemplateBridge, Stride.Assets.Presentation")
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Stride.Assets.Presentation.Templates.DotNetNewTemplateBridge")).FirstOrDefault(t => t != null);
            var registerMethod = bridgeType?.GetMethod("RegisterProjectTemplates", BindingFlags.Public | BindingFlags.Static);
            registerMethod?.Invoke(null, null);
        }
        catch
        {
            // Bridge registration is optional in test environments
        }
    }

    /// <inheritdoc />
    public override void InitializePlugin(ILogger logger)
    {
        Initialize(logger);
    }

    /// <inheritdoc />
    private void Initialize(ILogger logger)
    {
        var entityFactories = new Core.Collections.SortedList<EntityFactoryCategory, EntityFactoryCategory>();
        foreach (var factoryType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IEntityFactory).IsAssignableFrom(x) && x.GetConstructor(Type.EmptyTypes) != null))
        {
            var display = factoryType.GetCustomAttribute<DisplayAttribute>();
            if (display == null)
                continue;

            EntityFactoryCategory category;
            var existing = entityFactories.FirstOrDefault(x => x.Key.Name == display.Category);
            if (existing.Key == null)
            {
                category = new EntityFactoryCategory(display.Category);
                entityFactories.Add(category, category);
            }
            else
            {
                category = existing.Key;
            }

            var instance = (IEntityFactory)Activator.CreateInstance(factoryType)!;
            // We use int.MaxValue / 2 to give enough space to all factories that do not have an Order value
            category.AddFactory(instance, display.Name, display.Order ?? int.MaxValue / 2);
        }

        AssemblyRegistry.AssemblyRegistered += (sender, e) =>
        {
            var types = e.Assembly.GetTypes();
            SetTypeExpandRuleFallback(types);

            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
            {
                OnRegisteredAssetAssembly(types);
            }
        };

        AssemblyRegistry.AssemblyUnregistered += (sender, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
            {
                OnUnregisteredAssetAssembly(e.Assembly.GetTypes());
            }
        };

        try
        {
            SetTypeExpandRuleFallback(typeof(EntityComponent).GetInheritedInstantiableTypes().ToArray());
        }
        catch (ReflectionTypeLoadException ex)
        {
            SetTypeExpandRuleFallback(ex.Types.Where(t => t != null && typeof(EntityComponent).IsAssignableFrom(t) && !t.IsAbstract).ToArray()!);
        }
        catch
        {
            // Ignore type loading errors during component discovery
        }

        // Attempt to load and scan editor assembly for Gizmos & Highlighters
        try
        {
            Assembly.Load("Stride.Assets.Editor");
        }
        catch
        {
            // Stride.Assets.Editor may already be in AppDomain or loaded dynamically
        }

        var candidateAssemblies = new List<Assembly>();
        candidateAssemblies.AddRange(AssetRegistry.AssetAssemblies);
        candidateAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic));

        foreach (var assembly in candidateAssemblies.Distinct())
        {
            try
            {
                OnRegisteredAssetAssembly(GetAssemblyTypesSafe(assembly).ToArray());
            }
            catch
            {
                // Ignore type load errors on external assemblies
            }
        }

        EntityFactoryCategories = entityFactories.Keys.ToList();

        RegisterComponentOrders(logger);
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionViewModel session)
    {
        if (session is SessionViewModel sessionViewModel)
        {
            InitializeSession(sessionViewModel);
        }
    }

    /// <inheritdoc />
    public override void InitializeSession(SessionViewModel session)
    {
        try
        {
            var dialogServiceType = Type.GetType("Stride.Assets.Editor.Avalonia.Services.StrideDialogService, Stride.Assets.Editor.Avalonia")
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Stride.Assets.Editor.Avalonia.Services.StrideDialogService")).FirstOrDefault(t => t != null);
            if (dialogServiceType != null)
            {
                var dialogService = Activator.CreateInstance(dialogServiceType);
                if (dialogService != null)
                {
                    session.ServiceProvider.RegisterService(dialogService);
                }
            }
        }
        catch
        {
        }

        try
        {
            var assetsVmType = Type.GetType("Stride.Assets.Editor.ViewModels.StrideAssetsViewModel, Stride.Assets.Editor")
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Stride.Assets.Editor.ViewModels.StrideAssetsViewModel")).FirstOrDefault(t => t != null);
            if (assetsVmType != null)
            {
                Activator.CreateInstance(assetsVmType, session);
            }
        }
        catch
        {
        }

        RegisterNodePresenterCommandsAndUpdaters(session);

        // Connects to effect compiler (to import new effect permutations discovered by running the game)
        try
        {
            if (Stride.Core.Assets.Editor.Settings.EditorSettings.UseEffectCompilerServer.GetValue())
            {
                var sessionType = Type.GetType("Stride.Assets.Editor.EffectCompilerServerSession, Stride.Assets.Editor")
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Stride.Assets.Editor.EffectCompilerServerSession")).FirstOrDefault(t => t != null);
                if (sessionType != null)
                {
                    effectCompilerServerSession = Activator.CreateInstance(sessionType, session);
                }
            }
        }
        catch
        {
            // Optional session connection
        }

        // Extra packages to display in "add reference" dialog
        var packages = session.SuggestedPackages;
        if (packages == null)
        {
            packages = [];
            var suggestedField = typeof(SessionViewModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(f => f.Name.Contains("SuggestedPackages"));
            suggestedField?.SetValue(session, packages);
        }

        packages.Add(new PackageName(typeof(Stride.Engine.EntityComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName(typeof(Stride.UI.UIElement).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName(typeof(Stride.Particles.Components.ParticleSystemComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName(typeof(Stride.Navigation.NavigationComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName(typeof(Stride.Physics.StaticColliderComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName("Stride.BepuPhysics", new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName(typeof(Stride.Video.VideoComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName("Stride.Voxels", new PackageVersion(StrideVersion.NuGetVersion)));
        packages.Add(new PackageName("Stride.SpriteStudio.Runtime", new PackageVersion(StrideVersion.NuGetVersion)));
    }

    private static IEnumerable<Type> GetAssemblyTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Enumerable.Empty<Type>();
        }
    }

    private static void RegisterNodePresenterCommandsAndUpdaters(SessionViewModel session)
    {
        Assembly? editorAssembly = null;
        try
        {
            editorAssembly = Assembly.Load("Stride.Assets.Editor");
        }
        catch
        {
        }

        var candidateAssemblies = new[] { editorAssembly, typeof(StrideDefaultAssetsPlugin).Assembly }
            .Where(a => a != null)
            .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            .Distinct()
            .ToList();

        var commandNames = new[]
        {
            "FetchEntityCommand",
            "SetEntityReferenceCommand",
            "SetComponentReferenceCommand",
            "SetSymbolReferenceCommand",
            "PickupEntityCommand",
            "PickupEntityComponentCommand",
            "EditCurveCommand",
            "SkeletonNodePreserveAllCommand"
        };

        foreach (var cmdName in commandNames)
        {
            var cmdType = candidateAssemblies
                .Select(a => a!.GetType($"Stride.Assets.Editor.Quantum.NodePresenters.Commands.{cmdName}") ?? GetAssemblyTypesSafe(a!).FirstOrDefault(t => t.Name == cmdName))
                .FirstOrDefault(t => t != null);

            if (cmdType != null)
            {
                try
                {
                    var ctor = cmdType.GetConstructor([typeof(SessionViewModel)]) ?? cmdType.GetConstructor(Type.EmptyTypes);
                    var cmdInstance = ctor != null ? (ctor.GetParameters().Length == 1 ? ctor.Invoke([session]) : ctor.Invoke([])) : Activator.CreateInstance(cmdType);
                    if (cmdInstance is INodePresenterCommand command)
                    {
                        session.ActiveProperties.RegisterNodePresenterCommand(command);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Could not register command {cmdName}: {ex.Message}");
                }
            }
        }

        var updaterNames = new[]
        {
            "AnimationAssetNodeUpdater",
            "CameraSlotNodeUpdater",
            "EntityHierarchyAssetNodeUpdater",
            "EntityHierarchyEditorNodeUpdater",
            "GameSettingsAssetNodeUpdater",
            "GraphicsCompositorAssetNodeUpdater",
            "FXAAEffectNodeUpdater",
            "MaterialAssetNodeUpdater",
            "ModelAssetNodeUpdater",
            "ModelNodeLinkNodeUpdater",
            "SkeletonAssetNodeUpdater",
            "SpriteFontAssetNodeUpdater",
            "SpriteSheetAssetNodeUpdater",
            "UIAssetNodeUpdater",
            "TextureAssetNodeUpdater",
            "VideoAssetNodeUpdater",
            "UnloadableObjectPropertyNodeUpdater",
            "VisualScriptNodeUpdater",
            "NavigationNodeUpdater"
        };

        foreach (var updaterName in updaterNames)
        {
            var updaterType = candidateAssemblies
                .Select(a => a!.GetType($"Stride.Assets.Editor.Quantum.NodePresenters.Updaters.{updaterName}") ?? GetAssemblyTypesSafe(a!).FirstOrDefault(t => t.Name == updaterName))
                .FirstOrDefault(t => t != null);

            if (updaterType != null)
            {
                try
                {
                    var ctor = updaterType.GetConstructor([typeof(SessionViewModel)]) ?? updaterType.GetConstructor(Type.EmptyTypes);
                    var updaterInstance = ctor != null ? (ctor.GetParameters().Length == 1 ? ctor.Invoke([session]) : ctor.Invoke([])) : Activator.CreateInstance(updaterType);
                    if (updaterInstance is INodePresenterUpdater updater)
                    {
                        session.ActiveProperties.RegisterNodePresenterUpdater(updater);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Could not register updater {updaterName}: {ex.Message}");
                }
            }
        }
    }

    /// <inheritdoc />
    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        primitiveTypes.Add(typeof(AssetReference));
        primitiveTypes.Add(typeof(UrlReferenceBase));
    }

    /// <inheritdoc />
    public void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (!typeof(IPreviewView).IsAssignableFrom(type))
            {
                continue;
            }

            foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
            {
                assetPreviewViewTypes.Add(attribute.AssetPreviewType, type);
            }
        }
    }

    /// <inheritdoc />
    public void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
    {
        copyProcessors.Add(new EntityComponentCopyProcessor());
    }

    /// <inheritdoc />
    public void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
        pasteProcessors.Add(new EntityComponentPasteProcessor());
        pasteProcessors.Add(new EntityHierarchyPasteProcessor());
        pasteProcessors.Add(new UIHierarchyPasteProcessor());
    }

    /// <inheritdoc />
    public void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postPasteProcessors, SessionViewModel session)
    {
        postPasteProcessors.Add(new ScenePostPasteProcessor());
    }

    /// <inheritdoc />
    protected internal override void SessionDisposed(SessionViewModel session)
    {
        if (effectCompilerServerSession is IDisposable disposable)
        {
            disposable.Dispose();
            effectCompilerServerSession = null;
        }
    }

    /// <summary>
    /// Registers a resource dictionary containing type imagery or preview mappings.
    /// </summary>
    public void RegisterTypeImages(IDictionary<Type, object> dictionary)
    {
        foreach (var entry in dictionary)
        {
            TypeImages[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Get the component type which has the highest order (according to <see cref="ComponentOrderAttribute"/>)
    /// or <see langword="null"/> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    /// <param name="componentTypes">The collection of candidate component types.</param>
    /// <returns>The component type with the highest order value, or <c>null</c>.</returns>
    [return: MaybeNull]
    public static Type GetHighestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, false).FirstOrDefault();
    }

    /// <summary>
    /// Get the component type which has the lowest order (according to <see cref="ComponentOrderAttribute"/>)
    /// or <see langword="null"/> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    /// <param name="componentTypes">The collection of candidate component types.</param>
    /// <returns>The component type with the lowest order value, or <c>null</c>.</returns>
    [return: MaybeNull]
    public static Type GetLowestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, true).FirstOrDefault();
    }

    /// <summary>
    /// Returns an enumeration of component types ordered according to their <see cref="DisplayAttribute.Order"/>.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned first.</remarks>
    /// <param name="componentTypes">An enumeration of component types</param>
    /// <param name="ascending">True if the order is from the lowest order to the highest, False otherwise.</param>
    /// <returns>The sorted component types.</returns>
    public static IEnumerable<Type> GetComponentsByOrder(IEnumerable<Type> componentTypes, bool ascending)
    {
        // Note: ComponentOrders contains the component type in reverse registration order (last registered first).
        // Enumerable.OrderBy and Enumerable.OrderByDescending are stable, so order is preserved.
        var filtered = ComponentOrders.Join(componentTypes, t => t.type, c => c, (t, c) => t, ComponentTypeComparer.Default);
        return (ascending ? filtered.OrderBy(t => t.order) : filtered.OrderByDescending(t => t.order)).Select(t => t.type);
    }

    /// <summary>
    /// Update display name of scripts to have a decent default value if user didn't set one.
    /// </summary>
    private static void SetTypeExpandRuleFallback(Type[] types)
    {
        foreach (var type in types)
        {
            if (type.IsAssignableTo(typeof(EntityComponent)) && TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type, false) == null)
            {
                TypeDescriptorFactory.Default.AttributeRegistry.Register(type, new DisplayAttribute(type.Name) { Expand = ExpandRule.Once });
            }
        }
    }

    private void OnRegisteredAssetAssembly(Type[] types)
    {
        foreach (var type in types)
        {
            if (type.IsAssignableTo(typeof(IGizmo)))
            {
                if (type.GetCustomAttribute<GizmoComponentAttribute>(true) is { } attribute)
                {
                    GizmoTypes[attribute.ComponentType] = type;
                }
            }

            if (type.IsAssignableTo(typeof(AssetHighlighter)))
            {
                foreach (var attribute in type.GetCustomAttributes<AssetHighlighterAttribute>(false).NotNull())
                {
                    AssetHighlighterTypes[attribute.AssetType] = type;
                }
            }
        }
    }

    private void OnUnregisteredAssetAssembly(Type[] types)
    {
        foreach (var type in types)
        {
            if (type.IsAssignableTo(typeof(IGizmo)))
            {
                if (type.GetCustomAttribute<GizmoComponentAttribute>(true) is { } attribute)
                {
                    GizmoTypes.Remove(attribute.ComponentType);
                }
            }

            if (type.IsAssignableTo(typeof(AssetHighlighter)))
            {
                foreach (var attribute in type.GetCustomAttributes<AssetHighlighterAttribute>(false).NotNull())
                {
                    AssetHighlighterTypes.Remove(attribute.AssetType);
                }
            }
        }
    }

    private static void RegisterComponentOrders(ILogger logger)
    {
        var hashSet = new HashSet<int>();
        var componentTypes = AssetRegistry.AssetAssemblies
            .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            .Distinct()
            .SelectMany(x =>
            {
                return GetAssemblyTypesSafe(x).Where(y => typeof(EntityComponent).IsAssignableFrom(y));
            })
            .Distinct();

        var orders = new List<(Type, int)>();
        foreach (var type in componentTypes)
        {
            // Check with inheritance to ensure they have a reachable display attribute
            var attrib = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ComponentOrderAttribute>(type, false);
            if (attrib != null)
            {
                if (!hashSet.Add(attrib.Order))
                {
                    var other = orders.FirstOrDefault(t => t.Item2 == attrib.Order);
                    if (other.Item1 != null)
                    {
                        logger.Warning($"Two entity components have explicitly the same order value ({attrib.Order}): [{other.Item1}], [{type}]");
                    }
                }
                orders.Add((type, attrib.Order));
            }
        }

        // Sort descending by order so that highest order component appears first
        orders = orders.OrderByDescending(t => t.Item2).ToList();
        ComponentOrders = orders;
    }
}
