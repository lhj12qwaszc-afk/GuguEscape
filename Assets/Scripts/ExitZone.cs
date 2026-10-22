using UnityEngine;

// 가운데 방 천장 구멍 아래 탈출 지점.
// 과자를 다 모으면 햇빛이 켜지고, 그 아래로 가면 탈출(승리).
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("과자를 다 모으면 켜질 햇빛")]
    public Light sunlight;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void Update()
    {
        if (sunlight == null || GameSession.Instance == null) return;
        sunlight.enabled = GameSession.Instance.AllSnacksCollected;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var session = GameSession.Instance;
        if (session != null && session.AllSnacksCollected) session.Win();
    }
}
