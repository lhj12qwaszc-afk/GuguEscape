using UnityEngine;

// 동굴 입구. 구구가 닿으면 다른 방(씬)으로 이동한다.
[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour
{
    [Tooltip("이동할 씬 이름 (예: Cave_Room1)")]
    public string targetScene;
    [Tooltip("도착한 씬에서 구구가 나타날 SpawnPoint의 id")]
    public string spawnId;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameSession.Instance != null) GameSession.Instance.Travel(targetScene, spawnId);
    }
}
