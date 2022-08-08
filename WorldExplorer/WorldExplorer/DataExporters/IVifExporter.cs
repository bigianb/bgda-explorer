using JetBlackEngineLib.Data.Animation;
using JetBlackEngineLib.Data.Models;
using System.Windows.Media.Imaging;

namespace WorldExplorer.DataExporters;

public interface IVifExporter
{
    void SaveToFile(string savePath, Model model, WriteableBitmap? texture, AnimData? pose = null, int frame = 1,
        double scale = 1.0);
}