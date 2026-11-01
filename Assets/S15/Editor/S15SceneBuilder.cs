using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 메뉴 CG > S15 장면 만들기: S14_Landscape를 S15_Landscape로 저장하고 흐르는 물 2곳을 설정함.
// 다시 실행해도 됨: 재질 값은 아래 값으로 되돌아가고, Puddle_Flow는 새로 만듦.
public static class S15SceneBuilder
{
    const string Folder = "Assets/S15";
    const string S14ScenePath = "Assets/S14/S14_Landscape.unity";
    const string ScenePath = Folder + "/S15_Landscape.unity";
    const string GraphPath = Folder + "/S15_SG_Flow.shadergraph";
    const string TextureFolder = "Assets/ThirdParty/Textures";
    const string CustomTextureName = "flow_custom";

    [MenuItem("CG/S15 장면 만들기")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[S15] Play 모드를 끄고 실행하세요.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("[S15] 지금 씬을 Ctrl+S로 저장한 뒤 다시 실행하세요.");
                return;
            }

            // S15 씬이 아직 없으면 S14 씬을 복사해서 만듦
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                AssetDatabase.CopyAsset(S14ScenePath, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        GameObject props = GameObject.Find("S14_Props");
        if (props == null)
        {
            Debug.LogError("[S15] 씬에 S14_Props가 없습니다.");
            return;
        }

        SnapPropsToFloor(props.transform);
        SetupTableFlow(props.transform);
        SetupPuddle(props.transform);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Lightmapping.bakeCompleted += OnBakeCompleted;
        if (!Lightmapping.BakeAsync())
        {
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Debug.LogWarning("[S15] Generate Lighting을 시작하지 못했습니다. Lighting 창에서 직접 눌러 주세요.");
        }

        Debug.Log("[S15] 장면 구성 완료.");
    }

    static void OnBakeCompleted()
    {
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[S15] Generate Lighting 완료.");
    }

    // S14_Props 아래쪽의 정자 바닥 높이를 찾아 테이블을 올려 둠. 지붕에 닿은 값은 쓰지 않음
    static void SnapPropsToFloor(Transform props)
    {
        Physics.SyncTransforms();
        Vector3 origin = new Vector3(props.position.x, 20f, props.position.z);
        RaycastHit[] floorHits = Physics.RaycastAll(origin, Vector3.down, 40f)
            .Where(h => h.collider.transform.root.name.ToLowerInvariant().Contains("pavilion"))
            .OrderBy(h => h.point.y)
            .ToArray();

        if (floorHits.Length == 0 || floorHits[0].point.y > 2f)
        {
            Debug.LogWarning("[S15] 정자 바닥을 찾지 못해 S14_Props 높이를 그대로 둡니다.");
            return;
        }

        Vector3 position = props.position;
        position.y = floorHits[0].point.y;
        props.position = position;
        Debug.Log($"[S15] S14_Props 높이: {position.y:F2}");
    }

    // 실습 6: 테이블 상판에 두 층이 엇갈려 흐르는 물
    static void SetupTableFlow(Transform props)
    {
        Transform top = props.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Top");
        if (top == null)
        {
            Debug.LogWarning("[S15] 테이블 상판 Top을 찾지 못했습니다.");
            return;
        }

        Texture2D water1 = LoadTexture(TextureFolder + "/water1.png");
        Texture2D water2 = LoadTexture(TextureFolder + "/water2.png");

        // 상판 Scale (2, 0.1, 1.2)에 맞춘 Tiling
        Material mat = FlowMaterial("Mat_S15_SG_Flow", water1, water2,
            new Vector2(0f, 0.05f), new Vector2(0.02f, 0.1f), new Vector2(2f, 1.2f));
        top.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // 과제 3번: 직접 고른 텍스처로 바닥의 물웅덩이를 하나 더 만듦
    static void SetupPuddle(Transform props)
    {
        Transform old = props.Find("Puddle_Flow");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        string guid = AssetDatabase.IsValidFolder(TextureFolder)
            ? AssetDatabase.FindAssets($"{CustomTextureName} t:Texture2D", new[] { TextureFolder }).FirstOrDefault()
            : null;
        if (guid == null)
        {
            Debug.LogWarning($"[S15] {TextureFolder}에 {CustomTextureName} 텍스처가 없습니다. 넣은 뒤 다시 실행하세요.");
            return;
        }

        Texture2D custom = LoadTexture(AssetDatabase.GUIDToAssetPath(guid));
        Material mat = FlowMaterial("Mat_S15_SG_Flow_Puddle", custom, custom,
            new Vector2(0.03f, -0.02f), new Vector2(-0.01f, 0.04f), new Vector2(2f, 2f));

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Puddle_Flow";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(props, false);
        quad.transform.localPosition = new Vector3(-0.9f, 0.02f, -0.75f);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material FlowMaterial(string name, Texture2D tex1, Texture2D tex2, Vector2 flow1, Vector2 flow2, Vector2 tiling)
    {
        string path = $"{Folder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(AssetDatabase.LoadAssetAtPath<Shader>(GraphPath));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetTexture("_MainTex1", tex1);
        mat.SetTexture("_MainTex2", tex2);
        mat.SetVector("_Flow1", flow1);
        mat.SetVector("_Flow2", flow2);
        mat.SetVector("_Tiling", tiling);
        mat.SetFloat("_Blend", 0.5f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 슬라이드의 임포트 설정: Wrap Mode Repeat, Max Size 1024
    static Texture2D LoadTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[S15] {path}를 찾지 못했습니다.");
            return null;
        }

        if (importer.wrapMode != TextureWrapMode.Repeat || importer.maxTextureSize != 1024)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
