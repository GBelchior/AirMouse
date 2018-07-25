using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using AirMouse.Shared.Core;
using Android.Support.Design.Widget;
using AirMouse.Shared.Models;
using Android.Views;
using Android.Hardware;
using Android.Runtime;
using AirMouse.Shared.DTO;
using Java.Lang;

namespace AirMouse.Droid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ISensorEventListener
    {
        private NetworkManager networkManager;
        private ArrayAdapter<ServerInfo> arrayAdapter;

        private SensorManager sensorManager;
        private Sensor gyroscope;

        private SeekBar seekBarSensitivity;
        private Button btnLeft;
        private Button btnRight;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            sensorManager = (SensorManager)GetSystemService(SensorService);
            gyroscope = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
            sensorManager.RegisterListener(this, gyroscope, SensorDelay.Ui);

            seekBarSensitivity = FindViewById<SeekBar>(Resource.Id.seekBarSensitivity);
            btnLeft = FindViewById<Button>(Resource.Id.btnLeft);
            btnRight = FindViewById<Button>(Resource.Id.btnRight);

            networkManager = new NetworkManager();
            networkManager.ServerListUpdated += NetworkManager_ServerListUpdated;
            networkManager.ConnectionChanged += NetworkManager_ConnectionChanged;

            ListView listView = FindViewById<ListView>(Resource.Id.lvwDevices);
            arrayAdapter = new ArrayAdapter<ServerInfo>(this, Android.Resource.Layout.SimpleListItem1);
            listView.Adapter = arrayAdapter;
            listView.ItemClick += lvwDevices_ItemClick;

            networkManager.Start();
        }

        private void NetworkManager_ConnectionChanged(object sender, System.EventArgs e)
        {
            RunOnUiThread(() =>
            {
                TextView connectionStatus = FindViewById<TextView>(Resource.Id.txtConnection);

                if (networkManager.Connected)
                {
                    arrayAdapter.Clear();
                    arrayAdapter.NotifyDataSetChanged();

                    connectionStatus.Text = $"Connected to {networkManager.CurrentConnectedServer}";
                }
                else
                {
                    connectionStatus.Text = "Not connected";
                    networkManager.Start();
                }
            });
        }

        private void NetworkManager_ServerListUpdated(object sender, System.EventArgs e)
        {
            RunOnUiThread(() =>
            {
                arrayAdapter.Clear();
                arrayAdapter.AddAll(networkManager.CurrentServers);
                arrayAdapter.NotifyDataSetChanged();
            });
        }

        private void lvwDevices_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ServerInfo clickedServer = networkManager.CurrentServers[e.Position];
            networkManager.Connect(clickedServer);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {

        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (!networkManager.Connected) return;
            if (e.Sensor.Type != SensorType.Gyroscope) return;

            float normalizedX = -e.Values[2];
            float normalizedY = -e.Values[0];
            if (Math.Abs(normalizedX) < 0.01) normalizedX = 0;
            if (Math.Abs(normalizedY) < 0.01) normalizedY = 0;

            normalizedX *= seekBarSensitivity.Progress;
            normalizedY *= seekBarSensitivity.Progress;

            ClientInputDTO clientInput = new ClientInputDTO
            {
                MouseRelativeX = normalizedX,
                MouseRelativeY = normalizedY,

                MouseLeftButtonPressed = btnLeft.Pressed,
                MouseRightButtonPressed = btnRight.Pressed
            };

            networkManager.Send(clientInput);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                sensorManager.UnregisterListener(this);
            }
            else
            {
                sensorManager.RegisterListener(this, gyroscope, SensorDelay.Ui);
            }

            base.OnWindowFocusChanged(hasFocus);
        }
    }
}

