﻿namespace Marvin.ClientFramework.Kernel
{
    public interface ILoaderHandler
    {
        /// <summary>
        /// Initializes the loader handler and sets the current loader view on the view property
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the selected loader view
        /// </summary>
        ILoaderView View { get; }

        /// <summary>
        /// Registers the specified components and searches for a possible adapter to route events to the loader handler
        /// </summary>
        void CheckForAdapter(object sender, object instance);
    }
}