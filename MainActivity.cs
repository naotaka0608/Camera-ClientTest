using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Timers;
//using System.Drawing.Imaging;
using Android.Graphics;
using Android.Content.PM;
using Java.Nio;
using Xamarin.Essentials;

namespace ClientTest
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        TcpClient tcpClient;
        NetworkStream stream;
        Timer timer_TcpRcv;
        bool flgTimerRcv;

        bool flgLiveOn;


        EditText editText_IpAddress;
        EditText editText_Port;
        TextView textView_Detect;
        TextView textView_AllTime;
        Button button_TpcOpen;
        Button button_Send_On_Image;
        ToggleButton toggleButton_Detect;
        ImageView imageView_Main;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="savedInstanceState"></param>
        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            editText_IpAddress = FindViewById<EditText>(Resource.Id.editText_ipaddress);
            editText_Port = FindViewById<EditText>(Resource.Id.editText_port);

            textView_Detect = FindViewById<TextView>(Resource.Id.textView_Detect);
            textView_AllTime = FindViewById<TextView>(Resource.Id.textView_AllTime);

            timer_TcpRcv = new Timer();
            timer_TcpRcv.AutoReset = true; 
            timer_TcpRcv.Interval = 10;      
            timer_TcpRcv.Elapsed += new ElapsedEventHandler(OnTimerEvent_Tcp);
            timer_TcpRcv.Enabled = false;

            button_TpcOpen = FindViewById<Button>(Resource.Id.button_TcpOpen);
            button_TpcOpen.Tag = 0;
            button_TpcOpen.Click += ButtonOnClick;

            button_Send_On_Image = FindViewById<Button>(Resource.Id.button_Send_On_Image);
            button_Send_On_Image.Tag = 1;
            button_Send_On_Image.Click += ButtonOnClick;

            toggleButton_Detect = FindViewById<ToggleButton>(Resource.Id.toggleButton_Detect);
            toggleButton_Detect.Enabled = true;

            imageView_Main = FindViewById<ImageView>(Resource.Id.imageView_Main);

            InitMember();
            InitControll();

            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) => {
                Toast.MakeText(this, "例外発生！！", ToastLength.Short).Show();
                e.Handled = true;
            };
        }

        /// <summary>
        /// メンバー変数の初期化
        /// </summary>
        private void InitMember()
        {
            tcpClient = null;
            stream = null;

            flgTimerRcv = false;
            flgLiveOn = false;
        }

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitControll()
        {

            button_TpcOpen.Text = "オープン";
            button_Send_On_Image.Enabled = true;

            textView_AllTime.Text = "Time" + "0000" + " ms";
        }

        /// <summary>
        /// ボタンクリック処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            Button button = (Button)sender;
            int tag = (int)button.Tag;
            switch (tag)
            {
                case 0:
                    if (tcpClient != null)
                    {
                        TcpClose();
                    }
                    else
                    {
                        TcpOpen();
                    }
                    break;

                case 1:
                case 2:
                    if (flgLiveOn)
                    {
                        Send_Off_VideoCapure();
                    }
                    else
                    {
                        Send_On_VideoCapure();
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// TCPオープン
        /// </summary>
        private void TcpOpen()
        {

            //もしオープンしていたら、再度オープンしない
            if (tcpClient != null)
            {
                return;
            }

            if(editText_IpAddress.Text == "")
            {
                return;
            }

            if (editText_Port.Text == "")
            {
                return;
            }

            //テキストボックスからIPアドレスとポートを取得
            IPAddress ipAddress = IPAddress.Parse(editText_IpAddress.Text);
            int port = Int32.Parse(editText_Port.Text);

            //インスタンス生成
            tcpClient = new TcpClient();

            try
            {
                //TCP接続処理
                tcpClient.Connect(ipAddress, port);
                stream = tcpClient.GetStream();
                stream.ReadTimeout = 100000;
                stream.WriteTimeout = 100000;

                // 受信用タイマーをスタートさせる。
                timer_TcpRcv.Start(); 

                button_TpcOpen.Text = "クローズ";
            }
            catch (Exception ex)
            {
                button_TpcOpen.Text = "オープン";
            }

        }

        /// <summary>
        /// TCPクローズ
        /// </summary>
        private void TcpClose()
        {
            // TCPがオープンしていないとき
            if (tcpClient == null) return;

            try
            {
                //TCP切断処理
                timer_TcpRcv.Stop();
                tcpClient.Close();

                button_TpcOpen.Text = "オープン";

                // クローズできたら、tcpClientにnullを入れる
                if (tcpClient != null)
                {
                    tcpClient = null;
                    stream = null;
                }
            }
            catch (Exception ex)
            {
                button_TpcOpen.Text = "クローズ";
            }

        }

        /// <summary>
        /// VideoCapure On
        /// </summary>
        private void Send_On_VideoCapure()
        {
            // TCPがオープンしていないとき
            if (tcpClient == null)
            {
                return;
            }

            string msg = "";

            try
            {
                msg = "1";

                if (toggleButton_Detect.Checked)
                {
                    msg += "1";
                }
                else
                {
                    msg += "0";
                }

                //msgをアスキーに変換
                byte[] sendBytes = Encoding.ASCII.GetBytes(msg);
                // ソケット送信
                stream.Write(sendBytes, 0, sendBytes.GetLength(0));
                button_Send_On_Image.Text = "OFF";
                toggleButton_Detect.Enabled = false;


                flgLiveOn = true;

            }
            catch (Exception ex)
            {
                button_Send_On_Image.Text = "ON";
                toggleButton_Detect.Enabled = true;
                flgLiveOn = false;
            }
        }


        /// <summary>
        /// VideoCapure Off
        /// </summary>
        private void Send_Off_VideoCapure()
        {
            // TCPがオープンしていないとき
            if (tcpClient == null)
            {
                return;
            }
            string msg = "";

            try
            {
                msg = "0";

                if (toggleButton_Detect.Checked)
                {
                    msg += "1";
                }
                else
                {
                    msg += "0";
                }

                //msgをアスキーに変換
                byte[] sendBytes = Encoding.ASCII.GetBytes(msg);
                // ソケット送信
                stream.Write(sendBytes, 0, sendBytes.GetLength(0));

                button_Send_On_Image.Text = "ON";
                toggleButton_Detect.Enabled = true;
                flgLiveOn = false;
            }
            catch (Exception ex)
            {
                button_Send_On_Image.Text = "OFF";
                toggleButton_Detect.Enabled = false;
                flgLiveOn = true;
            }

        }


        /// <summary>
        /// Detect Off
        /// </summary>
        private void Send_On_Detect()
        {
            // TCPがオープンしていないとき
            if (tcpClient == null)
            {
                return;
            }
            string msg = "";

            try
            {
                msg = "2";

                //msgをアスキーに変換
                byte[] sendBytes = Encoding.ASCII.GetBytes(msg);
                // ソケット送信
                stream.Write(sendBytes, 0, sendBytes.GetLength(0));

                button_Send_On_Image.Text = "ON";
                flgLiveOn = false;
            }
            catch (Exception ex)
            {
                button_Send_On_Image.Text = "OFF";
                flgLiveOn = true;
            }

        }


        /// <summary>
        /// 受信処理
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimerEvent_Tcp(object source, ElapsedEventArgs e)
        {
            string msg = "";

            if (tcpClient.Available > 0)
            {

                if (flgTimerRcv) return;

                flgTimerRcv = true;
                try
                {

                    byte[] rcvBytes1 = new byte[4];
                    stream.Read(rcvBytes1, 0, 4); 

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(rcvBytes1);
                    }

                    int bufSize = BitConverter.ToInt32(rcvBytes1, 0);

                    byte[] rcvBytes = new byte[bufSize];

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();

                    int bytesReceived = 0;
                    while (bytesReceived < rcvBytes.Length)
                    {
                        bytesReceived += stream.Read(rcvBytes, bytesReceived, rcvBytes.Length - bytesReceived);
                    }
                    
                    Bitmap bitmap = BitmapFactory.DecodeByteArray(rcvBytes, 0, rcvBytes.Length);

                    imageView_Main.SetImageBitmap(bitmap);

                    textView_AllTime.Text = "Time" + sw.ElapsedMilliseconds.ToString() + " ms";

                    sw.Stop();

                }
                catch (Exception ex)
                {
                }
             
                flgTimerRcv = false;
            }
        }


        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}

    public static class DataOperation
    {
        /// <summary>
        /// 文字列を圧縮しバイナリ列として返します。
        /// </summary>
        public static byte[] CompressFromStr(string message) => Compress(Encoding.UTF8.GetBytes(message));

        /// <summary>
        /// バイナリを圧縮します。
        /// </summary>
        public static byte[] Compress(byte[] src)
        {
            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, CompressionMode.Compress, true/*msは*/))
                {
                    ds.Write(src, 0, src.Length);
                }

                // 圧縮した内容をbyte配列にして取り出す
                ms.Position = 0;
                byte[] comp = new byte[ms.Length];
                ms.Read(comp, 0, comp.Length);
                return comp;
            }
        }

        /// <summary>
        /// 圧縮データを文字列として復元します。
        /// </summary>
        public static string DecompressToStr(byte[] src) => Encoding.UTF8.GetString(Decompress(src));

        /// <summary>
        /// 圧縮済みのバイト列を解凍します。
        /// </summary>
        public static byte[] Decompress(byte[] src)
        {
            using (var ms = new MemoryStream(src))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
            {
                using (var dest = new MemoryStream())
                {
                    ds.CopyTo(dest);

                    dest.Position = 0;
                    byte[] decomp = new byte[dest.Length];
                    dest.Read(decomp, 0, decomp.Length);
                    return decomp;
                }
            }
        }
    }
}
