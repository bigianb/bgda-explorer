﻿/*  Copyright (C) 2012 Ian Brown

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
using WorldExplorer.Tools3D;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for SkeletonView.xaml
    /// </summary>
    public partial class SkeletonView : UserControl
    {
        public SkeletonView()
        {
            InitializeComponent();

            DataContextChanged += new DependencyPropertyChangedEventHandler(SkeletonView_DataContextChanged);
        }

        void SkeletonView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var model = DataContext as SkeletonViewModel;

            if (model == null)
                return;
        }
    }
}
