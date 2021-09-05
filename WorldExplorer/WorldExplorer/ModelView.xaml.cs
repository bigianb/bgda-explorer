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

using System.Windows;
using System.Windows.Input;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for ModelView.xaml
    /// </summary>
    public partial class ModelView
    {
        private ModelViewModel ViewModel => (ModelViewModel)DataContext;

        public ModelView()
        {
            InitializeComponent();

            viewport.KeyDown += Viewport_KeyDown;
        }

        private void Viewport_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.G:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        ViewModel.ShowExportForPosedModel();
                    }

                    break;
            }
        }

        private void normalsBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ModelViewModel vm)
            {
                // Force Update
                vm.VifModel = vm.VifModel;
            }
        }
    }
}