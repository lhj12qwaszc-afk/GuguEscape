using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 게임 화면 UI: 과자 개수, 남은 시간, 잠깐 뜨는 알림, 승리/실패 결과 화면.
// GameSession에 붙어서 방을 옮겨도 사라지지 않는다. 화면은 코드로 만든다.
public class GameHud : MonoBehaviour
{
    Font font;
    Text snackText, timeText, toastText, resultTitle, resultSub;
    GameObject resultPanel;
    CanvasGroup toastGroup;
    float toastTimer;
    int shownSnacks;

    static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.45f);
    static readonly Color SnackColor = new Color(0.88f, 0.66f, 0.29f); // 과자색 #E0A84A
    static readonly Color WarnColor = new Color(1f, 0.42f, 0.36f);     // 30초 남으면 빨갛게

    void Start()
    {
        // 한글이 나오도록 윈도우 기본 한글 글꼴(맑은 고딕)을 쓴다
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 32);
        BuildUi();
        EnsureEventSystem();
    }

    void Update()
    {
        var s = GameSession.Instance;
        if (s == null) return;

        // 위쪽: 과자 개수, 남은 시간
        snackText.text = $"과자  {s.SnackCount} / {GameSession.TotalSnacks}";
        int sec = Mathf.CeilToInt(s.TimeLeft);
        timeText.text = $"남은 시간  {sec / 60}:{sec % 60:00}";
        timeText.color = s.TimeLeft <= 30f ? WarnColor : Color.white;

        // 과자를 먹으면 가운데에 잠깐 알림
        if (s.SnackCount != shownSnacks)
        {
            shownSnacks = s.SnackCount;
            if (shownSnacks > 0)
                ShowToast(s.AllSnacksCollected
                    ? "과자를 다 모았다! 가운데 방 햇빛 아래로 가자"
                    : $"과자를 찾았다! ({shownSnacks}/{GameSession.TotalSnacks})");
        }
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f); // 마지막 0.5초 동안 흐려짐
        }

        // 끝나면 결과 화면
        if (s.IsOver && !resultPanel.activeSelf)
        {
            int used = Mathf.FloorToInt(s.timeLimit - s.TimeLeft);
            resultTitle.text = s.IsWin ? "탈출 성공!" : "시간 초과…";
            resultTitle.color = s.IsWin ? SnackColor : WarnColor;
            resultSub.text = s.IsWin
                ? $"과자 {s.SnackCount}/{GameSession.TotalSnacks} · 걸린 시간 {used / 60}:{used % 60:00}"
                : $"과자 {s.SnackCount}/{GameSession.TotalSnacks}개를 모았어요";
            resultPanel.SetActive(true);
        }
        else if (!s.IsOver && resultPanel.activeSelf)
        {
            resultPanel.SetActive(false);
        }
    }

    void ShowToast(string message)
    {
        toastText.text = message;
        toastTimer = 2.5f;
        toastGroup.alpha = 1f;
    }

    // ---------- 화면 만들기 ----------

    void BuildUi()
    {
        var canvasGo = new GameObject("HUD");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // 화면 깜깜(Fader, 999)보다는 아래
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 창 크기가 바뀌어도 비율 유지
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        // 왼쪽 위: 과자
        var snackBox = Box(root, "SnackBox", new Vector2(0, 1), new Vector2(30, -30), new Vector2(360, 80), PanelColor);
        snackText = Label(snackBox, "SnackText", 40, SnackColor);

        // 오른쪽 위: 시간
        var timeBox = Box(root, "TimeBox", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(420, 80), PanelColor);
        timeText = Label(timeBox, "TimeText", 40, Color.white);

        // 가운데 위: 잠깐 뜨는 알림
        var toast = Box(root, "Toast", new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1000, 90), PanelColor);
        toastText = Label(toast, "ToastText", 38, Color.white);
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;

        // 결과 화면 (처음엔 숨김)
        var panel = Box(root, "ResultPanel", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.7f));
        var pr = (RectTransform)panel;
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
        resultPanel = panel.gameObject;

        resultTitle = Label(Box(panel, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1200, 160), Color.clear), "TitleText", 96, SnackColor);
        resultTitle.fontStyle = FontStyle.Bold;
        resultSub = Label(Box(panel, "Sub", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1200, 80), Color.clear), "SubText", 40, Color.white);

        var buttonBox = Box(panel, "RestartButton", new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(380, 110), SnackColor);
        var button = buttonBox.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonBox.GetComponent<Image>();
        button.onClick.AddListener(() => { if (GameSession.Instance != null) GameSession.Instance.Restart(); });
        Label(buttonBox, "ButtonText", 46, new Color(0.2f, 0.12f, 0.05f)).text = "다시하기";

        resultPanel.SetActive(false);
    }

    // 버튼을 누를 수 있게 EventSystem을 하나 만든다 (새 Input System용)
    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.transform.SetParent(transform, false);
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    // 색 있는 네모 칸. anchor 기준으로 offset만큼 떨어진 곳에 size 크기로 놓는다.
    RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f && name == "RestartButton";
        return rt;
    }

    // 칸을 꽉 채우는 가운데 정렬 글자
    Text Label(Transform parent, string name, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
        return t;
    }
}
