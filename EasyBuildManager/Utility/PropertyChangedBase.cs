using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using EasyBuildManager.Annotations;
using Microsoft.VisualStudio.Shell;

namespace EasyBuildManager
{
    public abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null)
                return;
            Action handlerAction = () => handler(this, new PropertyChangedEventArgs(propertyName));
            Application.Current.Dispatcher.InvokeAsync(handlerAction);
        }
    }
}