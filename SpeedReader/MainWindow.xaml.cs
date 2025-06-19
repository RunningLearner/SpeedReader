using SpeedReader.Views;

using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpeedReader;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // DWM 윈도우 속성 중 캡션(타이틀바) 색 변경용
    private enum DWMWINDOWATTRIBUTE : uint
    {
        DWMWA_CAPTION_COLOR = 35,
        DWMWA_TEXT_COLOR = 36,
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hWnd,
        DWMWINDOWATTRIBUTE attribute,
        ref uint pvAttribute,
        uint cbAttribute);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// “단어별 읽기” 버튼 클릭 이벤트 핸들러
    /// </summary>
    private void OpenWordWindow_Click(object sender, RoutedEventArgs e)
    {
        // 입력된 텍스트 가져오기
        var text = InputTextBox.Text;

        // WordDisplayWindow에 텍스트 전달 후 표시
        var win = new WordDisplayWindow(text);
        win.Show();
    }

    /// <summary>
    /// 윈도우 로드 후 DWM API 호출로 타이틀바 배경/글자 색을 변경합니다.
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // 1) 캡션 바 배경색 (ARGB → 0xAARRGGBB)
        var bg = Color.FromRgb(0x1E, 0x1E, 0x1E);  // 원하는 다크 그레이
        uint color = (uint)(bg.R << 16 | bg.G << 8 | bg.B);
        DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, ref color, sizeof(uint));

        // 2) 캡션 바 글자색 (흰색)
        var fg = Colors.White;
        uint textColor = (uint)(fg.R << 16 | fg.G << 8 | fg.B);
        DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR, ref textColor, sizeof(uint));
    }
}