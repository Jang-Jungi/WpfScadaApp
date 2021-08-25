using System.Windows;
using System.Windows.Controls;

namespace WpfScadaApp
{
    public class CustomEventCtrlArgs : RoutedEventArgs
    {
        public CustomEventCtrlArgs(RoutedEvent routedEvent, object source)
            : base(routedEvent, source)
        { }
    }

    /// <summary>
    /// MyButton.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UcButton : UserControl
    {
        public static readonly RoutedEvent CustomClickEvent =
            EventManager.RegisterRoutedEvent("CustomClick", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(UcButton));

        public event RoutedEventHandler CustomClick
        {
            add { AddHandler(CustomClickEvent, value); }
            remove { RemoveHandler(CustomClickEvent, value); }
        }

        public UcButton()
        {
            InitializeComponent();
        }

        private void BtnClick_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new CustomEventCtrlArgs(UcButton.CustomClickEvent, sender));
        }
    }
}