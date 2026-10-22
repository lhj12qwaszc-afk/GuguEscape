using UnityEngine;

// 방에 들어왔을 때 구구가 나타나는 자리. Door의 spawnId와 같은 id를 적는다.
public class SpawnPoint : MonoBehaviour
{
    public string id;

    // Scene 화면에서만 보이는 표시 (초록 공 + 바라보는 방향)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
    }
}
