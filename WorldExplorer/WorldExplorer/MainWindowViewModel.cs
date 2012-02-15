using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace WorldExplorer
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel(string dataPath)
        {
            _worlds = new ReadOnlyCollection<WorldTreeViewModel>(new []{new WorldTreeViewModel(new World(dataPath, "Cellar1"))});
        }

        private ReadOnlyCollection<WorldTreeViewModel> _worlds;

        public ReadOnlyCollection<WorldTreeViewModel> Children
        {
            get { return _worlds; }
        }
    }
}
