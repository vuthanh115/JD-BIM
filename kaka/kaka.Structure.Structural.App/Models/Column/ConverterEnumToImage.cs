using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using kaka.Structure.Structural.App.Models.Column; // Đảm bảo namespace này chứa EnumDistributionType

namespace kaka.Structure.Structural.App.Models.Column // Namespace phải khớp với nơi bạn đặt file
{
    public class ConverterEnumToImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EnumDistributionType distType)
            {
                string imageName = null;

                // Ánh xạ Enum sang tên file ảnh thực tế trong folder Resources/Images
                // (Tôi dựa theo tên file trong ảnh chụp màn hình của bạn)
                switch (distType)
                {
                    case EnumDistributionType.L1:
                        imageName = "l1.png"; 
                        break;
                    case EnumDistributionType.L1_L2: // Kiểm tra lại tên chính xác trong Enum của bạn
                        imageName = "l1l2.png";
                        break;
                    case EnumDistributionType.L1_L2_L1:
                        imageName = "l1l2l1.png";
                        break;
                    // Thêm các trường hợp khác nếu có
                    default:
                        return null;
                }

                if (string.IsNullOrEmpty(imageName)) return null;

                // Tạo đường dẫn Pack URI. 
                // QUAN TRỌNG: Thay 'kaka.Structure.Structural.App' bằng Tên Assembly (Project Name) của bạn nếu khác.
                // Dựa vào ảnh, tên project có vẻ là: kaka.Structure.Structural.App
                string uriPath = $"pack://application:,,,/kaka.Structure.Structural.App;component/Resources/Images/Column/{imageName}";

                try
                {
                    // Tạo BitmapImage từ đường dẫn
                    return new BitmapImage(new Uri(uriPath));
                }
                catch (Exception)
                {
                    // Không tìm thấy ảnh hoặc lỗi đường dẫn -> Trả về null
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}