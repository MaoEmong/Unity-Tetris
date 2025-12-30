using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/*
=========================================================
GameSession.cs  (ONE FILE)
- GameSession (DontDestroyOnLoad manager)
- MainMenuController (UI handler for Main scene)
=========================================================
*/

public class GameSession : MonoBehaviour
{
    /* =========================
       [1] Singleton
       ========================= */
    public static GameSession I { get; private set; }

    /* =========================
       [2] Data to keep
       ========================= */
    public string nickname;
    public string serverIp;
    public int serverPort;

    public int lastScore;

    public TcpRankClient tcpClient;

    /* =========================
       [3] Options
       ========================= */
    public bool saveToPlayerPrefs = true;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        // TcpRankClient 자동 부착
        tcpClient = gameObject.GetComponent<TcpRankClient>();
        if (tcpClient == null)
            tcpClient = gameObject.AddComponent<TcpRankClient>();

        LoadPrefsIfAny();
    }

    /* =========================
       [4] Public API
       ========================= */
    public void SetConnection(string nick, string ip, int port)
    {
        nickname = nick;
        serverIp = ip;
        serverPort = port;

        if (saveToPlayerPrefs)
            SavePrefs();
    }

    public void SetScore(int score)
    {
        lastScore = score;
    }

    // 나중에 서버 붙일 때 여기 구현
    public void SubmitRankingIfPossible()
    {
        // StartCoroutine(ServerApi.PostRanking(nickname, lastScore, (ok, msg) =>
        // {
        //     if (ok) Debug.Log("랭킹 등록 성공: " + msg);
        //     else Debug.LogError("랭킹 등록 실패: " + msg);
        // }));

        if (tcpClient == null)
        {
            Debug.LogError("TcpRankClient not ready.");
            return;
        }

        Debug.Log($"[SubmitRanking] {nickname} / {lastScore}");
        tcpClient.SubmitScore(nickname, lastScore);

        // 게임 씬 이동
        SceneManager.LoadScene("MenuScene");
    }

    /* =========================
       [5] PlayerPrefs
       ========================= */
    private const string KEY_NICK = "TETRIS_NICK";
    private const string KEY_IP = "TETRIS_IP";
    private const string KEY_PORT = "TETRIS_PORT";

    private void SavePrefs()
    {
        PlayerPrefs.SetString(KEY_NICK, nickname);
        PlayerPrefs.SetString(KEY_IP, serverIp);
        PlayerPrefs.SetInt(KEY_PORT, serverPort);
        PlayerPrefs.Save();
    }

    private void LoadPrefsIfAny()
    {
        if (!PlayerPrefs.HasKey(KEY_NICK)) return;

        nickname = PlayerPrefs.GetString(KEY_NICK, "Player");
        serverIp = PlayerPrefs.GetString(KEY_IP, "127.0.0.1");
        serverPort = PlayerPrefs.GetInt(KEY_PORT, 8080);
    }
}

/* =========================================================
   MainMenuController (attach to a Main scene object)
   - Requires TMP_InputField / TMP_Text
   ========================================================= */
