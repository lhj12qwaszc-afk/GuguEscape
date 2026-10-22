using UnityEngine;
using UnityEngine.InputSystem;

// 3인칭 카메라: 마우스로 구구 주변을 빙 돌며 따라간다.
// 벽이 사이에 끼면 벽 앞으로 당겨 와서 구구가 가려지지 않게 한다.
// Esc → 마우스 커서 보이기 / 게임 화면 클릭 → 다시 숨기기
public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상. 비워 두면 Player 태그가 붙은 물체를 찾는다.")]
    public Transform target;
    [Tooltip("구구와 카메라 사이 거리 (m)")]
    public float distance = 3.4f;
    [Tooltip("바라볼 높이 (구구 머리쯤)")]
    public float lookHeight = 0.3f;

    [Header("마우스")]
    [Tooltip("마우스 감도 (픽셀당 도)")]
    public float mouseSensitivity = 0.15f;
    [Tooltip("위아래로 볼 수 있는 범위 (도). 음수 = 아래에서 올려다봄")]
    public float minPitch = -5f;
    public float maxPitch = 65f;

    [Header("부드러움 · 벽")]
    [Tooltip("따라가는 부드러움. 작을수록 빨리 붙는다.")]
    public float smoothTime = 0.08f;
    [Tooltip("벽과 카메라 사이에 둘 여유 (m)")]
    public float wallPadding = 0.2f;

    float yaw;          // 좌우 각도
    float pitch = 28f;  // 위아래 각도 (처음엔 비스듬히 내려다봄)
    Vector3 velocity;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
        SetCursorLocked(true);
        SnapToTarget();
    }

    void Update()
    {
        bool over = GameSession.Instance != null && GameSession.Instance.IsOver;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // 커서 잠금: Esc로 풀고, 클릭하면 다시 잠근다. 게임이 끝나면 버튼을 누를 수 있게 푼다.
        if (over || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)) SetCursorLocked(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);

        // 커서가 잠겨 있을 때만 마우스로 카메라를 돌린다
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
        }
    }

    // 구구가 움직인 뒤(Update 다음)에 카메라를 옮겨야 화면이 떨리지 않는다.
    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, WantedPosition(), ref velocity, smoothTime);
        transform.LookAt(LookPoint());
    }

    // 방을 옮긴 직후: 구구가 보는 방향 뒤로 카메라를 바로 붙인다
    public void SnapToTarget()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        velocity = Vector3.zero;
        transform.position = WantedPosition();
        transform.LookAt(LookPoint());
    }

    Vector3 LookPoint() => target.position + Vector3.up * lookHeight;

    Vector3 WantedPosition()
    {
        Vector3 from = LookPoint();
        // 각도(yaw, pitch)만큼 돌린 방향으로 distance만큼 뒤에 선다
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        // 구구 머리에서 카메라 자리까지 공을 굴려 보고, 벽에 맞으면 그 앞에 멈춘다
        if (Physics.SphereCast(from, 0.15f, dir, out RaycastHit hit, distance,
                               ~0, QueryTriggerInteraction.Ignore) && hit.transform != target)
            return from + dir * Mathf.Max(0.3f, hit.distance - wallPadding);
        return from + dir * distance;
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
