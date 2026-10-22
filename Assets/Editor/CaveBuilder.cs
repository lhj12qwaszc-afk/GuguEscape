using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 동굴 6개 씬(가운데 방 + 방 5개)을 임시 도형으로 한 번에 만드는 에디터 도구.
// 메뉴: Tools → Gugu → Build Cave Scenes
// 주의: 다시 실행하면 6개 씬을 처음부터 새로 만든다 (직접 고친 내용은 사라짐).
public static class CaveBuilder
{
    const string ScenesDir = "Assets/Scenes/";
    const string MatDir = "Assets/Materials/";
    const string PrefabDir = "Assets/Prefabs/";

    [MenuItem("Tools/Gugu/Build Cave Scenes")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureMaterial("M_DoorDark", "#050407");
        GameObject player = BuildPlayerPrefab();
        GameObject cam = BuildCameraPrefab();

        BuildHub(player, cam);
        BuildRoom1(player, cam);
        BuildRoom2(player, cam);
        BuildRoom3(player, cam);
        BuildRoom4(player, cam);
        BuildRoom5(player, cam);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesDir + "Cave_Hub.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Cave_Room1.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Cave_Room2.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Cave_Room3.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Cave_Room4.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Cave_Room5.unity", true),
        };
        EditorSceneManager.OpenScene(ScenesDir + "Cave_Hub.unity");
        Debug.Log("동굴 씬 6개를 만들었다.");
    }

    // ---------- 가운데 방 (시작 · 탈출, 입구 5개) ----------

    static void BuildHub(GameObject player, GameObject cam)
    {
        var scene = NewCaveScene("#1E1A2E");
        Groups(out var env, out var props, out var lighting, out var gameplay);
        Shell(env, 12f, 12f, 4f);

        // 천장: 가운데 1.6m 구멍만 남기고 덮는다. 카메라에는 안 보이고 그림자만 진다.
        Ceiling(env, "Ceiling_Back", new Vector3(0, 4.05f, -3.4f), new Vector3(12, 0.1f, 5.2f));
        Ceiling(env, "Ceiling_Front", new Vector3(0, 4.05f, 3.4f), new Vector3(12, 0.1f, 5.2f));
        Ceiling(env, "Ceiling_Left", new Vector3(-3.4f, 4.05f, 0), new Vector3(5.2f, 0.1f, 1.6f));
        Ceiling(env, "Ceiling_Right", new Vector3(3.4f, 4.05f, 0), new Vector3(5.2f, 0.1f, 1.6f));

        // 입구 5개: 북쪽 2개, 동쪽, 남쪽, 서쪽. 돌아올 때는 각 입구 앞에 나타난다.
        Door(env, "Door_Room1", new Vector3(-3, 1, 5.72f), 0, "Cave_Room1", "FromHub");
        Door(env, "Door_Room2", new Vector3(3, 1, 5.72f), 0, "Cave_Room2", "FromHub");
        Door(env, "Door_Room3", new Vector3(5.72f, 1, 0), 90, "Cave_Room3", "FromHub");
        Door(env, "Door_Room4", new Vector3(0, 1, -5.72f), 180, "Cave_Room4", "FromHub");
        Door(env, "Door_Room5", new Vector3(-5.72f, 1, 0), 270, "Cave_Room5", "FromHub");
        Spawn(gameplay, "FromRoom1", new Vector3(-3, 0.5f, 4.4f), 180);
        Spawn(gameplay, "FromRoom2", new Vector3(3, 0.5f, 4.4f), 180);
        Spawn(gameplay, "FromRoom3", new Vector3(4.4f, 0.5f, 0), 270);
        Spawn(gameplay, "FromRoom4", new Vector3(0, 0.5f, -4.4f), 0);
        Spawn(gameplay, "FromRoom5", new Vector3(-4.4f, 0.5f, 0), 90);

        Vector3[] pillars = { new(-4, 0, 2.5f), new(4, 0, 2.5f), new(-4, 0, -2.5f), new(4, 0, -2.5f) };
        for (int i = 0; i < pillars.Length; i++)
        {
            Pillar(props, "TorchPillar_" + (i + 1), pillars[i], 1.5f);
            Torch(lighting, "TorchLight_" + (i + 1), pillars[i] + Vector3.up * 1.85f);
        }
        Box(props, "Crate_1", new Vector3(2.2f, 0.25f, -3.2f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_2", new Vector3(-2.4f, 0.25f, -2.8f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_3", new Vector3(-4.8f, 0.25f, 4.8f), Vector3.one * 0.5f, "M_Crate");

        // 천장 구멍: 평소엔 희미한 빛, 과자를 다 모으면 밝은 햇빛(ExitZone이 켠다)
        var glow = SpotLight(lighting, "HoleGlow", new Vector3(0, 4.6f, 0), "#8FA3C9", 3f, 45f);
        var sun = SpotLight(lighting, "Sunlight", new Vector3(0, 4.6f, 0), "#FFF1C2", 25f, 45f);
        sun.enabled = false;
        glow.shadows = LightShadows.None;

        var exit = new GameObject("ExitZone");
        exit.transform.SetParent(gameplay, false);
        exit.transform.localPosition = new Vector3(0, 0.3f, 0);
        var sc = exit.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.7f;
        exit.AddComponent<ExitZone>().sunlight = sun;

        // 시작: 천장 구멍 바로 아래 공중에서 떨어진다
        Spawn(gameplay, "Start", new Vector3(0, 3.4f, 0), 0);
        PlacePlayer(player, cam, gameplay, new Vector3(0, 3.4f, 0), 0);
        Save(scene, "Cave_Hub");
    }

    // ---------- 방 1: 상자 창고 (상자 더미 뒤 구석) ----------

    static void BuildRoom1(GameObject player, GameObject cam)
    {
        var scene = RoomBase(player, cam, 8f, 8f, 1, out var props, out var lighting, out var gameplay);
        Box(props, "CrateStack_A", new Vector3(2.3f, 0.5f, 3.3f), new Vector3(0.7f, 1f, 0.7f), "M_Crate");
        Box(props, "CrateStack_B", new Vector3(3.4f, 0.5f, 1.9f), new Vector3(0.7f, 1f, 0.7f), "M_Crate");
        Box(props, "CrateStack_C", new Vector3(1.6f, 0.5f, 2.2f), new Vector3(0.7f, 1f, 0.7f), "M_Crate");
        Box(props, "Crate_1", new Vector3(-2f, 0.25f, 1f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_2", new Vector3(-2.6f, 0.25f, 1.7f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_3", new Vector3(0.8f, 0.25f, -1f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_Big", new Vector3(-3f, 0.5f, -2.5f), Vector3.one, "M_Crate");
        Torch(lighting, "TorchLight_1", new Vector3(-3.2f, 2f, -3.2f));
        Torch(lighting, "TorchLight_2", new Vector3(3.2f, 2f, -0.5f));
        SnackAt(gameplay, "Room1", new Vector3(3.3f, 0.4f, 3.3f), false);
        Save(scene, "Cave_Room1");
    }

    // ---------- 방 2: 기둥 숲 (맨 뒤 기둥 뒤) ----------

    static void BuildRoom2(GameObject player, GameObject cam)
    {
        var scene = RoomBase(player, cam, 9f, 9f, 2, out var props, out var lighting, out var gameplay);
        int n = 1;
        foreach (float z in new[] { -1.5f, 0.5f, 2.5f })
            foreach (float x in new[] { -2.5f, 0f, 2.5f })
                Pillar(props, "Pillar_" + n++, new Vector3(x, 0, z), 2f, 0.7f);
        Torch(lighting, "TorchLight_1", new Vector3(-2.5f, 2.3f, -1.5f));
        Torch(lighting, "TorchLight_2", new Vector3(2.5f, 2.3f, 0.5f));
        Torch(lighting, "TorchLight_3", new Vector3(-2.5f, 2.3f, 2.5f));
        SnackAt(gameplay, "Room2", new Vector3(0f, 0.4f, 3.5f), false);
        Save(scene, "Cave_Room2");
    }

    // ---------- 방 3: 높은 선반 (상자 계단을 점프로 올라가기) ----------

    static void BuildRoom3(GameObject player, GameObject cam)
    {
        var scene = RoomBase(player, cam, 8f, 8f, 3, out var props, out var lighting, out var gameplay);
        Box(props, "Step_1", new Vector3(-1.2f, 0.25f, 1.2f), new Vector3(0.9f, 0.5f, 0.9f), "M_Crate");
        Box(props, "Step_2", new Vector3(-2.2f, 0.5f, 2.2f), new Vector3(0.9f, 1f, 0.9f), "M_Crate");
        Box(props, "Step_3", new Vector3(-3.2f, 0.75f, 3.2f), new Vector3(0.9f, 1.5f, 0.9f), "M_Crate");
        Box(props, "Crate_1", new Vector3(2.5f, 0.25f, 2.8f), Vector3.one * 0.5f, "M_Crate");
        Box(props, "Crate_2", new Vector3(3f, 0.25f, -2f), Vector3.one * 0.5f, "M_Crate");
        Torch(lighting, "TorchLight_1", new Vector3(-3.2f, 2.6f, 1.5f));
        Torch(lighting, "TorchLight_2", new Vector3(3f, 2f, 1f));
        SnackAt(gameplay, "Room3", new Vector3(-3.2f, 1.9f, 3.2f), false);
        Save(scene, "Cave_Room3");
    }

    // ---------- 방 4: 어두운 방 (낮은 벽 뒤에서 과자만 살짝 빛남) ----------

    static void BuildRoom4(GameObject player, GameObject cam)
    {
        var scene = RoomBase(player, cam, 8f, 8f, 4, out var props, out var lighting, out var gameplay, "#0A0910");
        Box(props, "LowWall_1", new Vector3(-2.4f, 0.5f, 2.3f), new Vector3(3.2f, 1f, 0.3f), "M_Wall");
        Box(props, "LowWall_2", new Vector3(1.6f, 0.5f, 0.2f), new Vector3(0.3f, 1f, 3f), "M_Wall");
        Box(props, "Crate_1", new Vector3(2.8f, 0.25f, 2.8f), Vector3.one * 0.5f, "M_Crate");
        AddLight(lighting, "DimLight", new Vector3(0, 1.5f, -3f), "#FF8A3D", 1f, 3f);
        SnackAt(gameplay, "Room4", new Vector3(-3.2f, 0.4f, 3.2f), true);
        Save(scene, "Cave_Room4");
    }

    // ---------- 방 5: 미로 (낮은 벽 사이 막다른 곳) ----------

    static void BuildRoom5(GameObject player, GameObject cam)
    {
        var scene = RoomBase(player, cam, 9f, 9f, 5, out var props, out var lighting, out var gameplay);
        Box(props, "MazeWall_1", new Vector3(-1.175f, 0.75f, -1.5f), new Vector3(6.35f, 1.5f, 0.3f), "M_Wall");
        Box(props, "MazeWall_2", new Vector3(1.175f, 0.75f, 1.5f), new Vector3(6.35f, 1.5f, 0.3f), "M_Wall");
        Box(props, "MazeWall_3", new Vector3(-1f, 0.75f, 3.3f), new Vector3(0.3f, 1.5f, 2.1f), "M_Wall");
        Torch(lighting, "TorchLight_1", new Vector3(3.5f, 2f, -3f));
        Torch(lighting, "TorchLight_2", new Vector3(3.5f, 2f, 0f));
        Torch(lighting, "TorchLight_3", new Vector3(-3.5f, 2f, 2.5f));
        SnackAt(gameplay, "Room5", new Vector3(-3.5f, 0.4f, 3.5f), false);
        Save(scene, "Cave_Room5");
    }

    // ---------- 공통: 방 껍데기 + 돌아가는 입구 + 구구 ----------

    static UnityEngine.SceneManagement.Scene RoomBase(GameObject player, GameObject cam, float w, float d, int roomNo,
        out Transform props, out Transform lighting, out Transform gameplay, string ambient = "#1E1A2E")
    {
        var scene = NewCaveScene(ambient);
        Groups(out var env, out props, out lighting, out gameplay);
        Shell(env, w, d, 3.5f);
        // 남쪽 벽 입구 → 가운데 방의 이 방 입구 앞으로 돌아간다
        Door(env, "Door_Hub", new Vector3(0, 1, -d / 2 + 0.28f), 180, "Cave_Hub", "FromRoom" + roomNo);
        Spawn(gameplay, "FromHub", new Vector3(0, 0.5f, -d / 2 + 1.3f), 0);
        PlacePlayer(player, cam, gameplay, new Vector3(0, 0.5f, -d / 2 + 1.3f), 0);
        return scene;
    }

    static UnityEngine.SceneManagement.Scene NewCaveScene(string ambientHex)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Hex(ambientHex);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile != null)
        {
            var vol = new GameObject("Global Volume").AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = profile;
        }
        return scene;
    }

    static void Groups(out Transform env, out Transform props, out Transform lighting, out Transform gameplay)
    {
        env = new GameObject("Environment").transform;
        props = new GameObject("Props").transform;
        lighting = new GameObject("Lighting").transform;
        gameplay = new GameObject("Gameplay").transform;
    }

    static void Shell(Transform env, float w, float d, float h)
    {
        Box(env, "Floor", new Vector3(0, -0.05f, 0), new Vector3(w, 0.1f, d), "M_Floor");
        Box(env, "Wall_North", new Vector3(0, h / 2, d / 2), new Vector3(w, h, 0.3f), "M_Wall");
        Box(env, "Wall_South", new Vector3(0, h / 2, -d / 2), new Vector3(w, h, 0.3f), "M_Wall");
        Box(env, "Wall_East", new Vector3(w / 2, h / 2, 0), new Vector3(0.3f, h, d), "M_Wall");
        Box(env, "Wall_West", new Vector3(-w / 2, h / 2, 0), new Vector3(0.3f, h, d), "M_Wall");
    }

    static void PlacePlayer(GameObject playerPrefab, GameObject camPrefab, Transform gameplay, Vector3 pos, float rotY)
    {
        var p = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        p.transform.SetParent(gameplay, false);
        p.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
        var c = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab);
        c.transform.position = pos + new Vector3(0, 1.6f, -3f);
        c.transform.LookAt(pos + Vector3.up * 0.3f);
    }

    // ---------- 부품 ----------

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, string mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
        return go;
    }

    static void Ceiling(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = Box(parent, name, pos, scale, "M_Wall");
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    static void Pillar(Transform parent, string name, Vector3 basePos, float height, float width = 0.5f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = basePos + Vector3.up * (height / 2);
        go.transform.localScale = new Vector3(width, height / 2, width); // 원기둥은 기본 높이가 2m
        go.GetComponent<Renderer>().sharedMaterial = Mat("M_Pillar_Temp");
    }

    static void Door(Transform parent, string name, Vector3 pos, float rotY, string target, string spawnId)
    {
        var go = Box(parent, name, pos, new Vector3(1.4f, 2f, 0.25f), "M_DoorDark");
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var door = go.AddComponent<Door>();
        door.targetScene = target; door.spawnId = spawnId;
        // 입구 위 작은 불빛으로 문 위치를 알려 준다
        var lamp = new GameObject("DoorLamp");
        lamp.transform.SetParent(go.transform, false);
        lamp.transform.localPosition = new Vector3(0, 0.75f, -2.5f);
        var l = lamp.AddComponent<Light>();
        l.type = LightType.Point; l.color = Hex("#FFB066"); l.intensity = 1.5f; l.range = 2.5f;
    }

    static void Spawn(Transform parent, string id, Vector3 pos, float rotY)
    {
        var go = new GameObject("Spawn_" + id);
        go.transform.SetParent(parent, false);
        go.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
        go.AddComponent<SpawnPoint>().id = id;
    }

    static void SnackAt(Transform parent, string id, Vector3 pos, bool glow)
    {
        var root = new GameObject("Snack_" + id);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        var col = root.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.25f;
        root.AddComponent<Snack>().snackId = id;
        // 보이는 모양: 동전처럼 세운 납작한 원기둥 (진짜 과자 FBX로 바꿀 자리)
        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Visual";
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.transform.SetParent(root.transform, false);
        vis.transform.localRotation = Quaternion.Euler(90, 0, 0);
        vis.transform.localScale = new Vector3(0.25f, 0.03f, 0.25f);
        vis.GetComponent<Renderer>().sharedMaterial = Mat("M_Snack_Temp");
        if (glow)
        {
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.type = LightType.Point; l.color = Hex("#FFD27A"); l.intensity = 1.5f; l.range = 1.5f;
        }
    }

    static void Torch(Transform parent, string name, Vector3 pos) => AddLight(parent, name, pos, "#FF8A3D", 2.5f, 4f);

    static Light AddLight(Transform parent, string name, Vector3 pos, string hex, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = Hex(hex); l.intensity = intensity; l.range = range;
        return l;
    }

    static Light SpotLight(Transform parent, string name, Vector3 pos, string hex, float intensity, float angle)
    {
        var l = AddLight(parent, name, pos, hex, intensity, 6f);
        l.type = LightType.Spot; l.spotAngle = angle; l.innerSpotAngle = angle * 0.6f;
        l.shadows = LightShadows.Soft;
        l.transform.localRotation = Quaternion.Euler(90, 0, 0);
        return l;
    }

    // ---------- 프리팹 ----------

    static GameObject BuildPlayerPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.tag = "Player";
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f); // 키 0.6m (임시 구구)
        go.GetComponent<Renderer>().sharedMaterial = Mat("M_Player_Temp");
        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
        cc.skinWidth = 0.02f; cc.stepOffset = 0.1f; cc.minMoveDistance = 0f;
        go.AddComponent<PlayerController>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "Player.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildCameraPrefab()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#0D0B14");
        cam.nearClipPlane = 0.05f;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFollow>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "PlayerCamera.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- 도우미 ----------

    static void Save(UnityEngine.SceneManagement.Scene scene, string name) =>
        EditorSceneManager.SaveScene(scene, ScenesDir + name + ".unity");

    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");

    static void EnsureMaterial(string name, string hex)
    {
        if (Mat(name) != null) return;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hex(hex));
        m.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(m, MatDir + name + ".mat");
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
