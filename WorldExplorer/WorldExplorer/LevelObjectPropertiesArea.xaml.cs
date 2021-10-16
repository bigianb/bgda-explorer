using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;
using WorldExplorer.WorldDefs;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for LevelObjectPropertiesArea.xaml
    /// </summary>
    public partial class LevelObjectPropertiesArea : UserControl
    {
        // Using a DependencyProperty as the backing store for SelectedElement.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedElementProperty =
            DependencyProperty.Register("SelectedElement", typeof(WorldElementTreeViewModel),
                typeof(LevelObjectPropertiesArea), new PropertyMetadata(null, SelectedElementChanged));

        // Using a DependencyProperty as the backing store for SelectedObject.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(VisualObjectData), typeof(LevelObjectPropertiesArea),
                new PropertyMetadata(null, SelectedObjectChanged));

        public WorldElementTreeViewModel? SelectedElement
        {
            get => (WorldElementTreeViewModel?)GetValue(SelectedElementProperty);
            set => SetValue(SelectedElementProperty, value);
        }

        public VisualObjectData? SelectedObject
        {
            get => (VisualObjectData?)GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        public LevelObjectPropertiesArea()
        {
            InitializeComponent();
            ElementSelected(null);
        }

        public event EventHandler? ChangesApplied;

        private static void SelectedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LevelObjectPropertiesArea control))
            {
                return;
            }

            control.ElementSelected(e.NewValue as WorldElementTreeViewModel);
        }

        private static void SelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LevelObjectPropertiesArea control))
            {
                return;
            }

            control.ObjectSelected(e.NewValue as VisualObjectData);
        }

        private void ResetValues()
        {
            SelectedElement = null;
            SelectedObject = null;

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

        private void ApplyChangesClicked(object sender, RoutedEventArgs e)
        {
            if (SelectedElement == null && SelectedObject == null)
            {
                return;
            }

            int tempIntValue;
            double tempDoubleValue;
            var levelViewModel = (LevelViewModel)DataContext;

            if (SelectedElement != null)
            {
                SelectedElement.WorldElement.UsesRotFlags =
                    editor_UseRotFlagsBox.IsChecked.GetValueOrDefault(); // Uses Rot Flags
                if (GetIntHex(editor_XYZRotFlagsBox.Text, out tempIntValue)) // XYZ Rot Flags
                {
                    SelectedElement.WorldElement.XyzRotFlags = tempIntValue;
                }

                if (GetDouble(editor_CosBox.Text, out tempDoubleValue)) // Cos
                {
                    SelectedElement.WorldElement.CosAlpha = tempDoubleValue;
                }

                if (GetDouble(editor_SinBox.Text, out tempDoubleValue)) // Sin
                {
                    SelectedElement.WorldElement.SinAlpha = tempDoubleValue;
                }

                if (!GetDouble(editor_PosXBox.Text, out var posX)) // X
                {
                    posX = SelectedElement.WorldElement.Position.X;
                }

                if (GetDouble(editor_PosYBox.Text, out var posY)) // Y
                {
                    posY = SelectedElement.WorldElement.Position.Y;
                }

                if (GetDouble(editor_PosZBox.Text, out var posZ)) // Z
                {
                    posZ = SelectedElement.WorldElement.Position.Z;
                }

                SelectedElement.WorldElement.Position = new Vector3D(posX, posY, posZ);

                levelViewModel.RebuildScene();
            }
            else if (SelectedObject != null)
            {
                // Ensure there's an ObjectData instance before changing it
                SelectedObject.ObjectData ??= ObjectData.Empty;
                
                SelectedObject.ObjectData.Name = editor_Obj_NameBox.Text; // Name
                if (GetIntHex(editor_Obj_I6Box.Text, out tempIntValue)) // I6
                {
                    SelectedObject.ObjectData.I6 = (short)tempIntValue;
                }

                if (GetDouble(editor_Obj_Float1Box.Text, out tempDoubleValue)) // Float 1
                {
                    SelectedObject.ObjectData.Floats[0] = (float)tempDoubleValue;
                }

                if (GetDouble(editor_Obj_Float2Box.Text, out tempDoubleValue)) // Float 2
                {
                    SelectedObject.ObjectData.Floats[1] = (float)tempDoubleValue;
                }

                if (GetDouble(editor_Obj_Float3Box.Text, out tempDoubleValue)) // Float 3
                {
                    SelectedObject.ObjectData.Floats[2] = (float)tempDoubleValue;
                }

                SelectedObject.ObjectData.Properties.Clear();
                SelectedObject.ObjectData.Properties.AddRange(
                    editor_Obj_PropertiesBox.Text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries));

                levelViewModel.ObjectManager.RemoveObjectFromList(SelectedObject);

                var newObject = levelViewModel.ObjectManager.ParseObject(SelectedObject.ObjectData);

                SelectedObject = newObject;

                levelViewModel.RebuildScene();
            }

            ChangesApplied?.Invoke(this, EventArgs.Empty);
        }

        private void ElementSelected(WorldElementTreeViewModel? ele)
        {
            if (ele == null)
            {
                ResetValues();

                editor_ElementGrid.Visibility = Visibility.Collapsed;
                editor_ObjectGrid.Visibility = Visibility.Collapsed;

                return;
            }

            editor_NameText.Text = ele.Label;
            editor_UseRotFlagsBox.IsChecked = ele.WorldElement.UsesRotFlags;
            editor_XYZRotFlagsBox.Text = "0x" + ele.WorldElement.XyzRotFlags.ToString("X4");
            editor_CosBox.Text = ele.WorldElement.CosAlpha.ToString(CultureInfo.InvariantCulture);
            editor_SinBox.Text = ele.WorldElement.SinAlpha.ToString(CultureInfo.InvariantCulture);
            editor_PosXBox.Text = ele.WorldElement.Position.X.ToString(CultureInfo.InvariantCulture);
            editor_PosYBox.Text = ele.WorldElement.Position.Y.ToString(CultureInfo.InvariantCulture);
            editor_PosZBox.Text = ele.WorldElement.Position.Z.ToString(CultureInfo.InvariantCulture);

            editor_ElementGrid.Visibility = Visibility.Visible;
            editor_ObjectGrid.Visibility = Visibility.Collapsed;
        }

        private void ObjectSelected(VisualObjectData? obj)
        {
            if (obj == null)
            {
                ResetValues();

                editor_ElementGrid.Visibility = Visibility.Collapsed;
                editor_ObjectGrid.Visibility = Visibility.Collapsed;

                return;
            }
            
            // Ensure there's a value set before reading from it
            obj.ObjectData ??= ObjectData.Empty;

            editor_NameText.Text = "Object";
            editor_Obj_NameBox.Text = obj.ObjectData.Name;
            editor_Obj_I6Box.Text = "0x" + obj.ObjectData.I6.ToString("X4");
            editor_Obj_Float1Box.Text = obj.ObjectData.Floats[0].ToString(CultureInfo.InvariantCulture);
            editor_Obj_Float2Box.Text = obj.ObjectData.Floats[1].ToString(CultureInfo.InvariantCulture);
            editor_Obj_Float3Box.Text = obj.ObjectData.Floats[2].ToString(CultureInfo.InvariantCulture);
            editor_Obj_PropertiesBox.Text = obj.ObjectData.Properties.Count > 0
                ? string.Join("\n", obj.ObjectData.Properties)
                : null;

            editor_ElementGrid.Visibility = Visibility.Collapsed;
            editor_ObjectGrid.Visibility = Visibility.Visible;
        }

        private bool GetDouble(string text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private bool GetInt(string text, out int value)
        {
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private bool GetIntHex(string text, out int value)
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
    }
}