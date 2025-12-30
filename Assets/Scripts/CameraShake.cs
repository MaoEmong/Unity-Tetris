using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originPos;
    private Coroutine shakeRoutine;
    private int currentPriority = 0;

    void Awake()
    {
        originPos = transform.localPosition;
    }

    // priority: 숫자 클수록 우선순위 높음
    public void Shake(float duration, float strength, int priority)
    {
        // 이미 더 높은 우선순위 쉐이크 중이면 무시
        if (shakeRoutine != null && priority < currentPriority)
            return;

        // 같은거나 더 높은 우선순위면 교체
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        currentPriority = priority;
        shakeRoutine = StartCoroutine(DoShake(duration, strength));
    }

    IEnumerator DoShake(float duration, float strength)
    {
        float t = 0f;

        while (t < duration)
        {
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            transform.localPosition = originPos + new Vector3(x, y, 0f);

            t += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originPos;
        shakeRoutine = null;
        currentPriority = 0;
    }
}
