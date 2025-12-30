using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/*
=========================================================
TcpRankClient.cs (ONE FILE)
- TCP 연결 테스트(PING)
- 서버 연결 + 한 줄 송신 + 한 줄 수신
=========================================================
*/

public class TcpRankClient : MonoBehaviour
{
    [Header("Server")]
    public string ip = ENV.serverIp;
    public int port = 8000;

    [Header("Timeout (ms)")]
    public int connectTimeoutMs = 1500;
    public int readTimeoutMs = 1500;
    public int writeTimeoutMs = 1500;

    [ContextMenu("Test Ping")]
    public void TestPing()
    {
        // 메인스레드에서 바로 돌리면 잠깐 멈출 수 있어서 스레드로 한 번 감쌈
        // (연결 체크용이라 최소만)
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                string req = "{\"type\":\"ping\"}";
                var (ok, res) = SendOneLine(req);

                if (ok)
                    Debug.Log($"[TCP PING OK] {res}");
                else
                    Debug.LogError($"[TCP PING FAIL] {res}");
            }
            catch (Exception e)
            {
                Debug.LogError("[TCP PING EXCEPTION] " + e.Message);
            }
        });
    }

    public void SubmitScore(string nickname, int score)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                string json =
                    $"{{\"type\":\"submit\",\"nickname\":\"{nickname}\",\"score\":{score}}}";

                var (ok, res) = SendOneLine(json);

                if (ok)
                    Debug.Log("[SUBMIT OK] " + res);
                else
                    Debug.LogError("[SUBMIT FAIL] " + res);
            }
            catch (Exception e)
            {
                Debug.LogError("[SUBMIT EXCEPTION] " + e.Message);
            }
        });
    }

    // 핵심: 연결 -> 1줄 보내기 -> 1줄 받기 -> 종료
    private (bool ok, string responseOrError) SendOneLine(string line)
    {
        try
        {
            using (var client = new TcpClient())
            {
                // connect timeout 처리
                var ar = client.BeginConnect(ip, port, null, null);
                bool connected = ar.AsyncWaitHandle.WaitOne(connectTimeoutMs);
                if (!connected)
                    return (false, $"connect timeout ({connectTimeoutMs}ms)");

                client.EndConnect(ar);

                client.ReceiveTimeout = readTimeoutMs;
                client.SendTimeout = writeTimeoutMs;

                using (NetworkStream ns = client.GetStream())
                using (var writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                using (var reader = new StreamReader(ns, new UTF8Encoding(false)))
                {
                    // 송신
                    writer.WriteLine(line);

                    // 수신(서버가 \n으로 끝내야 함)
                    string resp = reader.ReadLine();
                    if (string.IsNullOrEmpty(resp))
                        return (false, "empty response");

                    return (true, resp);
                }
            }
        }
        catch (SocketException se)
        {
            return (false, $"socket error: {se.SocketErrorCode} / {se.Message}");
        }
        catch (IOException ioe)
        {
            return (false, $"io error: {ioe.Message}");
        }
        catch (Exception e)
        {
            return (false, $"error: {e.Message}");
        }
    }

    public void GetRanking(int top, Action<bool, string> onComplete)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                if (top <= 0) top = 10;
                if (top > 100) top = 100;

                string json = $"{{\"type\":\"get\",\"top\":{top}}}";
                var (ok, res) = SendOneLine(json);

                // ⚠️ 스레드에서 바로 UI 건드리면 안 되니까, 문자열만 콜백으로 넘김
                onComplete?.Invoke(ok, res);
            }
            catch (Exception e)
            {
                onComplete?.Invoke(false, "exception: " + e.Message);
            }
        });
    }
}
