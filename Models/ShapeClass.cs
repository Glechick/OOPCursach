using System.Collections.ObjectModel;

namespace SVMKurs.Models
{
    public class ShapeClass
    {
        public string Name
        {
            get; set;
        }
        public int Id
        {
            get; set;
        }
        public ObservableCollection<ShapeImage> Images
        {
            get; set;
        }

        public ShapeClass()
        {
            Images = new ObservableCollection<ShapeImage>();
        }
    }

    public class ShapeImage
    {
        public string FileName
        {
            get; set;
        }
        public string FilePath
        {
            get; set;
        }
        public string FileHash
        {
            get; set;
        }
        public byte[] ImageBytes
        {
            get; set;
        }
        public double[] Features
        {
            get; set;
        }
        public bool IsProcessed
        {
            get; set;
        }

        public double Feature1 => Features?[0] ?? 0;
        public double Feature2 => Features?[1] ?? 0;
        public double Feature3 => Features?[2] ?? 0;
    }
}