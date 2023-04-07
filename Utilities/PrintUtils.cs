using Onset_Serialization.Data;
using Onset_Serialization.Labels;
using Onset_Serialization.Models;
using System;
using System.Windows;

namespace Onset_Serialization.Utilities
{
    public class PrintUtils
    {
        private static T GetLabelObject<T>(string className) where T : class
        {
            string nameSpace = typeof(Carton_Label).Namespace;
            Type type = Type.GetType($"{nameSpace}.{className}", false);

            if (type == null)
                return null;

            var obj = Activator.CreateInstance(type) as T;
            return obj;
        }

        public static void PrintLabel(string labelName, LabelInfo info)
        {
            Object labelObj = GetLabelObject<Object>(labelName);
            var label = labelObj as IPrintable;
            if (label == null)
                throw new NotImplementedException($"`{labelName}` has not implemented IPrintable!");

            try
            {
                label.Print(info);
            }
            catch (Exception ex) { throw ex; }
            finally
            {
                if (label is Window)
                {
                    ((Window)label).Close();
                }
            }
        }

        public static LabelInfo GetLabelInfo(string labelName, SerialRouting route)
        {
            LabelInfo labelInfo = new LabelInfo();
            switch (labelName)
            {
                case nameof(MX1101_Box_Label):
                    labelInfo.ProductName = route.SerialData.Product.Name;
                    break;
            }
            labelInfo.SerialNumber = route.SerialNumber;
            return labelInfo;
        }
    }
}
