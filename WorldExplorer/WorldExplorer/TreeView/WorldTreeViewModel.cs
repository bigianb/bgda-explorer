using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    public class WorldTreeViewModel : TreeViewItemViewModel
    {
        readonly World _world;

        public WorldTreeViewModel(World world) 
            : base(null, true)
        {
            _world = world;
        }

        public string WorldName
        {
            get { return _world.Name; }
        }

        protected override void LoadChildren()
        {
            _world.Load();
            base.Children.Add(new GobTreeViewModel(this, _world.WorldGob));
        }
    }

    /// <summary>
    /// A simple model that just displays a text label.
    /// </summary>
    public class TextTreeViewModel : TreeViewItemViewModel
    {
        public TextTreeViewModel(TreeViewItemViewModel parent, string text) : base (parent, false)
        {
            _text = text;
        }

        private string _text;

        public string Text
        {
            get { return _text; }
        }
    }

    /// <summary>
    /// A simple model that displays a GOB file.
    /// </summary>
    public class GobTreeViewModel : TreeViewItemViewModel
    {
        public GobTreeViewModel(TreeViewItemViewModel parent, GobFile gobFile)
            : base(parent, true)
        {
            _gobFile = gobFile;
        }

        private GobFile _gobFile;

        public string Text
        {
            get { return _gobFile.Filename; }
        }

        protected override void LoadChildren()
        {
            foreach (var entry in _gobFile.Directory) {
                base.Children.Add(new TextTreeViewModel(this, entry.Key));
            }
        }
    }
}
