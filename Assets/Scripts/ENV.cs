using UnityEngine;

public class ENV
{
    public static string serverIp = "127.0.0.1";
    public static string serverPort = "8000";
    public static string serverURL = $"http://{ENV.serverIp}:{ENV.serverPort}/ranking";
}
