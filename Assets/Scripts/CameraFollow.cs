using UnityEngine;
using UnityEngine.InputSystem;

// 3인칭 카메라: 마우스 버튼을 꾹 누른 채 움직이면 구구 주변을 빙 돈다.
// 버튼을 떼면 화면이 멈춘다 (어지럽지 않게).
// 벽이 사이에 끼면 벽 앞으로 당겨 와서 구구가 가려지지 않게 한다.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상. 비워 두면 Player 태그가 붙은 물체를 찾는다.")]
    public Transform target;
    [Tooltip("구구와 카메라 사이 거리 (m)")]
    public float distance = 3.4f;
    [Tooltip("바라볼 높이 (구구 머리쯤)")]
    public float lookHeight = 0.3f;

    [Header("마우스")]
    [Tooltip("마우스 감도 (픽셀당 도). 크면 화면이 빨리 돌아 어지럽다.")]
    public float mouseSensitivity = 0.18f;
    [Tooltip("위아래로 볼 수 있는 범위 (도). 0 = 옆에서, 70 = 위에서 내려다봄")]
    public float minPitch = 0f;
    public float maxPitch = 70f;
    [Tooltip("돌아가는 부드러움. 작을수록 천천히 따라 돈다.")]
    public float turnSmooth = 15f;

    [Header("부드러움 · 벽")]
    [Tooltip("따라가는 부드러움. 작을수록 빨리 붙는다.")]
    public float smoothTime = 0.08f;
    [Tooltip("벽과 카메라 사이에 둘 여유 (m)")]
    public float wallPadding = 0.2f;

    float yaw;          // 지금 좌우 각도
    float pitch = 28f;  // 지금 위아래 각도 (처음엔 비스듬히 내려다봄)
    float targetYaw;    // 마우스가 원하는 각도 → 지금 각도가 천천히 따라간다
    float targetPitch = 28f;
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

    void Update()
    {
        bool over = GameSession.Instance != null && GameSession.Instance.IsOver;
        var mouse = Mouse.current;

        // 왼쪽이나 오른쪽 버튼을 누르고 있는 동안만 돌린다. 게임이 끝나면 버튼을 누를 수 있게 안 돌린다.
        bool dragging = !over && mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed);
        // 누르는 동안엔 커서를 숨기고 가둬서 창 밖으로 안 나가게, 떼면 다시 보이게
        SetCursorLocked(dragging);

        if (dragging)
        {
            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            targetYaw += delta.x;
            targetPitch = Mathf.Clamp(targetPitch - delta.y, minPitch, maxPitch);
        }

        // 휙 돌지 않고 부드럽게 따라 돌기
        float t = 1f - Mathf.Exp(-turnSmooth * Time.deltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);
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
        yaw = targetYaw = target.eulerAngles.y;
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
