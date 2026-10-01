using System.IO;
using RosSharp.RosBridgeClient;
using UnityEngine;

/// <summary>
/// Client side of the client/server setup: the Gazebo simulation (ROS 1 + rosbridge)
/// runs on a server, this Unity project connects to it over the network.
///
/// Reads StreamingAssets/twin_server.json and points every RosConnector of the scene at
/// ws://host:port. DefaultExecutionOrder makes this Awake run before RosConnector.Awake,
/// which starts connecting immediately. The file stays editable next to a build
/// (&lt;App&gt;_Data/StreamingAssets/), so changing server needs no rebuild.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class TwinServerConfig : MonoBehaviour
{
    public const string FileName = "twin_server.json";

    [System.Serializable]
    public class Config
    {
        public string host = "localhost";
        public int port = 9090;
    }

    public string ResolvedUrl { get; private set; }

    private void Awake()
    {
        Config config = Load(Path.Combine(Application.streamingAssetsPath, FileName));
        ResolvedUrl = $"ws://{config.host}:{config.port}";

        foreach (var connector in FindObjectsByType<RosConnector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            connector.RosBridgeServerUrl = ResolvedUrl;

        Debug.Log($"[TwinServerConfig] rosbridge server: {ResolvedUrl}");
    }

    private static Config Load(string path)
    {
        var config = new Config();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[TwinServerConfig] {path} not found, using {config.host}:{config.port}.");
            return config;
        }

        try
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TwinServerConfig] cannot read {path}: {e.Message}. Using defaults.");
            return new Config();
        }

        if (string.IsNullOrWhiteSpace(config.host) || config.port <= 0 || config.port > 65535)
        {
            Debug.LogError($"[TwinServerConfig] invalid host/port in {path}. Using defaults.");
            return new Config();
        }
        config.host = config.host.Trim();
        return config;
    }
}
