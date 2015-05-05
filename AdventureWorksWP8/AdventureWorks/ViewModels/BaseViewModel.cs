using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BuildSessions.Common;

namespace BuildSessions.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public NavigationHelper NavigationHelper;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void RaisePropertyChanged<T>(ref T property, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(property, value))
            {
                property = value;
                NotifyPropertyChanged(propertyName);
            }
        }

        public virtual void Navigate(Type page)
        {
            App.RootFrame.Navigate(page);
        }

        public virtual void Navigate(Type page, Object parameter)
        {
            App.RootFrame.Navigate(page, parameter);
        }
    }
}
