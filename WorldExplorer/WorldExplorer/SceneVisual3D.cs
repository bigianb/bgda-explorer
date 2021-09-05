using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorldExplorer
{
    internal class SceneVisual3D : ModelVisual3D
    {
        public static readonly DependencyProperty SceneProperty = DependencyProperty.Register("Scene",
            typeof(List<ModelVisual3D>),
            typeof(SceneVisual3D),
            new PropertyMetadata(null, ScenePropertyChanged));

        public List<ModelVisual3D>? Scene
        {
            set => SetValue(SceneProperty, value);
            get => (List<ModelVisual3D>?)GetValue(SceneProperty);
        }

        protected static void ScenePropertyChanged(DependencyObject? obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SceneVisual3D)?.ScenePropertyChanged(args);
        }

        protected void ScenePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var target = VisualTreeHelper.GetParent(this) as ModelVisual3D;
            if (target == null)
            {
                target = this;
            }

            target.Children.Clear();
            var theScene = Scene;
            if (theScene != null)
            {
                foreach (var element in theScene)
                {
                    target.Children.Add(element);
                }
            }
        }
    }
}