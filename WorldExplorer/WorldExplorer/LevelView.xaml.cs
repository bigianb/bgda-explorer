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
using System.Globalization;
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
                        // TODO: Toggle lighting
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
            //_lvm.PropertyChanged += Lvm_PropertyChanged;
        }


        protected virtual void OnSceneUpdated()
        {
            var ambientLight = _lvm?.ObjectManager.GetObjectByName("Ambient_Light");
            if (ambientLight != null)
            {
                Background = new SolidColorBrush(Color.FromRgb((byte)ambientLight.Floats[0], (byte)ambientLight.Floats[1], (byte)ambientLight.Floats[2]));
            }
            else
            {
                Background = Brushes.White;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            viewport.CameraController.MoveSensitivity = 30;
            base.OnRender(drawingContext);
        }

        public override void EndInit()
        {
            base.EndInit();

        }

        void viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var hitResult = GetHitTestResult(e.GetPosition(viewport));

                var levelViewModel = (LevelViewModel)DataContext;
                var worldNode = levelViewModel.WorldNode;

                if (!worldNode.IsExpanded)
                {
                    worldNode.IsExpanded = true;
                }

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

        VisualObjectData _selectedObject;
        WorldElementTreeViewModel _selectedElement;
        private void ElementSelected(WorldElementTreeViewModel ele)
        {
            if (ele == null)
            {
                ResetValues();

                editor_ElementGrid.Visibility = Visibility.Collapsed;
                editor_ObjectGrid.Visibility = Visibility.Collapsed;

                return;
            }

            _selectedObject = null;
            _selectedElement = ele;

            editor_NameText.Text = ele.Text;
            editor_UseRotFlagsBox.IsChecked = ele.WorldElement.usesRotFlags;
            editor_XYZRotFlagsBox.Text = "0x" + ele.WorldElement.xyzRotFlags.ToString("X4");
            editor_CosBox.Text = ele.WorldElement.cosAlpha.ToString(CultureInfo.InvariantCulture);
            editor_SinBox.Text = ele.WorldElement.sinAlpha.ToString(CultureInfo.InvariantCulture);
            editor_PosXBox.Text = ele.WorldElement.pos.X.ToString(CultureInfo.InvariantCulture);
            editor_PosYBox.Text = ele.WorldElement.pos.Y.ToString(CultureInfo.InvariantCulture);
            editor_PosZBox.Text = ele.WorldElement.pos.Z.ToString(CultureInfo.InvariantCulture);

            editor_ElementGrid.Visibility = Visibility.Visible;
            editor_ObjectGrid.Visibility = Visibility.Collapsed;

            // Expand after values have changed
            if (!editorExpander.IsExpanded)
            {
                editorExpander.IsExpanded = true;
            }
        }
        private void ObjectSelected(VisualObjectData obj)
        {
            if (obj == null)
            {
                ResetValues();

                editor_ElementGrid.Visibility = Visibility.Collapsed;
                editor_ObjectGrid.Visibility = Visibility.Collapsed;

                return;
            }

            _selectedElement = null;
            _selectedObject = obj;

            editor_NameText.Text = "Object";
            editor_Obj_NameBox.Text = obj.ObjectData.Name;
            editor_Obj_I6Box.Text = "0x" + obj.ObjectData.I6.ToString("X4");
            editor_Obj_Float1Box.Text = obj.ObjectData.Floats[0].ToString(CultureInfo.InvariantCulture);
            editor_Obj_Float2Box.Text = obj.ObjectData.Floats[1].ToString(CultureInfo.InvariantCulture);
            editor_Obj_Float3Box.Text = obj.ObjectData.Floats[2].ToString(CultureInfo.InvariantCulture);
            if (obj.ObjectData.Properties != null && obj.ObjectData.Properties.Count > 0)
            {
                editor_Obj_PropertiesBox.Text = string.Join("\n", obj.ObjectData.Properties);
            }
            else
            {
                editor_Obj_PropertiesBox.Text = null;
            }

            editor_ElementGrid.Visibility = Visibility.Collapsed;
            editor_ObjectGrid.Visibility = Visibility.Visible;

            // Expand after values have changed
            if (!editorExpander.IsExpanded)
            {
                editorExpander.IsExpanded = true;
            }
        }

        private void ApplyChangesClicked(object sender, RoutedEventArgs e)
        {
            if (_selectedElement == null && _selectedObject == null)
            {
                return;
            }

            int tempIntValue;
            double tempDoubleValue;
            var levelViewModel = (LevelViewModel)DataContext;

            if (_selectedElement != null)
            {
                _selectedElement.WorldElement.usesRotFlags =
                    editor_UseRotFlagsBox.IsChecked.GetValueOrDefault(); // Uses Rot Flags
                if (GetIntHex(editor_XYZRotFlagsBox.Text, out tempIntValue)) // XYZ Rot Flags
                {
                    _selectedElement.WorldElement.xyzRotFlags = tempIntValue;
                }

                if (GetDouble(editor_CosBox.Text, out tempDoubleValue)) // Cos
                {
                    _selectedElement.WorldElement.cosAlpha = tempDoubleValue;
                }

                if (GetDouble(editor_SinBox.Text, out tempDoubleValue)) // Sin
                {
                    _selectedElement.WorldElement.sinAlpha = tempDoubleValue;
                }

                if (GetDouble(editor_PosXBox.Text, out tempDoubleValue)) // X
                {
                    _selectedElement.WorldElement.pos.X = tempDoubleValue;
                }

                if (GetDouble(editor_PosYBox.Text, out tempDoubleValue)) // Y
                {
                    _selectedElement.WorldElement.pos.Y = tempDoubleValue;
                }

                if (GetDouble(editor_PosZBox.Text, out tempDoubleValue)) // Z
                {
                    _selectedElement.WorldElement.pos.Z = tempDoubleValue;
                }

                levelViewModel.RebuildScene();
            }
            else if (_selectedObject != null)
            {
                _selectedObject.ObjectData.Name = editor_Obj_NameBox.Text; // Name
                if (GetIntHex(editor_Obj_I6Box.Text, out tempIntValue)) // I6
                {
                    _selectedObject.ObjectData.I6 = (short)tempIntValue;
                }

                if (GetDouble(editor_Obj_Float1Box.Text, out tempDoubleValue)) // Float 1
                {
                    _selectedObject.ObjectData.Floats[0] = (float)tempDoubleValue;
                }

                if (GetDouble(editor_Obj_Float2Box.Text, out tempDoubleValue)) // Float 2
                {
                    _selectedObject.ObjectData.Floats[1] = (float)tempDoubleValue;
                }

                if (GetDouble(editor_Obj_Float3Box.Text, out tempDoubleValue)) // Float 3
                {
                    _selectedObject.ObjectData.Floats[2] = (float)tempDoubleValue;
                }

                _selectedObject.ObjectData.Properties.Clear();
                _selectedObject.ObjectData.Properties.AddRange(editor_Obj_PropertiesBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                levelViewModel.ObjectManager.RemoveObjectFromList(_selectedObject);

                var newObject = levelViewModel.ObjectManager.ParseObject(_selectedObject.ObjectData);

                _selectedObject = newObject;

                levelViewModel.RebuildScene();
            }
        }

        void ResetValues()
        {
            _selectedElement = null;

            editor_NameText.Text = "No Element Selected";
            editor_UseRotFlagsBox.IsChecked = false;
            editor_XYZRotFlagsBox.Text =
                editor_CosBox.Text =
                    editor_SinBox.Text =
                        editor_PosXBox.Text =
                            editor_PosYBox.Text =
                                editor_PosZBox.Text = null;

            editor_Obj_NameBox.Text =
                editor_Obj_I6Box.Text =
                    editor_Obj_Float1Box.Text =
                        editor_Obj_Float2Box.Text =
                            editor_Obj_Float3Box.Text =
                                editor_Obj_PropertiesBox.Text = null;
        }

        bool GetDouble(string text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
            value = 0;
            return false;
        }
        bool GetInt(string text, out int value)
        {
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
            value = 0;
            return false;
        }
        bool GetIntHex(string text, out int value)
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(2);
            }
            if (int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
            value = 0;
            return false;
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
