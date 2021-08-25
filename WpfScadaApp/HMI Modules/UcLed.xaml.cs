using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfScadaApp
{
    /// <summary>
    /// MyLed.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UcLed : UserControl
    {
        public UcLed()
        {
            InitializeComponent();
        }

        // 의존성 프로퍼티
        public static readonly DependencyProperty CurrStateProperty =
            DependencyProperty.Register("CurrState", typeof(Color), typeof(UcLed), new PropertyMetadata(Color.FromRgb(83, 86, 90)));
        
        // CurrState에 받아올 데이터의 type, UcLed의 값, 값이 없을 때의 기본 색상 설정

        public Color CurrState
        {
            get { return (Color)GetValue(CurrStateProperty); }
            //속성 값을 받아서 전달
            set { SetValue(CurrStateProperty, value); }
        }
    }
}