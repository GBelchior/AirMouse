using System;
using System.Collections.Generic;
using System.Text;

namespace AirMouse.Shared.Models
{
    public class ServerInfo
    {
        public string ServerName { get; set; }
        public string ServerAddress { get; set; }
        public DateTime LastSeen { get; set; }

        public ServerInfo()
        {
            LastSeen = DateTime.Now;
        }

        public bool IsDated(TimeSpan expirationTime)
        {
            return (DateTime.Now - LastSeen) > expirationTime;
        }

        public override string ToString()
        {
            return ServerName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ServerInfo)) return false;
            return (obj as ServerInfo).ServerName.Equals(ServerName) && (obj as ServerInfo).ServerAddress.Equals(ServerAddress);
        }

        public override int GetHashCode()
        {
            return $"{ServerName}|{ServerAddress}".GetHashCode();
        }
    }
}
