using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using RosSharp.RosBridgeClient;
using UnityEngine;

/// <summary>
/// Client side of the client/server setup: the Gazebo simulation (ROS 1 + rosbridge)
/// runs on a server, this Unity project connects to it over the network.
///
/// Reads StreamingAssets/twin_server.json and points every RosConnector of the scene at
/// ws://host:port. The file lists several servers (e.g. localhost, then the lab server for
/// a Windows client on the LAN); the first one accepting a TCP connection wins, the first
/// of the list is used if none answers. DefaultExecutionOrder makes this Awake run before RosConnector.Awake,
/// which starts connecting immediately. The file stays editable next to a build
/// (&lt;App&gt;_Data/StreamingAssets/), so changing server needs no rebuild.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class TwinServerConfig : MonoBehaviour
{
    public const string FileName = "twin_server.json";

    [System.Serializable]
    public class Server
    {
        public string name = "local";
        public string host = "localhost";
        public int port = 9090;

        public string Url { get { return $"ws://{host}:{port}"; } }
    }

    [System.Serializable]
    public class Config
    {
        [Tooltip("Tried in order; the first reachable one is used.")]
        public List<Server> servers = new List<Server>();

        [Tooltip("TCP connect timeout per server, in milliseconds.")]
        public int probeTimeoutMs = 400;

        // Single-server format of earlier twin_server.json files; used when servers is empty.
        public string host;
        public int port;

        [Tooltip("Gazebo is the reference: the opaque robot shows /joint_states, the robot " +
                 "driven by the IK is drawn as a translucent setpoint. See TiagoGazeboAuthority.")]
        public bool gazeboIsAuthority = true;

        [Tooltip("Draw the Gazebo test objects and move the twin with the Gazebo base. " +
                 "See TwinGazeboWorld.")]
        public bool gazeboWorld = true;

        [Tooltip("Drive the base with the joysticks / keyboard. See TwinBaseTeleop.")]
        public bool baseTeleop = true;

        [Tooltip("Open/close the grippers with the triggers / keyboard. See TwinGripperControl.")]
        public bool gripperControl = true;
    }

    /// <summary>Config read by the last Awake; defaults until then.</summary>
    public static Config Current { get; private set; } = Defaults();

    public string ResolvedUrl { get; private set; }

    private void Awake()
    {
        Config config = Load(Path.Combine(Application.streamingAssetsPath, FileName));
        Current = config;
        Server server = PickServer(config);
        ResolvedUrl = server.Url;

        foreach (var connector in FindObjectsByType<RosConnector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            connector.RosBridgeServerUrl = ResolvedUrl;

        Debug.Log($"[TwinServerConfig] rosbridge server: {server.name} ({ResolvedUrl})");
    }

    private static Server PickServer(Config config)
    {
        foreach (Server s in config.servers)
        {
            if (IsReachable(s, config.probeTimeoutMs)) return s;
            Debug.Log($"[TwinServerConfig] {s.name} ({s.Url}) not reachable.");
        }
        Debug.LogWarning($"[TwinServerConfig] no server answered; using {config.servers[0].name}, " +
                         "RosConnector will keep retrying.");
        return config.servers[0];
    }

    private static bool IsReachable(Server s, int timeoutMs)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connect = client.ConnectAsync(s.host, s.port);
                return connect.Wait(Mathf.Max(50, timeoutMs)) && client.Connected;
            }
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static Config Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[TwinServerConfig] {path} not found, using localhost:9090.");
            return Defaults();
        }
        var config = new Config();

        try
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TwinServerConfig] cannot read {path}: {e.Message}. Using defaults.");
            return Defaults();
        }

        if ((config.servers == null || config.servers.Count == 0) && !string.IsNullOrWhiteSpace(config.host))
            config.servers = new List<Server> { new Server { name = "server", host = config.host, port = config.port > 0 ? config.port : 9090 } };
        if (config.servers == null) config.servers = new List<Server>();

        var valid = new List<Server>();
        foreach (Server s in config.servers)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.host) || s.port <= 0 || s.port > 65535)
            {
                Debug.LogError($"[TwinServerConfig] invalid server entry in {path}; skipped.");
                continue;
            }
            s.host = s.host.Trim();
            if (string.IsNullOrWhiteSpace(s.name)) s.name = s.host;
            valid.Add(s);
        }
        if (valid.Count == 0)
        {
            Debug.LogError($"[TwinServerConfig] no valid server in {path}. Using defaults.");
            return Defaults();
        }
        config.servers = valid;
        return config;
    }

    private static Config Defaults()
    {
        return new Config { servers = new List<Server> { new Server() } };
    }
}
