using System.ComponentModel;

namespace WorldExplorer;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// The MainWindowViewModel that contains a lot of the information about the application.
    /// </summary>
    public MainWindowViewModel MainViewModel { get; set; }

    protected BaseViewModel(MainWindowViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion // INotifyPropertyChanged Members
}