// Copyright (c) 2020, Phoenix Contact GmbH & Co. KG
// Licensed under the Apache License, Version 2.0

using Caliburn.Micro;

namespace Moryx.Controls.Demo.ViewModels
{
    public class NavigationBarViewModel : Screen
    {
        private bool _isNavigationBarLocked;

        public override string DisplayName => "NavigationBar";

        public bool IsNavigationBarLocked
        {
            get { return _isNavigationBarLocked; }
            set
            {
                if (_isNavigationBarLocked != value)
                {
                    _isNavigationBarLocked = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
