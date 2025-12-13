using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GamepadMpcController
{
    public class RemoteHttpServer
    {
        private TcpListener listener;
        private bool running;
        public int Port { get; private set; }
        public string LocalIP { get; private set; }

        public RemoteHttpServer()
        {
            LocalIP = NetworkUtils.GetLocalIPAddress();
        }

        public bool Start(int port = 8080)
        {
            if (running)
                return true;

            try
            {
                Port = port;

                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                running = true;

                Task.Run(AcceptLoop);
                return true;
            }
            catch
            {
                running = false;
                listener = null;
                return false;
            }
        }

        private async Task AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch
                {
                    // Stop() déclenche une exception ici, c est normal
                }
            }
        }

        public void Stop()
        {
            running = false;
            if (listener == null)
                return;

            try { listener.Stop(); } catch { }
            finally
            {
                listener = null;
            }
        }

        // --- MAIN HANDLER ---
        private void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 2000;

                string request = ReadHttpRequest(stream);
                if (string.IsNullOrEmpty(request))
                    return;

                // API endpoint
                if (request.Contains("GET /api/"))
                {
                    HandleApi(request);
                    SendOk(stream);
                    return;
                }

                // Serve embedded HTML from resources
                string html = Encoding.UTF8.GetString(Properties.Resources.webui);
                SendHtml(stream, html);
            }
        }

        // --- READ HTTP REQUEST FULLY ---
        private string ReadHttpRequest(NetworkStream stream)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            int read;
            try
            {
                do
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                    // end of headers detected
                    if (sb.ToString().Contains("\r\n\r\n"))
                        break;

                } while (read > 0);

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        // --- SEND HTML RESPONSE ---
        private void SendHtml(NetworkStream stream, string html)
        {
            string header =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                "Content-Length: " + Encoding.UTF8.GetByteCount(html) + "\r\n" +
                "Connection: close\r\n\r\n";

            byte[] response = Encoding.UTF8.GetBytes(header + html);
            stream.Write(response, 0, response.Length);
        }

        // --- SEND OK FOR API ---
        private void SendOk(NetworkStream stream)
        {
            string header =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Length: 0\r\n" +
                "Connection: close\r\n\r\n";

            byte[] bytes = Encoding.UTF8.GetBytes(header);
            stream.Write(bytes, 0, bytes.Length);
        }

        // --- API ROUTING ---
        private void HandleApi(string request)
        {
            if (Program.FormRef == null)
                return;

            if (request.Contains("/api/playpause"))
                Program.FormRef.PlayPauseRemote();

            else if (request.Contains("/api/next"))
                Program.FormRef.NextRemote();

            else if (request.Contains("/api/previous"))
                Program.FormRef.PreviousRemote();

            else if (request.Contains("/api/seekforward"))
                Program.FormRef.SeekForwardRemote();

            else if (request.Contains("/api/seekbackward"))
                Program.FormRef.SeekBackwardRemote();

            else if (request.Contains("/api/volup"))
                Program.FormRef.VolumeUpRemote();

            else if (request.Contains("/api/voldown"))
                Program.FormRef.VolumeDownRemote();

            else if (request.Contains("/api/fullscreen"))
                Program.FormRef.FullscreenRemote();

            else if (request.Contains("/api/stop"))
                Program.FormRef.StopRemote();
        }
    }
}
