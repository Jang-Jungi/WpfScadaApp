using System.Windows;

namespace WpfScadaApp
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        // 로그 설정
        public static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();
    }
}
