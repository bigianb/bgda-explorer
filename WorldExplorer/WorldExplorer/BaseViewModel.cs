
using System.ComponentModel;

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
