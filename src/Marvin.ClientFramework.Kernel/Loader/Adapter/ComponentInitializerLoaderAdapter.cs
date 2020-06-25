﻿using Marvin.Container;
using Marvin.Modules;

namespace Marvin.ClientFramework.Kernel
{
    [KernelComponent(typeof(ILoaderAdapter))]
    public class ComponentInitializerLoaderAdapter : LoaderAdapterBase, ILoaderAdapter
    {
        private IComponentInitializer _componentInitializer;

        public bool CanAdapt(object component)
        {
            return component is IComponentInitializer;
        }

        public void Adapt(object component)
        {
            _componentInitializer = (IComponentInitializer) component;

            _componentInitializer.Starting += OnStarting;
            _componentInitializer.InitializingComponent += OnInitializingComponent;
        }
      

        private void OnInitializingComponent(object sender, IInitializable initializable)
        {
            RaiseChangeValueWithMessage($"Initializing {initializable.GetType().Name} ...");
        }

        private void OnStarting(object sender, int i)
        {
            RaiseChangeMessage($"Component initializer found {i} types. Starting to initialize.");
            RaiseAddToMax(i);
        }
    }
}