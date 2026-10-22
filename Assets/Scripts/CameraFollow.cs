using UnityEngine;

// 카메라가 구구 뒤에서 부드럽게 따라가며 구구를 바라본다.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상 (Player)")]
    public Transform target;
    [Tooltip("구구 기준 카메라 위치: 위로 0.85m, 뒤로 1.6m")]
    public Vector3 offset = new Vector3(0f, 0.85f, -1.6f);
    [Tooltip("바라볼 높이 (구구 머리쯤)")]
    public float lookHeight = 0.15f;
    [Tooltip("따라가는 부드러움. 작을수록 빨리 붙는다.")]
    public float smoothTime = 0.15f;

    Vector3 velocity;

    // 구구가 움직인 뒤(Update 다음)에 카메라를 옮겨야 화면이 떨리지 않는다.
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 wanted = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, wanted, ref velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
