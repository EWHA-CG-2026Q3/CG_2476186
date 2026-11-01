using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 메뉴 CG > S14 장면 만들기: S14_Landscape에 밤 환경, 화로, Bloom, 연기, 재질을 한 번에 설정함.
// 다시 실행해도 됨: S14_Props는 위치를 유지한 채 새로 만들고, 재질 값은 아래 값으로 되돌아감.
public static class S14SceneBuilder
{
    const string Folder = "Assets/S14";
    const string ScenePath = Folder + "/S14_Landscape.unity";
    const string TextureFolder = "Assets/ThirdParty/Textures";
    const string PavilionPrefab = "Assets/ThirdParty/Pavilion/Prefabs/Pavilion.prefab";
    const string PropsName = "S14_Props";

    // S02 테이블은 원래 높이가 약 2라서 절반으로 줄여 씀. 상판 윗면의 높이
    const float TableTop = 1.025f;

    static readonly Color NightColor = new Color(0.03f, 0.04f, 0.09f);

    [MenuItem("CG/S14 장면 만들기")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[S14] Play 모드를 끄고 실행하세요.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError($"[S14] {ScenePath} 씬을 연 뒤 실행하세요.");
            return;
        }

        SetupNight();
        SetupGround();
        SetupPavilionColliders();
        SetupPlayer();
        Transform props = SetupProps();
        SetupBloom();
        SetupShotCamera(props);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Generate Lighting: 끝나면 조명 데이터 연결을 씬에 저장함
        Lightmapping.bakeCompleted += OnBakeCompleted;
        if (!Lightmapping.BakeAsync())
        {
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Debug.LogWarning("[S14] Generate Lighting을 시작하지 못했습니다. Lighting 창에서 직접 눌러 주세요.");
        }

        Debug.Log("[S14] 장면 구성 완료. S14_Props를 정자 안으로 옮기세요.");
    }

    static void OnBakeCompleted()
    {
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[S14] Generate Lighting 완료.");
    }

    // 낮을 밤으로: 해, 주변광, 반사, 안개, 카메라 배경
    static void SetupNight()
    {
        GameObject sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            sun.GetComponent<Light>().color = Color.black;
        }

        string lightingPath = Folder + "/S14_LightingSettings.lighting";
        LightingSettings lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingPath);
        if (lighting == null)
        {
            lighting = new LightingSettings();
            AssetDatabase.CreateAsset(lighting, lightingPath);
        }
        lighting.bakedGI = false;
        lighting.realtimeGI = false;
        EditorUtility.SetDirty(lighting);
        Lightmapping.lightingSettings = lighting;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.12f);
        RenderSettings.reflectionIntensity = 0.4f;

        RenderSettings.fog = true;
        RenderSettings.fogColor = NightColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.03f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NightColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
    }

    static void SetupGround()
    {
        if (GameObject.Find("Ground") != null)
            return;

        Material mat = Lit("Mat_S14_Ground", new Color(0.2f, 0.2f, 0.2f), 0f, 0.1f);
        Primitive(PrimitiveType.Plane, "Ground", null, Vector3.zero, new Vector3(5f, 1f, 5f), mat);
    }

    // 정자 바닥과 계단을 밟을 수 있도록 Mesh Collider 추가
    static void SetupPavilionColliders()
    {
        GameObject pavilion = SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(g => g.name.ToLowerInvariant().Contains("pavilion"));
        if (pavilion == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PavilionPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[S14] {PavilionPrefab}이 없습니다. Pavilion을 ThirdParty로 옮긴 뒤 다시 실행하세요.");
                return;
            }

            pavilion = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            pavilion.transform.position = Vector3.zero;
        }

        foreach (MeshFilter filter in pavilion.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh != null && filter.GetComponent<Collider>() == null)
                filter.gameObject.AddComponent<MeshCollider>();
        }
    }

    static void SetupPlayer()
    {
        System.Type walkerType = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .FirstOrDefault(t => t.Name == "SceneWalker");
        if (walkerType == null)
        {
            Debug.LogWarning("[S14] SceneWalker.cs가 없습니다. Assets/S14에 복사한 뒤 다시 실행하세요.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, -10f);
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            controller = player.AddComponent<CharacterController>();
        controller.height = 1.7f;
        controller.radius = 0.3f;

        Component walker = player.GetComponent(walkerType);
        if (walker == null)
            walker = player.AddComponent(walkerType);

        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        cam.transform.localRotation = Quaternion.identity;

        // SceneWalker의 Camera Transform 칸 채우기
        SerializedObject serialized = new SerializedObject(walker);
        SerializedProperty property = serialized.GetIterator();
        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference
                && property.name.ToLowerInvariant().Contains("camera"))
            {
                property.objectReferenceValue = cam.transform;
            }
        }
        serialized.ApplyModifiedProperties();
    }

    // 테이블과 그 주변 전부를 S14_Props 하나에 묶음. 이것만 옮기면 함께 움직임
    static Transform SetupProps()
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        GameObject old = GameObject.Find(PropsName);
        if (old != null)
        {
            position = old.transform.position;
            rotation = old.transform.rotation;
            Object.DestroyImmediate(old);
        }

        Transform props = new GameObject(PropsName).transform;
        props.SetPositionAndRotation(position, rotation);

        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/S02/Table.prefab");
        GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, props);
        table.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        table.transform.localRotation = Quaternion.identity;
        table.transform.localScale = Vector3.one * 0.5f;

        BuildFireBowl(props);
        BuildPrimitives(props);
        BuildMyMeshes(props);
        BuildLights(props);
        return props;
    }

    // 실습 3, 5: 화로, 숯, 불빛, 연기
    static void BuildFireBowl(Transform props)
    {
        Material bowlMat = Lit("Mat_S14_FireBowl", Color.white, 0.5f, 0.25f, "metal");
        GameObject bowl = Primitive(PrimitiveType.Cylinder, "FireBowl", props,
            new Vector3(0f, TableTop + 0.05f, 0f), new Vector3(0.3f, 0.05f, 0.3f), bowlMat);

        Material coalMat = Lit("Mat_S14_Coal", new Color(0.02f, 0.02f, 0.02f), 0f, 0.1f);
        Color coalGlow = new Color(191f / 255f, 117f / 255f, 3f / 255f) * Mathf.Pow(2f, 5f);
        SetEmission(coalMat, coalGlow, FindTexture("lava"));

        Transform coals = new GameObject("Coals").transform;
        coals.SetParent(props, false);
        coals.localPosition = new Vector3(0f, TableTop + 0.13f, 0f);

        Primitive(PrimitiveType.Sphere, "Coal1", coals, new Vector3(-0.05f, 0f, 0.03f),
            new Vector3(0.12f, 0.08f, 0.1f), coalMat).transform.localRotation = Quaternion.Euler(10f, 40f, 0f);
        Primitive(PrimitiveType.Sphere, "Coal2", coals, new Vector3(0.05f, 0f, 0.03f),
            new Vector3(0.1f, 0.07f, 0.11f), coalMat).transform.localRotation = Quaternion.Euler(-15f, 110f, 5f);
        Primitive(PrimitiveType.Sphere, "Coal3", coals, new Vector3(0f, 0f, -0.06f),
            new Vector3(0.11f, 0.08f, 0.09f), coalMat).transform.localRotation = Quaternion.Euler(5f, 200f, -10f);
        Primitive(PrimitiveType.Sphere, "Coal4", coals, new Vector3(0f, 0.05f, 0f),
            new Vector3(0.09f, 0.06f, 0.1f), coalMat).transform.localRotation = Quaternion.Euler(20f, 300f, 15f);

        Light fire = AddLight("Light_Fire", props, LightType.Point, new Vector3(0f, TableTop + 0.3f, 0f), 1800f, 2f);
        fire.range = 2f;

        BuildSmoke(bowl.transform);
    }

    static void BuildSmoke(Transform bowl)
    {
        GameObject smoke = new GameObject("PS_Smoke");
        smoke.transform.SetParent(bowl, false);
        smoke.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem system = smoke.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.gray);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 10f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.01f;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0.3f;

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0f),
                new GradientColorKey(new Color(0.55f, 0.38f, 0.25f), 0.35f),
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.2f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f, 1f),
            });
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1f));

        ParticleSystemRenderer particleRenderer = smoke.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = SmokeMaterial();
        particleRenderer.maxParticleSize = 1f;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Material SmokeMaterial()
    {
        Material mat = LoadOrCreate("Mat_S14_Smoke", "Universal Render Pipeline/Particles/Lit");

        // Inspector의 Surface Type: Transparent, Blending Mode: Alpha와 같은 설정
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_BlendModePreserveSpecular", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetFloat("_Smoothness", 0f);

        Material particleDefault = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        if (particleDefault != null && particleDefault.mainTexture != null)
            mat.SetTexture("_BaseMap", particleDefault.mainTexture);
        else
            Debug.LogWarning("[S14] Default-Particle을 찾지 못했습니다. Mat_S14_Smoke의 Base Map을 직접 넣으세요.");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 과제 3번: 텍스처를 넣은 서로 다른 재질의 프리미티브 3개
    static void BuildPrimitives(Transform props)
    {
        Primitive(PrimitiveType.Cube, "Prim_WoodBox", props, new Vector3(1f, 0.2f, 0.6f), Vector3.one * 0.4f,
            Lit("Mat_S14_Wood", Color.white, 0f, 0.3f, "wood"));
        Primitive(PrimitiveType.Sphere, "Prim_StoneBall", props, new Vector3(-1f, 0.15f, 0.6f), Vector3.one * 0.3f,
            Lit("Mat_S14_Stone", Color.white, 0f, 0.15f, "stone"));
        Primitive(PrimitiveType.Cylinder, "Prim_MetalDrum", props, new Vector3(1f, 0.2f, -0.6f), new Vector3(0.3f, 0.2f, 0.3f),
            Lit("Mat_S14_Metal", Color.white, 0.8f, 0.5f, "metal"));
    }

    // 과제 3번: 내 메시 3개. 텍스처 없이 값만 사용, Pyramid는 Emission으로 Bloom이 보이게 함
    static void BuildMyMeshes(Transform props)
    {
        MyMesh<S04_CustomDiamondMesh>("Mesh_Diamond", props, new Vector3(-0.4f, TableTop + 0.15f, 0.05f), 0.15f,
            Lit("Mat_S14_Diamond", new Color(0.9f, 0.7f, 0.25f), 1f, 0.8f));

        Material glow = Lit("Mat_S14_Pyramid", new Color(0.1f, 0.3f, 0.5f), 0f, 0.5f);
        SetEmission(glow, new Color(0.2f, 0.6f, 1f) * 4f, null);
        MyMesh<S12_CustomPyramidMesh>("Mesh_Pyramid", props, new Vector3(0.28f, TableTop, 0.05f), 0.15f, glow);

        Material pentagonMat = Lit("Mat_S14_Pentagon", new Color(0.6f, 0.1f, 0.15f), 0.2f, 0.6f);
        pentagonMat.SetFloat("_Cull", (float)CullMode.Off); // 면이 한 장이라 양쪽에서 보이게 함
        EditorUtility.SetDirty(pentagonMat);
        MyMesh<S03_CustomPolygonMesh_Pentagon>("Mesh_Pentagon", props, new Vector3(0f, TableTop + 0.07f, 0.24f), 0.06f, pentagonMat);
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

    static void BuildLights(Transform props)
    {
        Light lamp = AddLight("Light_Lamp", props, LightType.Point, new Vector3(0f, 2.4f, 0f), 2500f, 3f);
        lamp.range = 5f;

        Light spot = AddLight("Light_Spot", props, LightType.Spot, new Vector3(0f, 2.4f, 0f), 3000f, 3f);
        spot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spot.spotAngle = 40f;
        spot.range = 4f;
    }

    static Light AddLight(string name, Transform parent, LightType type, Vector3 position, float temperature, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        Light light = go.AddComponent<Light>();
        light.type = type;
        light.useColorTemperature = true;
        light.colorTemperature = temperature;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
        return light;
    }

    // 실습 4: Global Volume과 Bloom
    static void SetupBloom()
    {
        string path = Folder + "/S14_VolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
            bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.5f);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);

        GameObject go = GameObject.Find("Global Volume");
        if (go == null)
            go = new GameObject("Global Volume");

        Volume volume = go.GetComponent<Volume>();
        if (volume == null)
            volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    // 실습 4: 구도용 카메라. 평소에는 꺼 둠
    static void SetupShotCamera(Transform props)
    {
        if (Resources.FindObjectsOfTypeAll<Camera>().Any(c => c.name == "ShotCamera" && c.gameObject.scene.IsValid()))
            return;

        GameObject go = new GameObject("ShotCamera");
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NightColor;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        Vector3 target = props.position + Vector3.up * (TableTop + 0.2f);
        go.transform.position = target + new Vector3(1.4f, 0.5f, -1.4f);
        go.transform.LookAt(target);
        go.SetActive(false);
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material Lit(string name, Color baseColor, float metallic, float smoothness, string textureName = null)
    {
        Material mat = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (textureName != null)
            mat.SetTexture("_BaseMap", FindTexture(textureName));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void SetEmission(Material mat, Color color, Texture map)
    {
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color);
        mat.SetTexture("_EmissionMap", map);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
    }

    static Material LoadOrCreate(string name, string shaderName)
    {
        string path = $"{Folder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // ThirdParty/Textures에서 이름으로 텍스처를 찾음 (metal, lava, wood, stone)
    static Texture2D FindTexture(string name)
    {
        if (AssetDatabase.IsValidFolder(TextureFolder))
        {
            string guid = AssetDatabase.FindAssets($"{name} t:Texture2D", new[] { TextureFolder }).FirstOrDefault();
            if (guid != null)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
        }

        Debug.LogWarning($"[S14] {TextureFolder}에 {name} 텍스처가 없습니다. 넣은 뒤 다시 실행하세요.");
        return null;
    }
}
