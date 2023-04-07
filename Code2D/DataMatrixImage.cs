using System;
using System.Windows.Media.Imaging;
using ZXing;

namespace Onset_Serialization.Code2D
{
    public class DataMatrixImage
    {
        public static BitmapImage GetDataMatrixCodeImage(string data)
        {
            BitmapImage image = new BitmapImage();

            try
            {
                var writer = new BarcodeWriter()
                {
                    Format = BarcodeFormat.DATA_MATRIX
                };
                System.Drawing.Bitmap bitmap = writer.Write(data);
                var ms = new System.IO.MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                image.BeginInit();
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                image.StreamSource = ms;
                image.EndInit();
            }
            catch
            {
                throw new Exception("Barcode generated error!");
            }

            return image;
        }
    }
}
