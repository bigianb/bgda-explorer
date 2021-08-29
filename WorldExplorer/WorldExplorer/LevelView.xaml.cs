/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.WorldDefs;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for LevelView.xaml
    /// </summary>
    public partial class LevelView : UserControl
    {
        private LevelViewModel _lvm;

        public LevelView()
        {
            InitializeComponent();
            DataContextChanged += LevelView_DataContextChanged;
            viewport.MouseUp += viewport_MouseUp;
            viewport.KeyDown += Viewport_KeyDown;
            ElementSelected(null);
        }

        private void Viewport_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.L:
                    {
                        // Toggle lighting
                        _lvm.EnableLevelSpecifiedLights = !_lvm.EnableLevelSpecifiedLights;
                    }
                    break;
            }
        }

        private void LevelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is LevelViewModel lvm))
            {
                // Cleared level view
                _lvm = null;
                return;
            }

            _lvm = lvm;
            _lvm.PropertyChanged += Lvm_PropertyChanged;
        }

        private void Lvm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("Changed: " + e.PropertyName);
        }

        private Brush TryGettingAmbientLightColor()
        {
            var ambientLight = _lvm?.ObjectManager.GetObjectByName("Ambient_Light");
            if (ambientLight == null) return null;

            return new SolidColorBrush(Color.FromRgb((byte)ambientLight.Floats[0], (byte)ambientLight.Floats[1], (byte)ambientLight.Floats[2]));
        }

        protected virtual void OnSceneUpdated()
        {
            Background = TryGettingAmbientLightColor() ?? Brushes.White;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            viewport.CameraController.MoveSensitivity = 30;
            base.OnRender(drawingContext);
        }

        void viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var hitResult = GetHitTestResult(e.GetPosition(viewport));

                var levelViewModel = (LevelViewModel)DataContext;
                var worldNode = levelViewModel.WorldNode;

                /*if (!worldNode.IsExpanded)
                {
                    worldNode.IsExpanded = true;
                }*/

                WorldElementTreeViewModel selectedElement = null;

                if (worldNode == null)
                {
                    ElementSelected(null);
                    return;
                }

                var vod = levelViewModel.ObjectManager.HitTest(hitResult);

                if (vod != null)
                {
                    ObjectSelected(vod);
                    return;
                }

                for (var i = 0; i < worldNode.Children.Count; i++)
                {
                    if (levelViewModel.Scene[i + 2] == hitResult)
                    {
                        selectedElement = (WorldElementTreeViewModel)worldNode.Children[i];
                        break;
                    }
                }

                ElementSelected(selectedElement);
            }
        }

        // VisualObjectData _selectedObject;
        // WorldElementTreeViewModel _selectedElement;
        private void ElementSelected(WorldElementTreeViewModel ele)
        {
            if (_lvm != null)
            {
                _lvm.SelectedObject = null;
                _lvm.SelectedElement = ele;
            }

            // Expand after values have changed
            if (!editorExpander.IsExpanded)
            {
                editorExpander.IsExpanded = true;
            }
        }
        private void ObjectSelected(VisualObjectData obj)
        {
            if (_lvm != null)
            {
                _lvm.SelectedElement = null;
                _lvm.SelectedObject = obj;
            }
            
            // Expand after values have changed
            if (!editorExpander.IsExpanded)
            {
                editorExpander.IsExpanded = true;
            }
        }

        ModelVisual3D GetHitTestResult(Point location)
        {
            var result = VisualTreeHelper.HitTest(viewport, location);
            if (result != null && result.VisualHit is ModelVisual3D)
            {
                var visual = (ModelVisual3D)result.VisualHit;
                return visual;
            }

            return null;
        }
    }
}
