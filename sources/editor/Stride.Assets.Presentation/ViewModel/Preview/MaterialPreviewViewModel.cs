using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public class MaterialPreviewViewModel : AssetPreviewViewModel
{
    private object? materialPreview;
    private object? selectedPrimitive;

    public MaterialPreviewViewModel(SessionViewModel session)
        : base(session)
    {
    }

    public Type PrimitiveTypes => Type.GetType("Stride.Assets.Editor.Preview.MaterialPreviewPrimitive, Stride.Assets.Editor") ?? typeof(Enum);

    public object? SelectedPrimitive
    {
        get => selectedPrimitive;
        set
        {
            SetValue(ref selectedPrimitive, value);
            SetPrimitive(value);
        }
    }

    public override void AttachPreview(IAssetPreview preview)
    {
        materialPreview = preview;
        SetPrimitive(SelectedPrimitive);
    }

    private void SetPrimitive(object? primitive)
    {
        if (materialPreview != null && primitive != null)
        {
            var method = materialPreview.GetType().GetMethod("SetPrimitive");
            if (method != null)
            {
                var paramType = method.GetParameters()[0].ParameterType;
                object val = paramType.IsEnum ? Enum.ToObject(paramType, Convert.ToInt32(primitive)) : primitive;
                method.Invoke(materialPreview, new[] { val });
            }
        }
    }
}
