// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Windows
{
    public static class DialogHelper
    {
        /// <summary>
        /// Create buttons corresponding to the provided button <paramref name="captions"/>
        /// </summary>
        /// <param name="captions">An enumeration of button captions.</param>
        /// <param name="defaultIndex">The 1-based index of the button that is the default button, or <c>null</c> if there is no default button.</param>
        /// <param name="cancelIndex">The 1-based index of the button that is the cancel button, or <c>null</c> if there is no cancel button.</param>
        /// <returns>An enumeration of buttons corresponding to the provided button <paramref name="captions"/>, which associated value are successived indices starting at <c>1</c>.</returns>
        /// <remarks>
        /// Index <c>0</c> is reserved for when the dialog is closed without clicking on a button.
        /// </remarks>
        [ItemNotNull, NotNull]
        public static IReadOnlyList<DialogButtonInfo> CreateButtons([NotNull] IEnumerable<string> captions, int? defaultIndex = null, int? cancelIndex = null)
        {
            var i = 1;
            var buttons = captions.Select(s =>
            {
                var buttonInfo = new DialogButtonInfo
                {
                    Content = s,
                    Result = i,
                    IsDefault = defaultIndex == i,
                    IsCancel = cancelIndex == i
                };
                i++;
                return buttonInfo;
            });
            return buttons.ToList();
        }
    }
}
