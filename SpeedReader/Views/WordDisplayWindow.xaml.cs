using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpeedReader.Views;
/// <summary>
/// WordDisplayWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class WordDisplayWindow : Window
{
    #region Fields

    /// <summary>단어별로 분할된 문자열 배열</summary>
    private readonly string[] _words;

    /// <summary>현재 표시 중인 단어의 인덱스</summary>
    private int _currentIndex;

    /// <summary>자동 전환 작업 취소용 토큰 소스</summary>
    private CancellationTokenSource? _cts;

    /// <summary>글자 크기 조정 단위</summary>
    private const double FontSizeStep = 2.0;

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
    #endregion

    #region Constructors

    /// <summary>
    /// 생성자: 텍스트를 단어별로 분리하고, Loaded 이벤트를 등록합니다.
    /// </summary>
    /// <param name="text">띄어쓰기로 구분된 원본 텍스트</param>
    public WordDisplayWindow(string text)
    {
        InitializeComponent();

        _words = text
            .Split(new[] { " ", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Loaded += WordDisplayWindow_Loaded;

        // 체크박스, 버튼, 키 이벤트 구독은 InitializeComponent 이후, _words 할당 이후에!
        AutoModeCheckBox.Checked += AutoModeCheckBox_Changed;
        AutoModeCheckBox.Unchecked += AutoModeCheckBox_Changed;

        DecreaseFontButton.Click += DecreaseFontSizeButton_Click;
        IncreaseFontButton.Click += IncreaseFontSizeButton_Click;

        // 수동 모드도 키 입력 가능하도록 포커스 확보
        KeyDown += WordDisplayWindow_KeyDown;
        Focusable = true;
        Focus();

        AutoModeCheckBox.IsChecked = false;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Loaded 이벤트: 표시할 첫 단어 세팅 및 자동 모드가 켜져 있으면 자동 루프 시작
    /// </summary>
    private void WordDisplayWindow_Loaded(object sender, RoutedEventArgs e)
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
        
        _currentIndex = 0;
        UpdateDisplayedWord();

        if (AutoModeCheckBox.IsChecked == true)
            StartAutoLoop();
    }

    /// <summary>
    /// 자동 모드 체크박스 상태가 변경되면 자동 루프 시작/중지
    /// </summary>
    private void AutoModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (AutoModeCheckBox.IsChecked == true)
            StartAutoLoop();
        else
            StopAutoLoop();
    }

    /// <summary>
    /// KeyDown 이벤트: 자동 모드가 꺼져 있을 때 좌/우 키로 단어 이동
    /// </summary>
    private void WordDisplayWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (AutoModeCheckBox.IsChecked == true)
            return;

        if (e.Key == Key.Right && _currentIndex < _words.Length - 1)
        {
            _currentIndex++;
            UpdateDisplayedWord();
        }
        else if (e.Key == Key.Left && _currentIndex > 0)
        {
            _currentIndex--;
            UpdateDisplayedWord();
        }
    }

    /// <summary>
    /// “–” 버튼 클릭: 글자 크기 감소
    /// </summary>
    private void DecreaseFontSizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WordTextBlock.FontSize > FontSizeStep)
            WordTextBlock.FontSize -= FontSizeStep;
    }

    /// <summary>
    /// “+” 버튼 클릭: 글자 크기 증가
    /// </summary>
    private void IncreaseFontSizeButton_Click(object sender, RoutedEventArgs e)
    {
        WordTextBlock.FontSize += FontSizeStep;
    }

    #endregion

    #region Methods

    /// <summary>
    /// 현재 인덱스의 단어를 TextBlock에 표시합니다.
    /// </summary>
    private void UpdateDisplayedWord()
    {
        WordTextBlock.Text =
            (_words.Length > 0 && _currentIndex >= 0 && _currentIndex < _words.Length)
            ? _words[_currentIndex]
            : string.Empty;
    }

    /// <summary>
    /// 자동 전환 루프를 시작합니다. 이전 루프가 있으면 취소 후 새로 실행.
    /// </summary>
    private void StartAutoLoop()
    {
        StopAutoLoop();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && _currentIndex < _words.Length - 1)
            {
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) break;

                Dispatcher.Invoke(() =>
                {
                    _currentIndex++;
                    UpdateDisplayedWord();
                });
            }
        }, token);
    }

    /// <summary>
    /// 자동 전환 루프를 중지하고 토큰을 해제합니다.
    /// </summary>
    private void StopAutoLoop()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    #endregion
}
