// Copyright (c) 2020, Phoenix Contact GmbH & Co. KG
// Licensed under the Apache License, Version 2.0

using System;

namespace Moryx.ClientFramework.Dialog 
{
    /// <summary>
    /// Possible options for an message box 
    /// </summary>
    [Flags]
    public enum MessageBoxOptions 
    {
        /// <summary>
        /// ok
        /// </summary>
        Ok = 2,

        /// <summary>
        /// cancel
        /// </summary>
        Cancel = 4,

        /// <summary>
        /// yes
        /// </summary>
        Yes = 8,

        /// <summary>
        /// no
        /// </summary>
        No = 16,

        /// <summary>
        /// ok and cancel
        /// </summary>
        OkCancel = Ok | Cancel,

        /// <summary>
        /// yes and no
        /// </summary>
        YesNo = Yes | No,

        /// <summary>
        /// yes, no and cancel
        /// </summary>
        YesNoCancel = Yes | No | Cancel
    }
}
