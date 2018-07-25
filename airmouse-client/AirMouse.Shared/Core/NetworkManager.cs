using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using AirMouse.Shared.DTO;
using Newtonsoft.Json;
using System.Linq;
using AirMouse.Shared.Models;
using System.Threading.Tasks;

namespace AirMouse.Shared.Core
{
    public class NetworkManager : IDisposable
    {
        private static readonly int ServerPortTCP = 23111;
        private static readonly int ServerPortUDP = 23112;

        private TcpClient tcpClient;
        private UdpClient udpClient;
        private bool listening;

        public event EventHandler ServerListUpdated;
        public event EventHandler ConnectionChanged;

        public List<ServerInfo> CurrentServers { get; }
        public ServerInfo CurrentConnectedServer { get; private set; }

        private bool connected = false;
        public bool Connected
        {
            get => connected;
            set
            {
                if (value == connected) return;

                connected = value;
                ConnectionChanged?.Invoke(this, new EventArgs());
            }
        }

        public NetworkManager()
        {
            CurrentServers = new List<ServerInfo>();
            udpClient = new UdpClient(ServerPortUDP, AddressFamily.InterNetwork);
        }

        public void Start()
        {
            if (!listening)
                Listen();
        }

        public void Stop()
        {
            CurrentServers.Clear();
            listening = false;
        }

        public bool Connect(ServerInfo server)
        {
            Stop();

            try
            {
                tcpClient = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
                tcpClient.Connect(server.ServerAddress, ServerPortTCP);

                CurrentConnectedServer = server;
                Connected = true;

                KeepConnected();

                return true;
            }
            catch
            {
                Start();
                return false;
            }
        }

        public void Disconnect()
        {
            if (!Connected) return;

            tcpClient.Close();
            tcpClient.Dispose();
            tcpClient = null;

            Connected = false;
            CurrentConnectedServer = null;
        }

        public void Send(ClientInputDTO data)
        {
            if (!Connected) throw new InvalidOperationException("Manager must be connected to a client before sending data");

            byte[] dataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            udpClient.Send(dataBytes, dataBytes.Length, CurrentConnectedServer.ServerAddress, ServerPortUDP);
        }

        private async void KeepConnected()
        {
            byte[] sendBuffer = Encoding.UTF8.GetBytes("\r");
            byte[] receiveBuffer = new byte[8];

            try
            {
                await Task.Run(() =>
                {
                    while (true)
                    {
                        tcpClient.Client.Send(sendBuffer);
                        tcpClient.Client.Receive(receiveBuffer);

                        string receiveMessage = Encoding.UTF8.GetString(receiveBuffer);
                        if (receiveMessage.Trim('\0') != "OK") throw new Exception();

                        Task.Delay(5000);
                    }
                });
            }
            catch
            {
                Disconnect();
            }
        }

        private async void Listen()
        {
            listening = true;

            Task t = Task.Run(async () =>
            {
                while (listening)
                {
                    int count = CurrentServers.Count;
                    ClearOldServers();

                    if (count != CurrentServers.Count)
                    {
                        ServerListUpdated?.Invoke(this, new EventArgs());
                    }

                    await Task.Delay(10000);
                }
            });

            while (listening)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                ServerBroadcastDTO serverMessage;

                try
                {
                    string resultString = Encoding.UTF8.GetString(result.Buffer);
                    serverMessage = JsonConvert.DeserializeObject<ServerBroadcastDTO>(resultString);
                    ServerInfo info = new ServerInfo
                    {
                        ServerName = serverMessage.ServerName,
                        ServerAddress = result.RemoteEndPoint.Address.ToString()
                    };

                    if (!listening) return;

                    AddOrUpdateServer(info);

                    ServerListUpdated?.Invoke(this, new EventArgs());
                }
                catch { }
            }
        }

        private void AddOrUpdateServer(ServerInfo server)
        {
            DateTime now = DateTime.Now;

            if (CurrentServers.Contains(server))
            {
                CurrentServers.Find(s => s.Equals(server)).LastSeen = server.LastSeen;
            }
            else
            {
                CurrentServers.Add(server);
            }
        }

        private void ClearOldServers()
        {
            CurrentServers.RemoveAll(s => s.IsDated(TimeSpan.FromSeconds(10)));
        }

        public void Dispose()
        {
            Stop();

            udpClient.Close();
            udpClient.Dispose();

            tcpClient.Close();
            tcpClient.Dispose();
        }
    }
}
