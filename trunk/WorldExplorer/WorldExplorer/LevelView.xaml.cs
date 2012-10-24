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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit;
using WorldExplorer.DataLoaders;
using WorldExplorer.Tools3D;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for LevelView.xaml
    /// </summary>
    public partial class LevelView : UserControl
    {
        public LevelView()
        {
            InitializeComponent();

            viewport.MouseUp += new MouseButtonEventHandler(viewport_MouseUp);
        }

        void viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var hitResult = GetHitTestResult(e.GetPosition(viewport));

                var levelViewModel = (LevelViewModel) DataContext;
                var worldNode = levelViewModel.WorldNode;

                /*if (!worldNode.IsExpanded)
                {
                    worldNode.IsExpanded = true;
                }*/

                WorldElementTreeViewModel selectedElement = null;

                for (int i = 2; i < levelViewModel.Scene.Count; i++)
                {
                    if (levelViewModel.Scene[i] == hitResult)
                    {
                        selectedElement = (WorldElementTreeViewModel) worldNode.Children[i - 2];
                        //worldNode.Children[i-2].IsSelected = true;
                        break;
                    }
                }

                ElementSelected(selectedElement);
            }
        }

        WorldElementTreeViewModel _selectedElement;
        private void ElementSelected(WorldElementTreeViewModel ele)
        {
            if (ele == null)
            {
                // Reset Values
                _selectedElement = null;

                editor_NameText.Text = "No Element Selected";
                editor_UseRotFlagsBox.IsChecked = false;
                editor_XYZRotFlagsBox.Text =
                    editor_CosBox.Text =
                        editor_SinBox.Text =
                            editor_PosXBox.Text =
                                editor_PosYBox.Text =
                                    editor_PosZBox.Text = null;

                return;
            }

            _selectedElement = ele;

            editor_NameText.Text = ele.Text;
            editor_UseRotFlagsBox.IsChecked = ele.WorldElement.usesRotFlags;
            editor_XYZRotFlagsBox.Text = "0x"+ele.WorldElement.xyzRotFlags.ToString("X4");
            editor_CosBox.Text = ele.WorldElement.cosAlpha.ToString(CultureInfo.InvariantCulture);
            editor_SinBox.Text = ele.WorldElement.sinAlpha.ToString(CultureInfo.InvariantCulture);
            editor_PosXBox.Text = ele.WorldElement.pos.X.ToString(CultureInfo.InvariantCulture);
            editor_PosYBox.Text = ele.WorldElement.pos.Y.ToString(CultureInfo.InvariantCulture);
            editor_PosZBox.Text = ele.WorldElement.pos.Z.ToString(CultureInfo.InvariantCulture);

            // Expand after values have changed
            if (!editorExpander.IsExpanded)
                editorExpander.IsExpanded = true;
        }

        private void ApplyChangesClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_selectedElement == null)
                return;

            int tempIntValue;
            double tempDoubleValue;

            _selectedElement.WorldElement.usesRotFlags = 
                editor_UseRotFlagsBox.IsChecked.GetValueOrDefault(); // Uses Rot Flags
            if (GetIntHex(editor_XYZRotFlagsBox.Text, out tempIntValue)) // XYZ Rot Flags
                _selectedElement.WorldElement.xyzRotFlags = tempIntValue;
            if (GetDouble(editor_CosBox.Text, out tempDoubleValue)) // Cos
                _selectedElement.WorldElement.cosAlpha = tempDoubleValue;
            if (GetDouble(editor_SinBox.Text, out tempDoubleValue)) // Sin
                _selectedElement.WorldElement.sinAlpha = tempDoubleValue;
            if (GetDouble(editor_PosXBox.Text, out tempDoubleValue)) // X
                _selectedElement.WorldElement.pos.X = tempDoubleValue;
            if (GetDouble(editor_PosYBox.Text, out tempDoubleValue)) // Y
                _selectedElement.WorldElement.pos.Y = tempDoubleValue;
            if (GetDouble(editor_PosZBox.Text, out tempDoubleValue)) // Z
                _selectedElement.WorldElement.pos.Z = tempDoubleValue;

            var levelViewModel = (LevelViewModel)DataContext;
            levelViewModel.RebuildScene();
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
            HitTestResult result = VisualTreeHelper.HitTest(viewport, location);
            if (result != null && result.VisualHit is ModelVisual3D)
            {
                ModelVisual3D visual = (ModelVisual3D)result.VisualHit;
                return visual;
            }

            return null;
        }
    }
}
