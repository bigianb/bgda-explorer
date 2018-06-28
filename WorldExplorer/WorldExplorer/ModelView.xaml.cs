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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for ModelView.xaml
    /// </summary>
    public partial class ModelView : UserControl
    {
        public ModelView()
        {
            InitializeComponent();

            DataContextChanged += new DependencyPropertyChangedEventHandler(ModelView_DataContextChanged);
        }

        void ModelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var model = DataContext as ModelViewModel;

            if (model == null)
                return;
        }

        private void normalsBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            var model = DataContext as ModelViewModel;

            // Force Update
            model.VifModel = model.VifModel;
        }
    }
}
