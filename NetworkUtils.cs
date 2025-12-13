using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GamepadMpcController
{
    public static class NetworkUtils
    {
        public static string GetLocalIPAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // On ignore les interfaces désactivées ou virtuelles
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // On récupère les IP associées
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // On évite les IP 169.xxx (APIPA)
                        string ip = addr.Address.ToString();
                        if (!ip.StartsWith("169.254"))
                            return ip;
                    }
                }
            }

            // fallback
            return "127.0.0.1";
        }
    }
}
