using UnityEngine;
using UnityEngine.InputSystem;

// 구구 조작: WASD 이동, Space 점프, 중력
// 날개를 다친 설정이라 날 수 없고, 짧게 점프만 할 수 있다.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [Tooltip("걷는 속도 (m/초). 구구 키가 0.3m라서 느리게 잡았다.")]
    public float moveSpeed = 1.5f;
    [Tooltip("몸을 돌리는 속도 (도/초)")]
    public float turnSpeed = 720f;

    [Header("점프 · 중력")]
    [Tooltip("점프 높이 (m). 벽(3m)은 못 넘고 상자(0.5m)는 못 올라갈 정도")]
    public float jumpHeight = 0.35f;
    public float gravity = -9.81f;

    CharacterController controller;
    Transform cam;
    float verticalSpeed;   // 위아래 속도 (점프 + 중력)

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1) WASD 입력 읽기
        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1;
        if (keyboard.sKey.isPressed) input.y -= 1;
        if (keyboard.dKey.isPressed) input.x += 1;
        if (keyboard.aKey.isPressed) input.x -= 1;
        input = Vector2.ClampMagnitude(input, 1f);

        // 2) 카메라가 보는 방향 기준으로 앞/옆 방향 만들기 (위아래는 빼고)
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();
        Vector3 move = forward * input.y + right * input.x;

        // 3) 움직이는 쪽으로 몸 돌리기
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // 4) 바닥에 있으면 점프 가능, 공중이면 중력으로 떨어짐
        if (controller.isGrounded)
        {
            if (verticalSpeed < 0) verticalSpeed = -1f; // 바닥에 붙어 있게 살짝 누름
            if (keyboard.spaceKey.wasPressedThisFrame)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity); // 원하는 높이까지 올라가는 속도
        }
        verticalSpeed += gravity * Time.deltaTime;

        // 5) 실제로 움직이기
        Vector3 velocity = move * moveSpeed + Vector3.up * verticalSpeed;
        controller.Move(velocity * Time.deltaTime);
    }
}
