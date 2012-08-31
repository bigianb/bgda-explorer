using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;

namespace WorldExplorer
{
    class SceneVisual3D : ModelVisual3D
    {
        public static readonly DependencyProperty SceneProperty = DependencyProperty.Register("Scene",
                                            typeof(List<ModelVisual3D>),
                                            typeof(SceneVisual3D),
                                            new PropertyMetadata(null, ScenePropertyChanged));

        public List<ModelVisual3D> Scene
        {
            set { SetValue(SceneProperty, value); }
            get { return (List<ModelVisual3D>)GetValue(SceneProperty); }
        }

        protected static void ScenePropertyChanged(DependencyObject obj,DependencyPropertyChangedEventArgs args)
        {
            if (obj != null)
            {
                (obj as SceneVisual3D).ScenePropertyChanged(args);
            }
        }

        protected void ScenePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            Children.Clear();
            List<ModelVisual3D> theScene = Scene;
            if (theScene != null)
            {
                foreach (var element in theScene)
                {
                    Children.Add(element);
                }
            }
        }
    }
}
