using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WpfScadaApp;

namespace WpfScadaApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string serverIpNum = "192.168.0.8";  // 윈도우(MQTT Broker, SQLServer) 아이피
        private string clientId = "SCADA_system";
        private string factoryId = "Kasan01";            //  Kasan01/4001/  kasan01/4002/ 
        private string motorAddr = "4002";
        private string tankAddr = "4001";

        private string connectionString = string.Empty;  // SQLServer 연결문자열
        private MqttClient client;                       // MQTT 접속을 객체

        public MainWindow()
        {
            InitializeComponent();
            App.LOGGER.Info("SCADA Strat");  // 시작 로그
            InitAllCustomComponnet();
        }
        // SCADA 시스템용 커스텀 초기화 메서드
        private void InitAllCustomComponnet()
        {
            LblStatus.Content = string.Empty;
            // IPAddress serverAddress = IPAddress.Parse(serverIpNum);
            client = new MqttClient(serverIpNum);
            client.MqttMsgPublished += Client_MqttMsgPublished;
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.ConnectionClosed += Client_ConnectionClosed;

            connectionString = "Data Source=localhost;Initial Catalog=HMI_Data;Integrated Security=True";
            // SQL 서버 연결
        }
        // MQTT 서버와 접속이 끊어졌을때 이벤트 처리
        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                LblStatus.Content = "모니터링 종료!";
            }));

        }
        // MQTT에서 메세지를 구독하면 이벤트처리(*****)
        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {                
                // 대리자에게 대신 UI Thread에 속한 컨트롤에 일처리 호출
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate 
                {
                    var message = Encoding.UTF8.GetString(e.Message); //e.Message(byte[]) ==> string 변환
                    LblStatus.Content = message;

                    // JSON 넘어온 데이터를 확인 후 내부 SCADA 작업
                    //"dev_addr" : "4001",
                    //"currtime" : "2021-08-26 11:05:30 ",
                    //"code" : "red",
                    //"value" : "1"
                    var currData = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);
                    Debug.WriteLine(currData);

                    if(currData["dev_addr"]=="4001" ) // Tank에서 데이터 수신
                    {
                        if (currData["code"] == "red" && currData["value"] == "1")  // 경고등 ON
                        {
                            LedAlarm.CurrState = Color.FromRgb(255, 0, 0);
                            MessageBox.Show("비상 상황! : Fuel Tank 모터를 구동하십시오!");
                        }
                        else if (currData["code"] == "green" && currData["value"] == "1") // 정상 동작 ON\
                        {
                            LedAlarm.CurrState = Color.FromRgb(0, 255, 0);
                        }
                        else if (currData["value"] == "0")
                        {
                            LedAlarm.CurrState = Color.FromRgb(80, 80, 80);
                        }
                    }
                    InsertData(currData); // HMI_Data/Dassan01_Device 테이블에 입력
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                App.LOGGER.Info($"예외발생, Client_MqttMsgPublishReceived : [{ex.Message}]");
            }
        }

        // SQL SEVER 테이블 입력처리
        private void InsertData(Dictionary<string, string> currData)
        {
            using (var conn = new SqlConnection(connectionString))  // close 자동
            {
                string insertQuery = "TEST";
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(insertQuery, conn);
                    if(cmd.ExecuteNonQuery() == 1) // 전송 성공
                    {
                        App.LOGGER.Info("IoT 데이터 입력 성공!");
                    }
                    else
                    {
                        App.LOGGER.Info($"오류 발생, InsertData 데이터 입력 실패 : [{insertQuery}]");
                    }
                }
                catch (Exception ex)
                {
                    App.LOGGER.Info($"예외 발생, InsertData : [{ex.Message}]");
                }
            }
        }

        // MQTT 에서 메세지를 발행(한뒤) 이벤트 처리
        private void Client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {

        }



        // 모니터링 시작 버튼 클릭처리
        private void BtnMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                    App.LOGGER.Info("모니터링 종료 : BtnMonitoring_Click");
                    BtnMonitoring.Content = "Start Mornitoring";
                }
                else
                {
                    client.Connect(clientId);// MQTT에 ID로 만 접속되도록 설정
                                             // 구독하면서 메시지 모니터링 시작
                    client.Subscribe(new string[] { $"{factoryId}/#" },
                                     new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

                    LblStatus.Content = "모니터링 시작!";
                    BtnMonitoring.Content = "Stop Mornitoring";
                    App.LOGGER.Info("모니터링 시작 : BtnMonitoring_Click");
                }
            }
            catch (Exception ex)
            {
                App.LOGGER.Info($"예외발생 : BtnMonitoring_Click : [{ex.Message}]");
            }
        }

        // 위급시 모터 동작처리
        private void BtnMotor_CustomClick(object sender, RoutedEventArgs e)
        {
            if (client.IsConnected)
            {
                var currtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string pubData = "{ " +
                                 "   \"dev_addr\" : \"4002\", " +
                                 $"   \"currtime\" : \"{currtime}\" , " +
                                 "   \"code\" : \"motor\", " +
                                 "   \"value\" : \"1\" " +
                                 "}";

                client.Publish($"{factoryId}/4002", Encoding.UTF8.GetBytes(pubData), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                //==> byte  형태로 변환하여 전송
                LblStatus.Content = pubData;

                App.LOGGER.Warn("위급처리 : FuelTank정지할 수 있습니다!");
                MessageBox.Show("위급처리!");
            }
            else
            {
                MessageBox.Show("모니터링에 접속하지 않았습니다. \n 먼저 접속을 시도하세요.");
            }
        }

        // 메인 윈도우 닫히기 전 처리
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client.IsConnected) client.Disconnect(); // 종료 시 
            // 리소스 해제
            App.LOGGER.Info("SCADA 프로그램 종료!");
        }
    }
}