using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RankingPanel : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject root;                 // RankingPanel 오브젝트 (없으면 자기 자신 사용)
    public Transform content;               // ScrollView/Viewport/Content
    public RankingSlotUI slotPrefab;        // RankingSlot Prefab
    public TMP_Text statusText;             // (선택) "불러오는 중..." 표시용

    [Header("Options")]
    public int top = 10;

    // 메인스레드 반영용 큐
    private readonly Queue<Action> _mainThreadJobs = new();

    private void Awake()
    {
        if (root == null) root = gameObject;
    }

    private void Update()
    {
        while (true)
        {
            Action job = null;
            lock (_mainThreadJobs)
            {
                if (_mainThreadJobs.Count == 0) break;
                job = _mainThreadJobs.Dequeue();
            }
            job?.Invoke();
        }
    }

    // ===== 외부(MainMenuController)에서 호출 =====
    public void Open()
    {
        root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        root.SetActive(false);
    }

    public void Refresh()
    {
        ClearSlots();

        if (statusText) statusText.text = "랭킹 불러오는 중...";

        var tcp = (GameSession.I != null) ? GameSession.I.tcpClient : null;
        if (tcp == null)
        {
            if (statusText) statusText.text = "TCP 클라이언트 없음";
            Debug.LogError("GameSession.I.tcpClient is null");
            return;
        }

        tcp.GetRanking(top, (ok, res) =>
        {
            EnqueueMainThread(() =>
            {
                if (!ok)
                {
                    if (statusText) statusText.text = "랭킹 조회 실패";
                    Debug.LogError("랭킹 조회 실패: " + res);
                    return;
                }

                // JsonUtility는 최상위 배열이 안되니까, 서버 응답이 {"ok":true,"rankings":[...]} 형태여야 함
                RankingResponse data = null;
                try
                {
                    data = JsonUtility.FromJson<RankingResponse>(res);
                }
                catch (Exception e)
                {
                    if (statusText) statusText.text = "응답 파싱 실패";
                    Debug.LogError("JSON parse error: " + e.Message + "\n" + res);
                    return;
                }

                if (data == null || data.rankings == null)
                {
                    if (statusText) statusText.text = "랭킹 없음";
                    return;
                }

                if (statusText) statusText.text = "";

                for (int i = 0; i < data.rankings.Length; i++)
                {
                    var r = data.rankings[i];
                    var slot = Instantiate(slotPrefab, content);
                    slot.Set(i + 1, r.name, r.score);
                }
            });
        });
    }

    private void ClearSlots()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private void EnqueueMainThread(Action a)
    {
        lock (_mainThreadJobs)
            _mainThreadJobs.Enqueue(a);
    }

    // ===== 서버 응답 DTO =====
    [Serializable]
    private class RankingResponse
    {
        public bool ok;
        public RankingItem[] rankings;
    }

    [Serializable]
    private class RankingItem
    {
        public string name;
        public int score;
    }
}
