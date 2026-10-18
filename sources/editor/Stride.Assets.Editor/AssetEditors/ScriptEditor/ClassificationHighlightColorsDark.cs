// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.AssetEditors.ScriptEditor
{
    /// <summary>
    /// Highlighting colors for dark theme.
    /// </summary>
    public class ClassificationHighlightColorsDark
    {
        public static readonly Color DefaultColor = new Color(220, 220, 220);
        public static readonly Color TypeColor = new Color(78, 201, 176);
        public static readonly Color KeywordColor = new Color(86, 156, 214);
        public static readonly Color CommentColor = new Color(87, 166, 74);
        public static readonly Color XmlCommentColor = new Color(87, 166, 74);
        public static readonly Color PreprocessorKeywordColor = new Color(155, 155, 155);
        public static readonly Color StringColor = new Color(214, 157, 133);
        public static readonly Color MethodColor = new Color(78, 201, 176);

        public Color Default => DefaultColor;
        public Color Type => TypeColor;
        public Color Comment => CommentColor;
        public Color XmlComment => XmlCommentColor;
        public Color Keyword => KeywordColor;
        public Color PreprocessorKeyword => PreprocessorKeywordColor;
        public Color String => StringColor;
        public Color Method => MethodColor;
    }
}
