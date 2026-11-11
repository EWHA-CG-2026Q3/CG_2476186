using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 메뉴 CG > S16 장면 만들기: S15_Landscape를 S16_Landscape로 저장하고 셰이더 실습 물체를 놓음.
// 다시 실행해도 됨: 재질 값은 아래 값으로 되돌아가고, S16_Shader 묶음은 새로 만듦.
public static class S16SceneBuilder
{
    const string Folder = "Assets/S16";
    const string S15ScenePath = "Assets/S15/S15_Landscape.unity";
    const string ScenePath = Folder + "/S16_Landscape.unity";
    const string TargetCoalPath = "Assets/S14/Mat_S14_Coal.mat";
    const string GroupName = "S16_Shader";
    const string ScreenshotPath = Folder + "/S16_GameView.png";
    const float TableTop = 1.025f;

    [MenuItem("CG/S16 장면 만들기")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[S16] Play 모드를 끄고 실행하세요.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("[S16] 지금 씬을 Ctrl+S로 저장한 뒤 다시 실행하세요.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                AssetDatabase.CopyAsset(S15ScenePath, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        GameObject props = GameObject.Find("S14_Props");
        if (props == null)
        {
            Debug.LogError("[S16] 씬에 S14_Props가 없습니다.");
            return;
        }

        BuildGroup(props.transform);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[S16] 장면 구성 완료.");
    }

    // 배치 모드용: 장면 만들기 + Generate Lighting + Game view 스크린샷
    public static void BuildBakeAndCapture()
    {
        EditorSceneManager.OpenScene(S15ScenePath);
        Build();
        Lightmapping.Bake();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Capture();
    }

    // 배치 모드용: 라이트 굽기 없이 장면만 다시 만들고 스크린샷
    public static void BuildAndCapture()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Build();
        Capture();
    }

    [MenuItem("CG/S16 스크린샷 찍기")]
    public static void Capture()
    {
        Transform cameraTransform = GameObject.Find(GroupName)?.transform.Find("S16_ShotCamera");
        if (cameraTransform == null)
        {
            Debug.LogError("[S16] S16_ShotCamera가 없습니다. 먼저 장면을 만드세요.");
            return;
        }

        // 배치 모드에서는 프레임이 돌지 않아 우리 메시의 Start가 아직 안 불렸을 수 있음
        foreach (MeshFilter filter in cameraTransform.parent.GetComponentsInChildren<MeshFilter>())
            if (filter.sharedMesh == null)
                filter.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

        GameObject go = cameraTransform.gameObject;
        bool wasActive = go.activeSelf;
        go.SetActive(true);
        Camera cam = go.GetComponent<Camera>();

        const int width = 1920, height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        go.SetActive(wasActive);

        File.WriteAllBytes(ScreenshotPath, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(rt);
        AssetDatabase.ImportAsset(ScreenshotPath);
        Debug.Log($"[S16] 스크린샷 저장: {ScreenshotPath}");
    }

    static void BuildGroup(Transform props)
    {
        Transform old = props.Find(GroupName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Transform group = new GameObject(GroupName).transform;
        group.SetParent(props, false);

        // 실습 1: 최소한의 셰이더 (빨간 원판)
        Material myShader = ShaderMaterial("Mat_S16_MyShader", "CG/S16_MyShader");

        // 실습 2: 숯 셰이더. 목표 숯(Mat_S14_Coal)의 Emission 색을 그대로 옮겨 옴
        Material targetCoal = AssetDatabase.LoadAssetAtPath<Material>(TargetCoalPath);
        Color coalGlow = targetCoal != null && targetCoal.HasProperty("_EmissionColor")
            ? targetCoal.GetColor("_EmissionColor")
            : new Color(191f / 255f, 117f / 255f, 3f / 255f) * Mathf.Pow(2f, 5f);
        Material myCoal = CoalMaterial("Mat_S16_MyCoal", Color.black, coalGlow);

        // 과제 3번: 숯과 다른 색의 복제본 2개
        Material jade = CoalMaterial("Mat_S16_MyCoal_Jade", new Color(0.02f, 0.1f, 0.06f), new Color(0.05f, 1.3f, 0.55f));
        Material violet = CoalMaterial("Mat_S16_MyCoal_Violet", new Color(0.08f, 0.02f, 0.12f), new Color(0.8f, 0.15f, 1.4f));

        // 화로 앞쪽 테이블 위: 왼쪽 목표 숯(Lit), 오른쪽 우리 숯 셰이더
        Vector3 ballScale = Vector3.one * 0.14f;
        float ballY = TableTop + 0.07f;
        Sphere("Sphere_TargetCoal", group, new Vector3(-0.15f, ballY, -0.18f), ballScale, targetCoal);
        Sphere("Sphere_MyCoal", group, new Vector3(0.15f, ballY, -0.18f), ballScale, myCoal);

        // 테이블 앞 바닥: 실습 1의 빨간 공과 우리 메시 2개
        Sphere("Sphere_MyShader", group, new Vector3(0f, 0.12f, -0.75f), Vector3.one * 0.24f, myShader);
        MyMesh<S04_CustomDiamondMesh>("Mesh_Diamond_S16", group, new Vector3(-0.55f, 0.25f, -0.8f), 0.25f, jade);
        MyMesh<S12_CustomPyramidMesh>("Mesh_Pyramid_S16", group, new Vector3(0.3f, 0f, -0.85f), 0.25f, violet);

        BuildShotCamera(group);
    }

    // 스크린샷용 카메라. 평소에는 꺼 둠
    static void BuildShotCamera(Transform group)
    {
        GameObject go = new GameObject("S16_ShotCamera");
        go.transform.SetParent(group, false);
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
        cam.allowHDR = true;
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.05f;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        Vector3 target = group.TransformPoint(new Vector3(0f, 0.6f, -0.45f));
        go.transform.position = group.TransformPoint(new Vector3(-0.35f, 1.45f, -2.2f));
        go.transform.LookAt(target);
        go.SetActive(false);
    }

    static Material ShaderMaterial(string name, string shaderName)
    {
        string path = $"{Folder}/{name}.mat";
        Shader shader = Shader.Find(shaderName);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CoalMaterial(string name, Color baseColor, Color emission)
    {
        Material mat = ShaderMaterial(name, "CG/S16_MyCoal");
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void MyMesh<T>(string name, Transform parent, Vector3 position, float scale, Material mat) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<T>();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
