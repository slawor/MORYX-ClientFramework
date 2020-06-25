﻿using Caliburn.Micro;

namespace Marvin.ClientFramework.Dialog
{
    /// <summary>
    /// Dialog screen implementation which provides the try close parameter in the result property
    /// </summary>
    public class DialogScreen : Screen, IDialogScreen
    {
        public override void TryClose(bool? dialogResult = null)
        {
            Result = dialogResult.HasValue && dialogResult.Value;

            base.TryClose(dialogResult);
        }

        /// <summary>
        /// Returns the result of the TryClose
        /// </summary>
        public bool Result { get; private set; }
    }
}