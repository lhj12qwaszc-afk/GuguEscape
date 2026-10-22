using UnityEngine;

// 방마다 숨겨진 과자. 빙글빙글 돌며 둥둥 뜨고, 구구가 닿으면 먹는다.
[RequireComponent(typeof(Collider))]
public class Snack : MonoBehaviour
{
    [Tooltip("과자마다 다른 이름 (예: Room1). 먹은 과자는 방에 다시 와도 안 나온다.")]
    public string snackId;
    [Tooltip("도는 속도 (도/초)")]
    public float spinSpeed = 90f;
    [Tooltip("위아래로 움직이는 높이 (m)")]
    public float bobHeight = 0.06f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void Start()
    {
        // 이미 먹은 과자면 없앤다
        if (GameSession.Instance != null && GameSession.Instance.IsCollected(snackId))
        {
            Destroy(gameObject);
            return;
        }
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameSession.Instance != null) GameSession.Instance.Collect(snackId);
        Destroy(gameObject);
    }
}
