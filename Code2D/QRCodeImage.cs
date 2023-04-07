using QRCoder;
using System;
using System.Windows.Media.Imaging;

namespace Generac_Kodiak_Serialization.Code2D
{
    public class QRCodeImage
    {
        public static BitmapImage GetQRCodeImage(string data)
        {
            BitmapImage image = new BitmapImage();

            try
            {
                QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
                QRCodeData qrdata = qRCodeGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                QRCode qrcode = new QRCode(qrdata);
                System.Drawing.Bitmap qrimage = qrcode.GetGraphic(32);
                var ms = new System.IO.MemoryStream();
                qrimage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                image.BeginInit();
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                image.StreamSource = ms;
                image.EndInit();
            }
            catch
            {
                throw new Exception("QRCode generated error!");
            }

            return image;
        }
    }
}
