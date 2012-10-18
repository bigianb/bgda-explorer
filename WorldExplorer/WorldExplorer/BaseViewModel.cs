using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Imaging;
using WorldExplorer.DataModel;
using System.Windows.Media;

namespace WorldExplorer
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The MainWindowViewModel that contains a lot of the information about the application.
        /// </summary>
        public MainWindowViewModel MainViewModel { get; set; }

        public BaseViewModel(MainWindowViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
