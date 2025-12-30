using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public static class ServerApi
{
    // -----------------------------------------
    // [1] Generic: POST JSON
    // url: full url (ex: http://127.0.0.1:8080/ranking)
    // jsonBody: already-built json string
    // -----------------------------------------
    public static IEnumerator Post(string url, string jsonBody, Action<bool, string> onComplete = null)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
            else
            {
                // 서버가 에러 바디를 내려주면 그걸 우선 반환
                string err = string.IsNullOrEmpty(req.downloadHandler.text) ? req.error : req.downloadHandler.text;
                onComplete?.Invoke(false, err);
            }
        }
    }

    // -----------------------------------------
    // [2] Domain: POST Ranking
    // nickname/score -> json -> PostJson
    // -----------------------------------------
    public static IEnumerator PostRanking(string nickname, int score, Action<bool, string> onComplete = null)
    {
        // ENV.serverURL은 base로 두는 걸 추천 (ex: http://127.0.0.1:8080)
        string url = $"{ENV.serverURL}/ranking";

        var body = new SetRanking
        {
            nickname = nickname,
            score = score
        };

        string json = JsonUtility.ToJson(body);
        yield return Post(url, json, onComplete);
    }

}
