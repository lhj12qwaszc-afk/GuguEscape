using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬(방)이 바뀌어도 사라지지 않는 게임 진행 관리자.
// 모은 과자, 남은 시간, 방 이동(화면 깜깜 → 다른 방)을 맡는다.
public class GameSession : MonoBehaviour
{
    public const int TotalSnacks = 5;
    public const string HubScene = "Cave_Hub";

    public static GameSession Instance { get; private set; }

    [Tooltip("제한 시간 (초)")]
    public float timeLimit = 180f;
    [Tooltip("화면이 깜깜해지는 / 밝아지는 시간 (초)")]
    public float fadeTime = 0.4f;

    public float TimeLeft { get; private set; }
    public int SnackCount => collected.Count;
    public bool AllSnacksCollected => collected.Count >= TotalSnacks;
    public bool IsOver { get; private set; }
    public bool IsWin { get; private set; }
    // 방을 옮기는 중이거나 게임이 끝나면 조작을 막는다
    public bool InputLocked => traveling || IsOver;

    readonly HashSet<string> collected = new HashSet<string>();
    string nextSpawnId;
    Image fadeImage;
    bool traveling;

    // 어느 씬에서 플레이를 시작해도 관리자가 하나 생기게 한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnStart()
    {
        if (Instance == null) new GameObject("GameSession").AddComponent<GameSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        TimeLeft = timeLimit;
        CreateFader();
        gameObject.AddComponent<GameHud>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void Update()
    {
        if (IsOver) return;
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            Lose();
        }
    }

    // ---------- 과자 ----------

    public bool IsCollected(string snackId) => collected.Contains(snackId);

    public void Collect(string snackId)
    {
        if (!collected.Add(snackId)) return;
        Debug.Log($"과자 획득: {snackId} ({SnackCount}/{TotalSnacks})");
        if (AllSnacksCollected) Debug.Log("과자를 다 모았다! 가운데 방 햇빛 아래로 가면 탈출");
    }

    // ---------- 승리 / 실패 ----------

    public void Win()
    {
        if (IsOver) return;
        IsOver = true; IsWin = true;
        Debug.Log("탈출 성공!");
    }

    public void Lose()
    {
        if (IsOver) return;
        IsOver = true; IsWin = false;
        Debug.Log("시간 초과… 실패");
    }

    // 처음부터 다시: 과자·시간을 처음으로 돌리고, 가운데 방 천장 아래에서 다시 떨어진다
    public void Restart()
    {
        if (traveling) return;
        collected.Clear();
        TimeLeft = timeLimit;
        IsOver = false; IsWin = false;
        StartCoroutine(TravelRoutine(HubScene, "Start"));
    }

    // ---------- 방 이동 ----------

    // 문에 들어가면 호출: 깜깜해지고 → 씬을 바꾸고 → spawnId 자리에 구구를 놓고 → 밝아진다
    public void Travel(string sceneName, string spawnId)
    {
        if (traveling || IsOver) return;
        StartCoroutine(TravelRoutine(sceneName, spawnId));
    }

    IEnumerator TravelRoutine(string sceneName, string spawnId)
    {
        traveling = true;
        yield return Fade(0f, 1f);
        nextSpawnId = spawnId;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return null; // 새 씬의 Start가 한 번 돌고 나서
        yield return Fade(1f, 0f);
        traveling = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(nextSpawnId)) return;
        PlacePlayer(nextSpawnId);
        nextSpawnId = null;
    }

    void PlacePlayer(string spawnId)
    {
        SpawnPoint spawn = null;
        foreach (var s in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            if (s.id == spawnId) { spawn = s; break; }
        var player = GameObject.FindWithTag("Player");
        if (spawn == null || player == null) return;

        // CharacterController가 켜져 있으면 위치를 바로 못 바꾸므로 잠깐 끈다
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        if (cc != null) cc.enabled = true;

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.SnapToTarget();
    }

    // ---------- 화면 깜깜하게 ----------

    void CreateFader()
    {
        var canvasGo = new GameObject("Fader");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 다른 UI보다 위

        var imgGo = new GameObject("Black");
        imgGo.transform.SetParent(canvasGo.transform, false);
        fadeImage = imgGo.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, to);
    }
}
