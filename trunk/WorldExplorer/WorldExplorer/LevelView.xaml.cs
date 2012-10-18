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

            DataContextChanged += new DependencyPropertyChangedEventHandler(ModelView_DataContextChanged);

            viewport.MouseUp += new MouseButtonEventHandler(viewport_MouseUp);
        }

        void viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var hitResult = GetHitTestResult(e.GetPosition(viewport));

                var levelViewModel = (LevelViewModel) DataContext;
                var worldNode = levelViewModel.WorldNode;

                if (!worldNode.IsExpanded)
                {
                    worldNode.IsExpanded = true;
                }

                for (int i = 2; i < levelViewModel.Scene.Count; i++)
                {
                    if (levelViewModel.Scene[i] == hitResult)
                    {
                        worldNode.Children[i-2].IsSelected = true;
                        break;
                    }
                }
            }
        }

        void ModelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var model = DataContext as LevelViewModel;

            if (model == null)
                return;
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
