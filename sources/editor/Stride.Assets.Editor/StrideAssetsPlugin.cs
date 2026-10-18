// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Views;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModels;

#nullable enable

namespace Stride.Assets.Editor
{
    public abstract class StrideAssetsPlugin : AssetsEditorPlugin
    {
        private readonly Dictionary<object, object> enumImagesDictionary = [];
        private readonly List<ITemplateProvider> templateProviderList = [];

        protected virtual void RegisterResourceDictionary(IDictionary<object, object> dictionary)
        {
            foreach (var (key, value) in dictionary)
            {
                if (key is Enum)
                {
                    enumImagesDictionary.Add(key, value);
                }

                if (value is ITemplateProvider provider)
                {
                    templateProviderList.Add(provider);
                }
            }
        }

        protected abstract void Initialize(ILogger logger);

        /// <inheritdoc />
        public sealed override void InitializePlugin(ILogger logger)
        {
            enumImagesDictionary.Clear();
            templateProviderList.Clear();

            Initialize(logger);
        }

        /// <inheritdoc />
        public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
        {
            foreach (var type in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes()))
            {
                var serializer = SerializerSelector.AssetWithReuse.GetSerializer(type);
                if (serializer != null)
                {
                    var serializerType = serializer.GetType();
                    if (serializerType.IsGenericType && serializerType.GetGenericTypeDefinition() == typeof(ReferenceSerializer<>))
                    {
                        primitiveTypes.Add(type);
                    }
                }
            }

            primitiveTypes.Add(typeof(AssetReference));
            primitiveTypes.Add(typeof(UrlReferenceBase));
        }

        /// <inheritdoc />
        public sealed override void RegisterEnumImages(IDictionary<object, object> enumImages)
        {
            foreach (var pair in enumImagesDictionary)
                enumImages.Add(pair);
        }

        /// <inheritdoc />
        public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postPasteProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
        {
            templateProviders.AddRange(templateProviderList);
        }

        /// <inheritdoc />
        public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } attribute)
                {
                    assetPreviewViewModelTypes.Add(attribute.AssetPreviewType, type);
                }
            }
        }

        /// <inheritdoc />
        public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
        {
        }
    }
}

namespace Stride.Editor
{
    public abstract class StrideAssetsPlugin : Stride.Assets.Editor.StrideAssetsPlugin
    {
    }
}
