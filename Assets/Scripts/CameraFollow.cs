using UnityEngine;

// 카메라가 구구 뒤에서 부드럽게 따라가며 구구를 바라본다.
// 벽이 사이에 끼면 벽 앞으로 당겨 와서 구구가 가려지지 않게 한다.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상. 비워 두면 Player 태그가 붙은 물체를 찾는다.")]
    public Transform target;
    [Tooltip("구구 기준 카메라 위치: 위로 1.6m, 뒤로 3m")]
    public Vector3 offset = new Vector3(0f, 1.6f, -3f);
    [Tooltip("바라볼 높이 (구구 머리쯤)")]
    public float lookHeight = 0.3f;
    [Tooltip("따라가는 부드러움. 작을수록 빨리 붙는다.")]
    public float smoothTime = 0.15f;
    [Tooltip("벽과 카메라 사이에 둘 여유 (m)")]
    public float wallPadding = 0.2f;

    Vector3 velocity;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
        SnapToTarget();
    }

    // 구구가 움직인 뒤(Update 다음)에 카메라를 옮겨야 화면이 떨리지 않는다.
    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, WantedPosition(), ref velocity, smoothTime);
        transform.LookAt(LookPoint());
    }

    // 방을 옮긴 직후처럼 부드럽게 말고 바로 붙어야 할 때
    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        transform.position = WantedPosition();
        transform.LookAt(LookPoint());
    }

    Vector3 LookPoint() => target.position + Vector3.up * lookHeight;

    Vector3 WantedPosition()
    {
        Vector3 from = LookPoint();
        Vector3 wanted = target.position + offset;
        Vector3 dir = wanted - from;
        // 구구 머리에서 카메라 자리까지 공을 굴려 보고, 벽에 맞으면 그 앞에 멈춘다
        if (Physics.SphereCast(from, 0.15f, dir.normalized, out RaycastHit hit, dir.magnitude,
                               ~0, QueryTriggerInteraction.Ignore) && hit.transform != target)
            return from + dir.normalized * Mathf.Max(0.3f, hit.distance - wallPadding);
        return wanted;
    }
}
