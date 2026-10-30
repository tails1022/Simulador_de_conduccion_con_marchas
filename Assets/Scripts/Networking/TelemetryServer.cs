using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GyroPilot.Networking
{
    public class TelemetryServer : MonoBehaviour
    {
        public static TelemetryServer Instance { get; private set; }

        [Header("Configuracin del Servidor")]
        [SerializeField] private int initialPort = 8090;
        public int ActivePort { get; private set; } = 8090;
        public string ServerUrl { get; private set; } = "";
        public List<string> LocalIPs { get; private set; } = new List<string>();
        public int SelectedIPIndex { get; private set; } = 0;
        public event Action OnServerUrlChanged;

        [Header("Telemetra Actual")]
        public VehicleInputData CurrentInput;
        public bool IsClientConnected => clientConnectedCount > 0 && (Time.time - lastInputTime) < 1.5f;
        public string LastActivityLog { get; private set; } = "Esperando primera conexin...";

        private TcpListener tcpListener;
        private Thread listenerThread;
        private volatile bool isRunning = false;
        private int clientConnectedCount = 0;
        private float lastInputTime = -10f;
        private readonly object inputLock = new object();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            // Si este script está adjunto a un GameObject con otros componentes de escena (GameManager, HUD, etc.)
            // aislarlo en su propio GameObject independiente para que no arrastre la UI entre cambios de escena.
            if (transform.parent != null || GetComponent<GyroPilot.UI.SimulatorHUD>() != null || GetComponent<GyroPilot.Vehicle.CarSelectionManager>() != null)
            {
                GameObject dedicatedHost = new GameObject("[TelemetryServer_Dedicated]");
                var dedicatedServer = dedicatedHost.AddComponent<TelemetryServer>();
                dedicatedServer.initialPort = this.initialPort;
                DontDestroyOnLoad(dedicatedHost);
                Instance = dedicatedServer;
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            FindLocalIPAddresses();
            StartServer();
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }

        public void SetCustomUrl(string customUrl)
        {
            if (string.IsNullOrEmpty(customUrl)) return;
            if (!customUrl.EndsWith("/")) customUrl += "/";
            ServerUrl = customUrl;
            Debug.Log($"[TelemetryServer] URL personalizada establecida: {ServerUrl}");
            OnServerUrlChanged?.Invoke();
        }

        public void CycleNextIP()
        {
            FindLocalIPAddresses(); // Re-escanea por si cambi la conexin
            if (LocalIPs.Count <= 1) return;
            SelectedIPIndex = (SelectedIPIndex + 1) % LocalIPs.Count;
            ServerUrl = $"http://{LocalIPs[SelectedIPIndex]}:{ActivePort}/";
            Debug.Log($"[TelemetryServer] IP cambiada a: {ServerUrl}");
            OnServerUrlChanged?.Invoke();
        }

        public void FindLocalIPAddresses()
        {
            LocalIPs.Clear();
            HashSet<string> seen = new HashSet<string>();

            // 1. Escaneo por NetworkInterface con try/catch individual por adaptador
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    try
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up || 
                            ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                            continue;

                        string name = (ni.Name + " " + ni.Description).ToLowerInvariant();

                        // Ignorar adaptadores virtuales que no comunican con el exterior
                        if (name.Contains("virtualbox") || name.Contains("vmware") || 
                            name.Contains("vethernet") || name.Contains("hyper-v") ||
                            name.Contains("wsl"))
                        {
                            continue;
                        }

                        var ipProps = ni.GetIPProperties();
                        if (ipProps == null) continue;

                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                string ip = addr.Address.ToString();

                                // Ignorar APIPA y subred interna de VirtualBox
                                if (ip.StartsWith("169.254.") || ip.StartsWith("192.168.56."))
                                    continue;

                                if (!seen.Contains(ip))
                                {
                                    seen.Add(ip);
                                    LocalIPs.Add(ip);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 2. Escaneo por DNS de Windows (garantiza encontrar la IP de Wi-Fi y USB)
            try
            {
                foreach (var ipAddr in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = ipAddr.ToString();
                        if (!ip.StartsWith("169.254.") && !ip.StartsWith("192.168.56.") && 
                            !ip.StartsWith("127.") && !seen.Contains(ip))
                        {
                            seen.Add(ip);
                            LocalIPs.Add(ip);
                        }
                    }
                }
            }
            catch { }

            if (LocalIPs.Count == 0)
            {
                LocalIPs.Add("127.0.0.1");
            }

            if (SelectedIPIndex >= LocalIPs.Count) SelectedIPIndex = 0;
            ServerUrl = $"http://{LocalIPs[SelectedIPIndex]}:{ActivePort}/";
            Debug.Log($"[TelemetryServer] IPs encontradas ({LocalIPs.Count}): {string.Join(", ", LocalIPs)} | URL activa: {ServerUrl}");
            OnServerUrlChanged?.Invoke();
        }

        private void StartServer()
        {
            int port = initialPort;
            while (port < initialPort + 50)
            {
                try
                {
                    tcpListener = new TcpListener(IPAddress.Any, port);
                    tcpListener.Start();
                    ActivePort = port;
                    isRunning = true;
                    break;
                }
                catch (SocketException)
                {
                    port++;
                }
            }

            if (!isRunning)
            {
                Debug.LogError("[TelemetryServer] No se pudo encontrar un puerto libre.");
                return;
            }

            string primaryIP = LocalIPs.Count > 0 ? LocalIPs[0] : "localhost";
            ServerUrl = $"http://{primaryIP}:{ActivePort}/";
            Debug.Log($"[TelemetryServer] Servidor iniciado con xito en: {ServerUrl}");

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "TelemetryServer_Listener"
            };
            listenerThread.Start();
        }

        private void StopServer()
        {
            isRunning = false;
            try
            {
                tcpListener?.Stop();
            }
            catch { }

            try
            {
                listenerThread?.Abort();
            }
            catch { }
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning) Debug.LogWarning($"[TelemetryServer] Error aceptando cliente: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            client.NoDelay = true;
            client.ReceiveTimeout = 4000;
            client.SendTimeout = 4000;
            string remoteIp = "desconocido";

            try
            {
                if (client.Client != null && client.Client.RemoteEndPoint != null)
                {
                    remoteIp = client.Client.RemoteEndPoint.ToString();
                }

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) return;

                    // Detectar si el navegador intent conectar con HTTPS (SSL/TLS Handshake empieza por 0x16)
                    if (buffer[0] == 0x16)
                    {
                        LastActivityLog = $"Intento HTTPS desde {remoteIp} -> Usa http:// (sin S)";
                        Debug.LogWarning($"[TelemetryServer] El celular ({remoteIp}) intent conectar con HTTPS. Por favor escribe explcitamente: http://{LocalIPs[SelectedIPIndex]}:{ActivePort}/");
                        return;
                    }

                    string requestText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] lines = requestText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    if (lines.Length == 0) return;

                    string requestLine = lines[0];
                    LastActivityLog = $"{DateTime.Now:HH:mm:ss} de {remoteIp}: {requestLine}";
                    Debug.Log($"[TelemetryServer] Conexin entrante de {remoteIp} -> {requestLine}");

                    Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrEmpty(line)) break;
                        int colon = line.IndexOf(':');
                        if (colon > 0)
                        {
                            string k = line.Substring(0, colon).Trim();
                            string v = line.Substring(colon + 1).Trim();
                            headers[k] = v;
                        }
                    }

                    // Manejo de favicon
                    if (requestLine.Contains("/favicon.ico"))
                    {
                        byte[] empty = Encoding.ASCII.GetBytes("HTTP/1.1 204 No Content\r\nConnection: close\r\n\r\n");
                        stream.Write(empty, 0, empty.Length);
                        stream.Flush();
                        return;
                    }

                    // Manejo de WebSocket Upgrade
                    if (headers.TryGetValue("Upgrade", out string upgrade) && upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
                    {
                        LastActivityLog = $"{DateTime.Now:HH:mm:ss} WebSocket CONECTADO desde {remoteIp}";
                        Debug.Log($"[TelemetryServer] WebSocket abierto con xito desde {remoteIp}");
                        HandleWebSocket(client, stream, headers);
                        return;
                    }

                    // Manejo de HTTP GET inicial (sirve la pgina del mando mvil)
                    if (requestLine.StartsWith("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] htmlBytes = Encoding.UTF8.GetBytes(GetMobileHtml());
                        string response = "HTTP/1.1 200 OK\r\n" +
                                          "Content-Type: text/html; charset=utf-8\r\n" +
                                          "Access-Control-Allow-Origin: *\r\n" +
                                          "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                          $"Content-Length: {htmlBytes.Length}\r\n" +
                                          "Connection: close\r\n\r\n";
                        byte[] headerBytes = Encoding.ASCII.GetBytes(response);
                        stream.Write(headerBytes, 0, headerBytes.Length);
                        stream.Write(htmlBytes, 0, htmlBytes.Length);
                        stream.Flush();
                        LastActivityLog = $"{DateTime.Now:HH:mm:ss} HTML enviado a {remoteIp} ({htmlBytes.Length} bytes)";
                        Debug.Log($"[TelemetryServer] WebApp enviada correctamente a {remoteIp}");
                        return;
                    }

                    // Manejo de HTTP POST de respaldo (/api/input)
                    if (requestLine.StartsWith("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        int bodyIndex = requestText.IndexOf("\r\n\r\n");
                        string body = bodyIndex >= 0 ? requestText.Substring(bodyIndex + 4) : "";
                        ProcessJsonInput(body);

                        string resp = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK";
                        byte[] rb = Encoding.ASCII.GetBytes(resp);
                        stream.Write(rb, 0, rb.Length);
                        stream.Flush();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TelemetryServer] Excepcin en cliente {remoteIp}: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (client.Client != null && client.Client.Connected)
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                    }
                }
                catch { }
                try { client.Close(); } catch { }
            }
        }

        private void HandleWebSocket(TcpClient client, NetworkStream stream, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Sec-WebSocket-Key", out string secKey)) return;

            string acceptKey = Convert.ToBase64String(
                SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(secKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))
            );

            string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();

            Interlocked.Increment(ref clientConnectedCount);

            try
            {
                byte[] buffer = new byte[4096];
                while (isRunning && client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead < 2) break;

                    int opcode = buffer[0] & 0x0F;
                    if (opcode == 8) // Close
                    {
                        break;
                    }

                    bool masked = (buffer[1] & 0x80) != 0;
                    ulong payloadLength = (ulong)(buffer[1] & 0x7F);
                    int offset = 2;

                    if (payloadLength == 126)
                    {
                        payloadLength = (ulong)((buffer[2] << 8) | buffer[3]);
                        offset = 4;
                    }
                    else if (payloadLength == 127)
                    {
                        offset = 10;
                    }

                    byte[] mask = new byte[4];
                    if (masked)
                    {
                        Array.Copy(buffer, offset, mask, 0, 4);
                        offset += 4;
                    }

                    byte[] decoded = new byte[payloadLength];
                    for (ulong i = 0; i < payloadLength; i++)
                    {
                        decoded[i] = (byte)(buffer[offset + (int)i] ^ mask[i % 4]);
                    }

                    string text = Encoding.UTF8.GetString(decoded);
                    ProcessJsonInput(text);
                }
            }
            catch { }
            finally
            {
                Interlocked.Decrement(ref clientConnectedCount);
            }
        }

        private void ProcessJsonInput(string json)
        {
            try
            {
                VehicleInputData data = JsonUtility.FromJson<VehicleInputData>(json);
                data.isConnected = true;
                lock (inputLock)
                {
                    CurrentInput = data;
                }
                lastInputTime = Time.time;
            }
            catch { }
        }

        public VehicleInputData GetLatestInput()
        {
            lock (inputLock)
            {
                return CurrentInput;
            }
        }

        private string GetMobileHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <title>GyroPilot 360 - Volante & Pedales</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-user-select: none; touch-action: none; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        body { background: #0c0f14; color: #fff; width: 100vw; height: 100vh; overflow: hidden; display: flex; flex-direction: column; }
        
        #topbar { height: 42px; background: #161b22; display: flex; align-items: center; justify-content: space-between; padding: 0 16px; border-bottom: 2px solid #238636; }
        .title { font-weight: 800; letter-spacing: 1.5px; font-size: 13px; color: #58a6ff; }
        .status { display: flex; align-items: center; gap: 8px; font-size: 12px; }
        .dot { width: 10px; height: 10px; border-radius: 50%; background: #da3633; box-shadow: 0 0 8px #da3633; }
        .dot.connected { background: #238636; box-shadow: 0 0 8px #238636; }

        #main-container { flex: 1; display: grid; grid-template-columns: 32% 36% 32%; height: calc(100vh - 42px); }

        /* PEDALES - IZQUIERDA */
        .pedals-col { display: flex; flex-direction: column; padding: 12px; gap: 12px; height: 100%; }
        .pedal { flex: 1; border-radius: 14px; border: none; font-size: 20px; font-weight: 900; letter-spacing: 1px; color: white; display: flex; align-items: center; justify-content: center; flex-direction: column; cursor: pointer; transition: transform 0.05s, filter 0.05s; }
        .pedal:active, .pedal.active { transform: scale(0.97); filter: brightness(1.25); }
        .pedal-gas { background: linear-gradient(180deg, #2ea043 0%, #196c2e 100%); box-shadow: 0 6px 14px rgba(46,160,67,0.4); border: 2px solid #3fb950; }
        .pedal-brake { background: linear-gradient(180deg, #f85149 0%, #b62324 100%); box-shadow: 0 6px 14px rgba(248,81,73,0.4); border: 2px solid #ff7b72; }
        .pedal-sub { font-size: 11px; opacity: 0.8; margin-top: 4px; font-weight: 500; }

        /* CENTRO - VOLANTE Y CALIBRACIÓN */
        .center-col { display: flex; flex-direction: column; align-items: center; justify-content: space-evenly; padding: 10px; border-left: 1px solid #21262d; border-right: 1px solid #21262d; }
        .wheel-display { width: 105px; height: 105px; border-radius: 50%; border: 6px dashed #58a6ff; display: flex; align-items: center; justify-content: center; position: relative; transition: transform 0.05s ease-out; }
        .wheel-display::after { content: ''; width: 6px; height: 32px; background: #f0883e; position: absolute; top: 0; border-radius: 3px; }
        .wheel-degrees { font-size: 16px; font-weight: 800; color: #c9d1d9; }
        .btn-engine { width: 145px; font-weight: 800; font-size: 13px; padding: 9px 12px; border-radius: 20px; cursor: pointer; transition: transform 0.08s, filter 0.08s; box-shadow: 0 4px 12px rgba(0,0,0,0.5); }
        .btn-engine:active { transform: scale(0.95); }
        .engine-off { background: linear-gradient(180deg, #da3633, #8e1515); color: #fff; border: 2px solid #f85149; box-shadow: 0 0 12px rgba(218,54,51,0.5); }
        .engine-on { background: linear-gradient(180deg, #238636, #14461d); color: #fff; border: 2px solid #3fb950; box-shadow: 0 0 12px rgba(35,134,54,0.5); }
        .btn-calibrate { background: #21262d; border: 1px solid #30363d; color: #58a6ff; font-weight: 700; font-size: 12px; padding: 8px 16px; border-radius: 20px; cursor: pointer; box-shadow: 0 3px 8px rgba(0,0,0,0.4); }
        .btn-calibrate:active { background: #30363d; transform: scale(0.96); }
        .perm-banner { font-size: 11px; color: #8b949e; text-align: center; }

        /* MARCHAS EN H - DERECHA */
        .gears-col { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 6px; height: 100%; }
        .shifter-title { font-size: 11px; text-align: center; color: #8b949e; font-weight: 800; letter-spacing: 1.5px; margin-bottom: 6px; }
        .h-shifter-plate { width: 180px; height: 190px; background: linear-gradient(145deg, #1b2028, #0b0e13); border: 2px solid #30363d; border-radius: 18px; position: relative; box-shadow: inset 0 3px 10px rgba(0,0,0,0.85), 0 6px 18px rgba(0,0,0,0.5); touch-action: none; user-select: none; }

        /* Ranuras de la H */
        .slot-rail { position: absolute; background: #05070a; border: 1px solid #21262d; border-radius: 6px; box-shadow: inset 0 3px 6px rgba(0,0,0,0.95); }
        .rail-v { width: 14px; height: 126px; top: 32px; }
        .rail-left { left: 28px; }
        .rail-mid { left: 83px; }
        .rail-right { left: 138px; }
        .rail-h { width: 124px; height: 14px; top: 88px; left: 28px; }

        /* Nmeros de las Marchas */
        .gear-slot-label { position: absolute; width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 17px; font-weight: 900; color: #6e7681; cursor: pointer; transition: all 0.15s; z-index: 2; }
        .gear-slot-label.active { color: #fff; text-shadow: 0 0 12px #58a6ff; transform: scale(1.15); }
        .pos-1 { left: 19px; top: 5px; }
        .pos-3 { left: 74px; top: 5px; }
        .pos-5 { left: 129px; top: 5px; }
        .pos-2 { left: 19px; bottom: 5px; }
        .pos-4 { left: 74px; bottom: 5px; }
        .pos-r { left: 129px; bottom: 5px; color: #da3633; }
        .pos-r.active { color: #fff; text-shadow: 0 0 12px #f85149; }
        .pos-n { left: 78px; top: 85px; font-size: 12px; color: #30363d; pointer-events: none; }

        /* Pomo de la palanca (Shift Knob) */
        .shift-knob { position: absolute; width: 44px; height: 44px; border-radius: 50%; background: radial-gradient(circle at 35% 35%, #58a6ff, #093c7a); border: 2.5px solid #e6edf3; box-shadow: 0 6px 14px rgba(0,0,0,0.95), 0 0 14px rgba(88,166,255,0.5); display: flex; align-items: center; justify-content: center; font-size: 19px; font-weight: 900; color: white; z-index: 10; top: 73px; left: 68px; transform: translate(-55px, -58px); cursor: grab; transition: transform 0.12s cubic-bezier(0.2, 0.9, 0.3, 1.2); }
        .shift-knob:active { cursor: grabbing; box-shadow: 0 2px 8px rgba(0,0,0,0.9), 0 0 20px rgba(88,166,255,0.8); }
        .shifter-status { font-size: 11px; font-weight: 800; color: #58a6ff; margin-top: 6px; letter-spacing: 1px; }
    </style>
</head>
<body>
    <div id=""topbar"">
        <div class=""title"">GYROPILOT 360</div>
        <div class=""status"">
            <span id=""status-text"">Conectando...</span>
            <div id=""status-dot"" class=""dot""></div>
        </div>
    </div>

    <div id=""main-container"">
        <!-- IZQUIERDA: PEDALES -->
        <div class=""pedals-col"">
            <button id=""btn-gas"" class=""pedal pedal-gas"">
                ACELERAR
                <span class=""pedal-sub"">ADELANTE</span>
            </button>
            <button id=""btn-brake"" class=""pedal pedal-brake"">
                FRENAR
                <span class=""pedal-sub"">FRENO / REVERSA</span>
            </button>
        </div>

        <!-- CENTRO: VOLANTE, ENCENDIDO Y CALIBRACIÓN -->
        <div class=""center-col"">
            <div id=""wheel-circle"" class=""wheel-display"">
                <span id=""wheel-text"" class=""wheel-degrees"">0°</span>
            </div>
            <button id=""btn-engine"" class=""btn-engine engine-off"" onclick=""toggleEngine()"">⚡ ENCENDER AUTO</button>
            <button id=""btn-cal"" class=""btn-calibrate"">🎯 CALIBRAR CENTRO</button>
            <div id=""perm-hint"" class=""perm-banner"">Gira el móvil como volante</div>
        </div>

        <!-- DERECHA: PALANCA EN H -->
        <div class=""gears-col"">
            <div class=""shifter-title"">CAJA MANUAL EN H</div>
            <div class=""h-shifter-plate"" id=""shifter-plate"">
                <!-- Rieles -->
                <div class=""slot-rail rail-v rail-left""></div>
                <div class=""slot-rail rail-v rail-mid""></div>
                <div class=""slot-rail rail-v rail-right""></div>
                <div class=""slot-rail rail-h rail-h""></div>

                <!-- Etiquetas de Marchas (Tap o visual) -->
                <div class=""gear-slot-label pos-1 active"" id=""slot-1"" onclick=""snapToGear(1)"">1</div>
                <div class=""gear-slot-label pos-3"" id=""slot-3"" onclick=""snapToGear(3)"">3</div>
                <div class=""gear-slot-label pos-5"" id=""slot-5"" onclick=""snapToGear(5)"">5</div>
                <div class=""gear-slot-label pos-2"" id=""slot-2"" onclick=""snapToGear(2)"">2</div>
                <div class=""gear-slot-label pos-4"" id=""slot-4"" onclick=""snapToGear(4)"">4</div>
                <div class=""gear-slot-label pos-r"" id=""slot-r"" onclick=""snapToGear(-1)"">R</div>
                <div class=""gear-slot-label pos-n"" id=""slot-n"">N</div>

                <!-- Pomo de la Palanca -->
                <div class=""shift-knob"" id=""shift-knob"">1</div>
            </div>
            <div class=""shifter-status"" id=""shifter-status"">MARCHA 1</div>
        </div>
    </div>

    <script>
        let ws = null;
        let throttle = 0.0;
        let brake = 0.0;
        let gear = 1;
        let zeroOffset = 0;
        let currentSteer = 0.0; // -1 a 1
        let lastRawAngle = 0;

        const statusDot = document.getElementById('status-dot');
        const statusText = document.getElementById('status-text');
        const wheelCircle = document.getElementById('wheel-circle');
        const wheelText = document.getElementById('wheel-text');

        function connectWebSocket() {
            const host = window.location.host;
            ws = new WebSocket('ws://' + host + '/ws');

            ws.onopen = () => {
                statusDot.classList.add('connected');
                statusText.innerText = 'CONECTADO';
            };

            ws.onclose = () => {
                statusDot.classList.remove('connected');
                statusText.innerText = 'RECONECTANDO...';
                setTimeout(connectWebSocket, 1000);
            };

            ws.onerror = () => {
                ws.close();
            };
        }

        connectWebSocket();

        // Sensor de Giroscopio y Acelermetro Multimtodo
        let hasSensorData = false;
        const debugState = document.getElementById('perm-hint');

        function applyCalculatedAngle(rawAngle) {
            hasSensorData = true;
            lastRawAngle = rawAngle;
            let calibratedAngle = rawAngle - zeroOffset;

            // Sensibilidad: +-40 grados de inclinacin = 100% giro
            let norm = calibratedAngle / 40.0;
            if (norm > 1.0) norm = 1.0;
            if (norm < -1.0) norm = -1.0;

            // Zona muerta mnima (1.5 grados)
            if (Math.abs(norm) < 0.035) norm = 0.0;

            currentSteer = norm;

            wheelCircle.style.transform = `rotate(${calibratedAngle}deg)`;
            wheelText.innerText = Math.round(calibratedAngle) + '';
            if (debugState) debugState.innerText = `Volante: ${Math.round(calibratedAngle)} (Activo)`;
        }

        function getScreenAngle() {
            if (window.screen && window.screen.orientation && typeof window.screen.orientation.angle !== 'undefined') {
                return window.screen.orientation.angle;
            }
            if (typeof window.orientation !== 'undefined') {
                return window.orientation;
            }
            return 0;
        }

        // Mtodo 1: DeviceOrientation
        function handleOrientation(event) {
            if (event.beta === null && event.gamma === null) return;

            let sAngle = getScreenAngle();
            let raw = 0;

            if (sAngle === 90) {
                raw = -event.beta;
            } else if (sAngle === 270 || sAngle === -90) {
                raw = event.beta;
            } else {
                // Modo horizontal o vertical estndar
                raw = event.gamma || 0;
                // Si gamma est casi plano pero beta se mueve con la inclinacin
                if (Math.abs(raw) < 2 && Math.abs(event.beta) > 10) {
                    raw = -event.beta;
                }
            }

            applyCalculatedAngle(raw);
        }

        // Mtodo 2: DeviceMotion (Gravedad directa, infalible en Android)
        function handleMotion(event) {
            if (hasSensorData) return; // Si Orientation ya est enviando, priorizar Orientation
            const acc = event.accelerationIncludingGravity;
            if (!acc || (acc.x === null && acc.y === null)) return;

            let sAngle = getScreenAngle();
            let rawG = 0;

            // En landscape el vector de gravedad en X o Y mide la rotacin de las manos
            if (sAngle === 90) {
                rawG = -(acc.y || 0) * 8.5;
            } else if (sAngle === 270 || sAngle === -90) {
                rawG = (acc.y || 0) * 8.5;
            } else {
                rawG = (acc.x || 0) * 8.5;
            }

            applyCalculatedAngle(rawG);
        }

        // Registrar ambos detectores
        window.addEventListener('deviceorientation', handleOrientation);
        window.addEventListener('devicemotion', handleMotion);

        // Botn de Calibracin y Solicitud de Permisos (Safari / Chrome)
        document.getElementById('btn-cal').addEventListener('click', () => {
            zeroOffset = lastRawAngle;

            if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
                DeviceOrientationEvent.requestPermission().then(state => {
                    if (state === 'granted') {
                        window.addEventListener('deviceorientation', handleOrientation);
                    }
                }).catch(()=>{});
            }

            if (typeof DeviceMotionEvent !== 'undefined' && typeof DeviceMotionEvent.requestPermission === 'function') {
                DeviceMotionEvent.requestPermission().then(state => {
                    if (state === 'granted') {
                        window.addEventListener('devicemotion', handleMotion);
                    }
                }).catch(()=>{});
            }

            if (debugState) debugState.innerText = 'Centro Calibrado (0)';
            if (navigator.vibrate) navigator.vibrate(40);
        });

        // Respaldo Tctil en el Volante (Deslizar el dedo por el centro tambin gira)
        const centerCol = document.querySelector('.center-col');
        let touchStartX = 0;
        let isTouchingWheel = false;

        centerCol.addEventListener('touchstart', (e) => {
            if (e.target.id === 'btn-cal') return;
            touchStartX = e.touches[0].clientX;
            isTouchingWheel = true;
        });

        centerCol.addEventListener('touchmove', (e) => {
            if (!isTouchingWheel) return;
            let deltaX = e.touches[0].clientX - touchStartX;
            let touchAngle = (deltaX / 80.0) * 45.0;
            applyCalculatedAngle(touchAngle);
        });

        centerCol.addEventListener('touchend', () => {
            if (isTouchingWheel) {
                isTouchingWheel = false;
                applyCalculatedAngle(0);
            }
        });

        // Pedales Tctiles
        const btnGas = document.getElementById('btn-gas');
        const btnBrake = document.getElementById('btn-brake');

        btnGas.addEventListener('touchstart', (e) => { e.preventDefault(); throttle = 1.0; btnGas.classList.add('active'); if(navigator.vibrate) navigator.vibrate(20); });
        btnGas.addEventListener('touchend', (e) => { e.preventDefault(); throttle = 0.0; btnGas.classList.remove('active'); });
        btnGas.addEventListener('touchcancel', () => { throttle = 0.0; btnGas.classList.remove('active'); });

        btnBrake.addEventListener('touchstart', (e) => { e.preventDefault(); brake = 1.0; btnBrake.classList.add('active'); if(navigator.vibrate) navigator.vibrate(20); });
        btnBrake.addEventListener('touchend', (e) => { e.preventDefault(); brake = 0.0; btnBrake.classList.remove('active'); });
        btnBrake.addEventListener('touchcancel', () => { brake = 0.0; btnBrake.classList.remove('active'); });

        // Respaldo de click de ratn para pruebas en PC
        btnGas.addEventListener('mousedown', () => { throttle = 1.0; btnGas.classList.add('active'); });
        btnGas.addEventListener('mouseup', () => { throttle = 0.0; btnGas.classList.remove('active'); });
        btnBrake.addEventListener('mousedown', () => { brake = 1.0; btnBrake.classList.add('active'); });
        btnBrake.addEventListener('mouseup', () => { brake = 0.0; btnBrake.classList.remove('active'); });

        // ==========================================
        // PALANCA DE CAMBIOS EN H (INTERACTIVA)
        // ==========================================
        const shifterPlate = document.getElementById('shifter-plate');
        const shiftKnob = document.getElementById('shift-knob');
        const shifterStatus = document.getElementById('shifter-status');

        const gearSlots = {
            '1':  { x: -55, y: -58, label: '1', name: 'MARCHA 1' },
            '2':  { x: -55, y: 58,  label: '2', name: 'MARCHA 2' },
            '3':  { x: 0,   y: -58, label: '3', name: 'MARCHA 3' },
            '4':  { x: 0,   y: 58,  label: '4', name: 'MARCHA 4' },
            '5':  { x: 55,  y: -58, label: '5', name: 'MARCHA 5' },
            '-1': { x: 55,  y: 58,  label: 'R', name: 'REVERSA' },
            '0':  { x: 0,   y: 0,   label: 'N', name: 'NEUTRO' }
        };

        let isDraggingKnob = false;
        let knobCurX = -55;
        let knobCurY = -58;

        function snapToGear(targetGear) {
            gear = targetGear;
            const pos = gearSlots[targetGear.toString()] || gearSlots['0'];

            knobCurX = pos.x;
            knobCurY = pos.y;

            shiftKnob.style.transition = 'transform 0.14s cubic-bezier(0.2, 0.9, 0.3, 1.2)';
            shiftKnob.style.transform = `translate(${pos.x}px, ${pos.y}px)`;
            shiftKnob.innerText = pos.label;
            if (shifterStatus) shifterStatus.innerText = pos.name;

            // Actualizar resaltado de ranura
            document.querySelectorAll('.gear-slot-label').forEach(el => el.classList.remove('active'));
            let activeId = targetGear === -1 ? 'slot-r' : (targetGear === 0 ? 'slot-n' : 'slot-' + targetGear);
            const activeEl = document.getElementById(activeId);
            if (activeEl) activeEl.classList.add('active');

            if (navigator.vibrate) navigator.vibrate([25]);
        }
        window.snapToGear = snapToGear;

        // Arrastre por pantalla tctil en la H
        function handleShifterDrag(clientX, clientY) {
            const rect = shifterPlate.getBoundingClientRect();
            const plateCenterX = rect.left + rect.width * 0.5;
            const plateCenterY = rect.top + rect.height * 0.5;

            const dx = clientX - plateCenterX;
            const dy = clientY - plateCenterY;

            // Friccin en rieles en H
            if (Math.abs(dy) > 16) {
                // Dentro de uno de los 3 rieles verticales
                knobCurX = dx < -27 ? -55 : (dx > 27 ? 55 : 0);
                knobCurY = Math.max(-58, Math.min(58, dy));
            } else {
                // Dentro del riel horizontal de Neutro
                knobCurX = Math.max(-55, Math.min(55, dx));
                knobCurY = 0;
            }

            shiftKnob.style.transition = 'none';
            shiftKnob.style.transform = `translate(${knobCurX}px, ${knobCurY}px)`;
        }

        function finishShifterDrag() {
            if (!isDraggingKnob) return;
            isDraggingKnob = false;

            // Detectar ranura ms cercana
            let chosenGear = 0; // Neutro
            if (knobCurY < -24) {
                if (knobCurX < -27) chosenGear = 1;
                else if (knobCurX > 27) chosenGear = 5;
                else chosenGear = 3;
            } else if (knobCurY > 24) {
                if (knobCurX < -27) chosenGear = 2;
                else if (knobCurX > 27) chosenGear = -1; // R
                else chosenGear = 4;
            } else {
                chosenGear = 0; // Neutro
            }

            snapToGear(chosenGear);
        }

        shifterPlate.addEventListener('touchstart', (e) => {
            e.preventDefault();
            isDraggingKnob = true;
            handleShifterDrag(e.touches[0].clientX, e.touches[0].clientY);
        });

        shifterPlate.addEventListener('touchmove', (e) => {
            if (!isDraggingKnob) return;
            e.preventDefault();
            handleShifterDrag(e.touches[0].clientX, e.touches[0].clientY);
        });

        shifterPlate.addEventListener('touchend', finishShifterDrag);
        shifterPlate.addEventListener('touchcancel', finishShifterDrag);

        // Respaldo de ratn para pruebas en PC
        shifterPlate.addEventListener('mousedown', (e) => {
            isDraggingKnob = true;
            handleShifterDrag(e.clientX, e.clientY);
        });

        window.addEventListener('mousemove', (e) => {
            if (isDraggingKnob) handleShifterDrag(e.clientX, e.clientY);
        });

        window.addEventListener('mouseup', () => {
            if (isDraggingKnob) finishShifterDrag();
        });

        // Encendido / Apagado de motor desde el celular
        let engineState = 0; // Comienza apagado
        function toggleEngine() {
            engineState = (engineState === 1) ? 0 : 1;
            const btn = document.getElementById('btn-engine');
            if (btn) {
                if (engineState === 1) {
                    btn.innerHTML = '🛑 APAGAR AUTO';
                    btn.className = 'btn-engine engine-on';
                } else {
                    btn.innerHTML = '⚡ ENCENDER AUTO';
                    btn.className = 'btn-engine engine-off';
                }
            }
            if (navigator.vibrate) navigator.vibrate([35]);
        }
        window.toggleEngine = toggleEngine;

        // Iniciar en Marcha 1
        snapToGear(1);

        // Loop de envío de telemetría a 60 FPS
        setInterval(() => {
            const payload = JSON.stringify({
                steer: Number(currentSteer.toFixed(3)),
                throttle: Number(throttle.toFixed(2)),
                brake: Number(brake.toFixed(2)),
                gear: gear,
                engine: engineState,
                timestamp: Date.now()
            });

            if (ws && ws.readyState === WebSocket.OPEN) {
                ws.send(payload);
            } else {
                fetch('/api/input', { method: 'POST', body: payload }).then(() => {
                    statusDot.classList.add('connected');
                    statusText.innerText = 'CONECTADO (HTTP)';
                }).catch(() => {});
            }
        }, 16);
    </script>
</body>
</html>";
        }
    }
}
