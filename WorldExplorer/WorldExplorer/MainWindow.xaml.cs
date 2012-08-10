using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WorldExplorer.Tools3D;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainWindowViewModel model = new MainWindowViewModel(@"C:\emu\bgda\BG\DATA");
            DataContext = model;

            var trackball = new Trackball();
            trackball.EventSource = trackballSource;
            model.CameraTransform = trackball.Transform;

            var skeletonTrackball = new Trackball();
            skeletonTrackball.EventSource = trackballSourceSkeleton;
            model.SkeletonCameraTransform = skeletonTrackball.Transform;
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            MainWindowViewModel model = (MainWindowViewModel)DataContext;
            model.SelectedNode = e.NewValue;
        }

    }
}
