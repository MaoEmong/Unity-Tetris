using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
    [Header("Input Fields (TMP)")]
    public TMP_InputField nicknameInput;
    public TMP_InputField ipInput;
    public TMP_InputField portInput;

    [Header("UI")]
    public TMP_Text messageText;
    public RankingPanel rankingPanel;

    [Header("Scenes")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        // GameSession 없으면 자동 생성
        if (GameSession.I == null)
        {
            var go = new GameObject("GameSession");
            go.AddComponent<GameSession>();
        }
        // 저장값 있으면 입력칸에 채우기
        if (nicknameInput) nicknameInput.text = string.IsNullOrEmpty(GameSession.I.nickname) ? "Player" : GameSession.I.nickname;
        if (ipInput) ipInput.text = string.IsNullOrEmpty(GameSession.I.serverIp) ? "127.0.0.1" : GameSession.I.serverIp;
        if (portInput) portInput.text = (GameSession.I.serverPort <= 0 ? 8080 : GameSession.I.serverPort).ToString();

        SetMessage("");
    }

    // 버튼 OnClick에 연결
    public void OnClickEnterGame()
    {
        string nick = nicknameInput ? nicknameInput.text.Trim() : "";
        string ip = ipInput ? ipInput.text.Trim() : "";
        string portStr = portInput ? portInput.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(nick))
        {
            SetMessage("닉네임을 입력하세요.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ip))
        {
            SetMessage("IP를 입력하세요. (예: 127.0.0.1)");
            return;
        }

        if (!int.TryParse(portStr, out int port) || port <= 0 || port > 65535)
        {
            SetMessage("포트번호가 올바르지 않습니다. (1~65535)");
            return;
        }

        GameSession.I.SetConnection(nick, ip, port);

        // 게임 씬 이동
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    private void SetMessage(string msg)
    {
        if (messageText) messageText.text = msg;
    }

    public void OnClickOpenRanking()
    {
        rankingPanel.Open();
    }

    public void OnClickCloseRanking()
    {
        rankingPanel.Close();
    }
}

