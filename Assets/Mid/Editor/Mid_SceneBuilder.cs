using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 메뉴 CG > 중간 장면 만들기: S15_Landscape를 Midterm_Pavilion으로 복사하고
// S14의 화로·테이블·소품을 지운 뒤, 「별을 읽는 마녀의 정자」의 혼천의, 자수정, 거울,
// 테이블, 카페트, 북두칠성을 놓음.
// 다시 실행해도 됨: Mid_로 시작하는 오브젝트는 위치를 유지한 채 새로 만들고, 재질 값은 아래 값으로 되돌아감.
public static class Mid_SceneBuilder
{
    const string Folder = "Assets/Mid";
    const string S15ScenePath = "Assets/S15/S15_Landscape.unity";
    const string ScenePath = Folder + "/Midterm_Pavilion.unity";
    const string OldPropsName = "S14_Props";   // S14의 화로, 테이블, 소품 묶음: 중간 장면에서는 지움
    const string PropsName = "Mid_Props";      // 마녀의 정자 소품 묶음: 이것만 옮기면 전부 따라옴
    const float TableHeight = 0.75f;

    // 정자 앞면에서 트여 있는 칸(계단 쪽)의 양쪽 기둥 x 위치. 장면 화면에서 잰 값, 소품 중심 기준
    const float EntranceLeftX = -2.5f;
    const float EntranceRightX = 0.34f;

    // 혼천의 크기
    const float PedestalHeight = 0.8f;
    const float OuterRadius = 0.45f;
    const float InnerRadius = 0.40f;
    const float CoreRadius = 0.33f;
    const float Latitude = 37.5f;    // 서울의 위도: 북극 축이 땅에서 이만큼 들려 있음

    [MenuItem("CG/중간 장면 만들기")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Mid] Play 모드를 끄고 실행하세요.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("[Mid] 지금 씬을 Ctrl+S로 저장한 뒤 다시 실행하세요.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                AssetDatabase.CopyAsset(S15ScenePath, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        Transform props = SetupPropsRoot();
        if (props == null) return;

        Material brass = Lit("Mat_Mid_Brass", new Color(0.78f, 0.58f, 0.26f), 1f, 0.7f);
        Material bronze = Lit("Mat_Mid_Bronze", new Color(0.32f, 0.22f, 0.14f), 0.8f, 0.45f);
        Material orb = Lit("Mat_Mid_Orb", new Color(0.05f, 0.1f, 0.2f), 0f, 0.8f);
        SetEmission(orb, new Color(0.3f, 0.6f, 1f) * 3f);

        Transform armillary = BuildArmillary(props, brass, bronze, orb);
        BuildCarpet(props);
        float tableTop = BuildTable(props);
        BuildAmethyst(props, tableTop);
        BuildMirror(props, brass, bronze);
        BuildRoomLight(props);
        float entrance = FindEntrance(props);
        BuildEntranceCandles(props, bronze, entrance);
        BuildGlowPath(props, entrance);
        Transform sky = BuildSky(props);
        BuildShotCamera(props, tableTop);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeTransform = props;
        Debug.Log($"[Mid] 장면 구성 완료. {PropsName}(소품 전체)와 {sky.name}(별자리)를 원하는 곳으로 옮기세요.");
    }

    // ---- 입구와 길 ----

    // 정자 바닥의 높이: 이보다 아래로 떨어지는 곳이 계단의 시작(입구)
    // props 기준 -z 방향(플레이어가 걸어오는 쪽)으로 걸어가며 바닥이 꺼지는 거리를 찾음
    static float FindEntrance(Transform props)
    {
        Physics.SyncTransforms();
        float floorY = props.position.y;
        for (float d = 0.5f; d < 8f; d += 0.05f)
        {
            float? y = GroundY(props.TransformPoint(new Vector3(0f, 0f, -d)), floorY + 0.3f);
            if (y.HasValue && y.Value < floorY - 0.12f)
            {
                Debug.Log($"[Mid] 입구(계단 시작): 소품 중심에서 {d:F2} m 앞");
                return d;
            }
        }
        Debug.LogWarning("[Mid] 입구를 찾지 못해 2.5 m 앞으로 둡니다.");
        return 2.5f;
    }

    // 위에서 아래로 쏜 광선이 maxY보다 낮은 곳에서 처음 맞는 면의 높이 (지붕, 난간 윗면은 건너뜀)
    static float? GroundY(Vector3 point, float maxY)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(point.x, maxY + 20f, point.z), Vector3.down, 60f);
        float? best = null;
        foreach (RaycastHit h in hits)
            if (!(h.collider is CharacterController) && h.point.y <= maxY && (!best.HasValue || h.point.y > best.Value))
                best = h.point.y;
        return best;
    }

    // 입구 양쪽에 서 있는 큰 촛대 두 개
    static void BuildEntranceCandles(Transform props, Material bronze, float entrance)
    {
        Transform root = Reset(props, "Mid_EntranceCandles", Vector3.zero);
        Material wax = Lit("Mat_Mid_Wax", new Color(0.92f, 0.86f, 0.72f), 0f, 0.3f);
        Material flame = Lit("Mat_Mid_Flame", Color.black, 0f, 0f);
        Color flameColor = new Color(1f, 0.55f, 0.15f) * 8f;
        SetEmission(flame, flameColor);

        for (int side = -1; side <= 1; side += 2)
        {
            Transform holder = Empty(side < 0 ? "Candle_L" : "Candle_R", root, new Vector3(side < 0 ? EntranceLeftX + 0.2f : EntranceRightX - 0.2f, 0f, -(entrance + 0.25f)));
            float? ground = GroundY(holder.position, props.position.y + 0.05f);   // 계단 앞 데크 높이에 세움
            if (ground.HasValue)
                holder.localPosition = new Vector3(holder.localPosition.x, props.InverseTransformPoint(new Vector3(0f, ground.Value, 0f)).y, holder.localPosition.z);

            const float standTop = 0.95f;
            GameObject stand = MeshObject<Mid_PedestalMesh>("Stand", holder, Vector3.zero, Vector3.zero, bronze);
            Mid_PedestalMesh standMesh = stand.GetComponent<Mid_PedestalMesh>();
            standMesh.profile = new[]
            {
                new Vector2(0.14f, 0f), new Vector2(0.14f, 0.03f), new Vector2(0.04f, 0.08f),
                new Vector2(0.025f, standTop - 0.06f), new Vector2(0.08f, standTop - 0.02f), new Vector2(0.08f, standTop),
            };
            standMesh.Build();

            GameObject candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            candle.name = "Wax";
            Object.DestroyImmediate(candle.GetComponent<Collider>());
            candle.transform.SetParent(holder, false);
            candle.transform.localPosition = new Vector3(0f, standTop + 0.11f, 0f);
            candle.transform.localScale = new Vector3(0.06f, 0.11f, 0.06f);
            candle.GetComponent<MeshRenderer>().sharedMaterial = wax;

            GameObject fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fire.name = "Flame";
            Object.DestroyImmediate(fire.GetComponent<Collider>());
            fire.transform.SetParent(holder, false);
            fire.transform.localPosition = new Vector3(0f, standTop + 0.26f, 0f);
            fire.transform.localScale = new Vector3(0.03f, 0.06f, 0.03f);
            fire.GetComponent<MeshRenderer>().sharedMaterial = flame;
            fire.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Mid_GlowPulse firePulse = fire.AddComponent<Mid_GlowPulse>();
            firePulse.flicker = true; firePulse.speed = 6f; firePulse.amount = 0.3f; firePulse.phase = side * 3f;
            firePulse.emission = flameColor;

            Light light = AddLight("Light", holder, new Vector3(0f, standTop + 0.35f, 0f), new Color(1f, 0.62f, 0.3f), 1.5f, 3f);
            light.shadows = LightShadows.None;
            Mid_GlowPulse lightPulse = light.gameObject.AddComponent<Mid_GlowPulse>();
            lightPulse.flicker = true; lightPulse.speed = 6f; lightPulse.amount = 0.25f; lightPulse.phase = side * 3f;
        }
    }

    // 계단 찾기: 입구 앞을 격자로 훑어 높이를 잼. 데크보다 낮고 땅보다 높은 칸 = 계단
    // 돌려주는 값: x = 계단 왼쪽 끝, y = 오른쪽 끝, z = 계단이 시작하는 거리, w = 끝나는(땅에 닿는) 거리
    static Vector4? FindStairs(Transform props, float entrance)
    {
        float floorY = props.position.y;
        float deckY = float.MinValue, groundY = float.MaxValue;
        var heights = new System.Collections.Generic.Dictionary<(int, int), float>();
        for (int iz = 0; iz <= 80; iz++)
        {
            for (int ix = -40; ix <= 40; ix++)
            {
                float? h = GroundY(props.TransformPoint(new Vector3(ix * 0.1f, 0f, -(entrance + iz * 0.1f))), floorY + 0.05f);
                if (!h.HasValue) continue;
                heights[(ix, iz)] = h.Value;
                if (iz <= 2) deckY = Mathf.Max(deckY, h.Value);
                groundY = Mathf.Min(groundY, h.Value);
            }
        }

        float xMin = float.MaxValue, xMax = float.MinValue, dMin = float.MaxValue, dMax = float.MinValue;
        foreach (var kv in heights)
        {
            if (kv.Value <= groundY + 0.06f || kv.Value >= deckY - 0.06f) continue;
            xMin = Mathf.Min(xMin, kv.Key.Item1 * 0.1f); xMax = Mathf.Max(xMax, kv.Key.Item1 * 0.1f);
            float d = entrance + kv.Key.Item2 * 0.1f;
            dMin = Mathf.Min(dMin, d); dMax = Mathf.Max(dMax, d);
        }
        Debug.Log($"[Mid] 높이: 데크 {deckY:F2}, 땅 {groundY:F2} / 계단 x {xMin:F2}~{xMax:F2}, 거리 {dMin:F2}~{dMax:F2}");
        if (xMin > xMax) return null;
        return new Vector4(xMin, xMax, dMin, dMax);
    }

    // 계단 양옆과 그 앞 평지에만 깔린, 은은한 흰빛 돌 (데크 위에는 두지 않음)
    static void BuildGlowPath(Transform props, float entrance)
    {
        Transform root = Reset(props, "Mid_GlowPath", Vector3.zero);
        Vector4? found = FindStairs(props, entrance);
        if (!found.HasValue)
        {
            Debug.LogWarning("[Mid] 계단을 찾지 못해 바닥 조명을 만들지 않았습니다.");
            return;
        }
        Vector4 stairs = found.Value;
        float left = stairs.x + 0.12f, right = stairs.y - 0.12f, centerX = (stairs.x + stairs.y) * 0.5f;

        Color c = new Color(1f, 0.96f, 0.9f);   // 은은한 흰빛
        Material stoneMat = Lit("Mat_Mid_GlowStone", new Color(0.2f, 0.2f, 0.2f), 0f, 0.9f);
        SetEmission(stoneMat, c * 1.6f);

        float floorY = props.position.y - 0.1f;   // 이보다 높은 곳(데크)은 건너뜀
        int count = 0;
        for (float d = stairs.z + 0.15f; d < stairs.w + 3.2f; d += 0.45f, count++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 local = new Vector3(side < 0 ? left : right, 0f, -d);
                float? y = GroundY(props.TransformPoint(local), floorY + 0.05f);
                if (!y.HasValue) continue;
                local.y = props.InverseTransformPoint(new Vector3(0f, y.Value, 0f)).y;

                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"Glow_{count}_{(side < 0 ? "L" : "R")}";
                Object.DestroyImmediate(stone.GetComponent<Collider>());
                stone.transform.SetParent(root, false);
                stone.transform.localPosition = local + Vector3.up * 0.01f;
                stone.transform.localScale = new Vector3(0.09f, 0.03f, 0.07f);
                stone.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
                stone.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Mid_GlowPulse pulse = stone.AddComponent<Mid_GlowPulse>();
                pulse.emission = c * 1.6f; pulse.speed = 0.8f; pulse.amount = 0.2f; pulse.phase = count * 0.5f;
            }

            if (count % 2 == 0)   // 빛은 두 칸마다 하나, 계단 가운데 바닥 가까이
            {
                Vector3 local = new Vector3(centerX, 0f, -d);
                float? y = GroundY(props.TransformPoint(local), floorY + 0.05f);
                if (!y.HasValue) continue;
                local.y = props.InverseTransformPoint(new Vector3(0f, y.Value, 0f)).y + 0.3f;
                Light light = AddLight($"Light_{count}", root, local, c, 0.5f, 1.5f);
                light.shadows = LightShadows.None;
                Mid_GlowPulse pulse = light.gameObject.AddComponent<Mid_GlowPulse>();
                pulse.speed = 0.8f; pulse.amount = 0.2f; pulse.phase = count * 0.5f;
            }
        }
    }

    // S14_Props 자리에 Mid_Props를 만들고, 화로·테이블·소품이 든 S14_Props는 지움
    static Transform SetupPropsRoot()
    {
        GameObject old = GameObject.Find(OldPropsName);
        GameObject root = GameObject.Find(PropsName);
        if (root == null)
        {
            if (old == null)
            {
                Debug.LogError($"[Mid] 씬에 {OldPropsName}도 {PropsName}도 없습니다.");
                return null;
            }
            root = new GameObject(PropsName);
            root.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
        }
        if (old != null) Object.DestroyImmediate(old);
        return root.transform;
    }

    // 필수 ①: 반복문 메시 2종(고리, 받침대) / 필수 ④: 부모-자식 + 직접 만든 회전 행렬
    //
    // Mid_Armillary            ← 이것만 옮기면 전부 따라옴
    // ├ Pedestal               받침대 (Mid_PedestalMesh)
    // └ Armillary_Root         고정된 바깥 틀 (육합의)
    //   ├ Ring_Horizon         지평환: 땅과 나란한 고리
    //   ├ Ring_Meridian        자오환: 남북을 잇는 세로 고리
    //   ├ Core_Orb             가운데에서 돌면서 떠오르는 구
    //   └ Gimbal_Sky           북극을 축으로 도는 하늘 (삼신의)
    //     ├ Ring_Equator       적도환
    //     ├ Ring_Ecliptic      황도환 (23.5° 기울어짐)
    //     ├ Ring_Colure        극을 지나는 세로 고리
    //     └ Gimbal_Sight       하늘보다 빠르게 반대로 도는 관측 고리 (사유의)
    //       ├ Ring_Sight
    //       └ Sight_Tube       망원 막대: 관측 고리 안에서 또 한 번 돎
    static Transform BuildArmillary(Transform props, Material brass, Material bronze, Material orb)
    {
        Transform root = Reset(props, "Mid_Armillary", new Vector3(-1.1f, 0f, 1.1f));

        GameObject pedestal = MeshObject<Mid_PedestalMesh>("Pedestal", root, Vector3.zero, Vector3.zero, bronze);
        pedestal.AddComponent<MeshCollider>().sharedMesh = pedestal.GetComponent<MeshFilter>().sharedMesh;

        Transform frame = Empty("Armillary_Root", root, new Vector3(0f, PedestalHeight + OuterRadius + 0.02f, 0f));
        Ring("Ring_Horizon", frame, OuterRadius, 0.016f, Vector3.zero, brass);
        Ring("Ring_Meridian", frame, OuterRadius, 0.016f, new Vector3(0f, 0f, 90f), brass);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core_Orb";
        Object.DestroyImmediate(core.GetComponent<Collider>());
        core.transform.SetParent(frame, false);
        core.transform.localScale = Vector3.one * 0.1f;
        core.GetComponent<MeshRenderer>().sharedMaterial = orb;
        Spin(core, Vector3.zero, Vector3.up, 45f, 0.03f, 1.5f);

        // 북극 축: 수직에서 (90° - 위도)만큼 북쪽(z)으로 기울임
        Transform sky = Empty("Gimbal_Sky", frame, Vector3.zero);
        Spin(sky.gameObject, new Vector3(90f - Latitude, 0f, 0f), Vector3.up, 12f);
        Ring("Ring_Equator", sky, InnerRadius, 0.012f, Vector3.zero, brass);
        Ring("Ring_Ecliptic", sky, InnerRadius, 0.012f, new Vector3(23.5f, 0f, 0f), brass);
        Ring("Ring_Colure", sky, InnerRadius, 0.012f, new Vector3(0f, 0f, 90f), brass);

        Transform sight = Empty("Gimbal_Sight", sky, Vector3.zero);
        Spin(sight.gameObject, Vector3.zero, Vector3.up, -30f);
        Ring("Ring_Sight", sight, CoreRadius, 0.01f, new Vector3(0f, 0f, 90f), brass);

        // 망원 막대: 받침대 메시를 가늘고 길게 바꿔 다시 씀. 세로 고리(YZ 평면) 안에서 x축으로 돎
        GameObject tube = MeshObject<Mid_PedestalMesh>("Sight_Tube", sight, Vector3.zero, Vector3.zero, bronze);
        Mid_PedestalMesh tubeMesh = tube.GetComponent<Mid_PedestalMesh>();
        tubeMesh.segments = 12;
        tubeMesh.profile = new[]
        {
            new Vector2(0.012f, -CoreRadius), new Vector2(0.018f, -CoreRadius + 0.03f),
            new Vector2(0.012f, 0f),
            new Vector2(0.018f, CoreRadius - 0.03f), new Vector2(0.012f, CoreRadius),
        };
        tubeMesh.Build();
        Spin(tube, new Vector3(90f, 0f, 0f), Vector3.right, 20f);

        return root;
    }

    // 장식: 점성술 마법진이 그려진 어두운 깔개. 무늬를 코드로 그리고, 선만 아주 약하게 빛나게 함 (텍스처는 처음 한 번만 만듦)
    static void BuildCarpet(Transform props)
    {
        Transform root = Reset(props, "Mid_Carpet", new Vector3(0f, 0.005f, 0.15f));
        GameObject carpet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carpet.name = "StarRug";
        Object.DestroyImmediate(carpet.GetComponent<Collider>());   // 밟을 때 걸리지 않게
        carpet.transform.SetParent(root, false);
        carpet.transform.localScale = new Vector3(2.6f, 0.01f, 1.9f);

        Texture2D tex = StarRugTexture();
        Material mat = Lit("Mat_Mid_Carpet", Color.white, 0f, 0.2f);
        mat.SetTexture("_BaseMap", tex);
        SetEmission(mat, new Color(0.5f, 0.85f, 0.8f) * 0.35f);   // 어두운 바탕은 거의 안 빛나고 밝은 선만 은은하게
        mat.SetTexture("_EmissionMap", tex);
        EditorUtility.SetDirty(mat);
        carpet.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // 동양풍 장식에 쓰는 부드러운 청색 계열
    static readonly Color Sky = new Color(0.66f, 0.81f, 0.92f);       // 하늘빛
    static readonly Color PaleSky = new Color(0.82f, 0.90f, 0.95f);   // 연하늘
    static readonly Color Blue = new Color(0.40f, 0.58f, 0.80f);      // 청색
    static readonly Color Indigo = new Color(0.22f, 0.34f, 0.56f);    // 쪽빛
    static readonly Color Jade = new Color(0.70f, 0.86f, 0.84f);      // 옥색
    static readonly Color Ivory = new Color(0.94f, 0.94f, 0.91f);     // 흰 비단
    static readonly Color Navy = new Color(0.12f, 0.18f, 0.38f);      // 남색

    // 깔개 무늬: 남흑색 바탕 위에 원 세 겹, 열두 별자리 자리 표시, 원에 내접한 팔방 별, 가운데 작은 원
    static Texture2D StarRugTexture()
    {
        string path = Folder + "/Tex_Mid_StarRug.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int w = 520, h = 380;
        Color ground = new Color(0.04f, 0.06f, 0.11f);   // 남흑색 바탕
        Color band = new Color(0.07f, 0.13f, 0.17f);     // 테두리 띠 (어두운 청록)
        Color line = new Color(0.36f, 0.62f, 0.58f);     // 마법진 선 (옥색)
        Color silver = new Color(0.66f, 0.70f, 0.76f);   // 별자리 표시 (은색)

        // 팔방 별 = 원에 내접한 정사각형 두 개(45° 엇갈림)의 변 8개
        var star = new System.Collections.Generic.List<(Vector2, Vector2)>();
        for (int t = 0; t < 2; t++)
            for (int i = 0; i < 4; i++)
            {
                float a0 = Mathf.PI / 4f + t * Mathf.PI / 4f + i * Mathf.PI / 2f;
                float a1 = a0 + Mathf.PI / 2f;
                star.Add((new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 0.86f, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 0.86f));
            }

        float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        float unit = h * 0.42f;
        float px = 1f / unit;   // 1픽셀 = 반지름 1 기준으로 얼마인지
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float weave = y % 6 < 3 ? 1f : 0.85f;   // 짠 결
                Color c = ground * weave;

                Vector2 p = new Vector2((x - w * 0.5f) / unit, (y - h * 0.5f) / unit);
                float r = p.magnitude;
                float ang = Mathf.Atan2(p.y, p.x);

                // 원 세 겹
                if (Mathf.Abs(r - 1.0f) < 1.6f * px || Mathf.Abs(r - 0.86f) < 1.2f * px || Mathf.Abs(r - 0.5f) < 1.0f * px) c = line;
                // 두 바깥 원 사이: 열두 별자리 자리마다 작은 은색 점과 짧은 눈금
                float sector = Mathf.Repeat(ang / (2f * Mathf.PI) * 12f, 1f);
                if (r > 0.88f && r < 0.98f)
                {
                    Vector2 mark = new Vector2(Mathf.Cos(Mathf.Round(ang / (2f * Mathf.PI) * 12f) * 2f * Mathf.PI / 12f), Mathf.Sin(Mathf.Round(ang / (2f * Mathf.PI) * 12f) * 2f * Mathf.PI / 12f)) * 0.93f;
                    if (Vector2.Distance(p, mark) < 3.2f * px) c = silver;
                    if (Mathf.Abs(sector - 0.5f) < 0.6f * px * 12f / (2f * Mathf.PI * r) * 2f) c = line * 0.7f;   // 칸 나눔 눈금
                }
                // 팔방 별
                foreach (var (a0, a1) in star)
                    if (SegmentDistance(p, a0, a1) < 1.1f * px) c = line;
                // 가운데: 작은 원과 은색 점
                if (Mathf.Abs(r - 0.14f) < 1.0f * px) c = line;
                if (r < 0.04f) c = silver;

                // 테두리: 어두운 청록 띠 + 옥색 가는 줄 두 개
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 22) c = band * weave;
                if (edge == 6 || edge == 7 || edge == 18) c = line;

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // 장식: 다리 하나짜리 둥근 테이블 + 바닥까지 늘어지는 어두운 남청색 천. 상판 높이를 돌려줌
    // 테이블과 천은 받침대와 같은 회전체 메시(Mid_PedestalMesh)에 옆모습만 다르게 줌
    static float BuildTable(Transform props)
    {
        Transform root = Reset(props, "Mid_Table", Vector3.zero);
        const float top = TableHeight;

        Material wood = Lit("Mat_Mid_DarkWood", new Color(0.24f, 0.13f, 0.07f), 0f, 0.55f);
        GameObject table = MeshObject<Mid_PedestalMesh>("RoundTable", root, Vector3.zero, Vector3.zero, wood);
        Mid_PedestalMesh tableMesh = table.GetComponent<Mid_PedestalMesh>();
        tableMesh.segments = 48;
        tableMesh.profile = new[]
        {
            new Vector2(0.25f, 0f), new Vector2(0.25f, 0.03f), new Vector2(0.06f, 0.08f),   // 받침
            new Vector2(0.05f, top - 0.07f), new Vector2(0.12f, top - 0.03f),                // 다리
            new Vector2(0.45f, top - 0.03f), new Vector2(0.45f, top),                        // 상판
        };
        tableMesh.Build();

        // 어두운 남청색 천: 상판을 덮고 바닥 바로 위까지 퍼지며 늘어짐 (깔개와 같은 색 계열)
        Material cloth = Lit("Mat_Mid_TableCloth", new Color(0.10f, 0.17f, 0.26f), 0f, 0.4f);
        cloth.DisableKeyword("_EMISSION");
        cloth.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(cloth);
        GameObject cover = MeshObject<Mid_PedestalMesh>("TableCloth", root, Vector3.zero, Vector3.zero, cloth);
        Mid_PedestalMesh coverMesh = cover.GetComponent<Mid_PedestalMesh>();
        coverMesh.segments = 48;
        coverMesh.profile = new[]
        {
            new Vector2(0.62f, 0.012f),         // 바닥에 닿을 듯한 끝단
            new Vector2(0.58f, 0.30f),
            new Vector2(0.53f, top - 0.12f),
            new Vector2(0.50f, top - 0.03f),
            new Vector2(0.47f, top + 0.012f),   // 상판을 덮는 윗면 가장자리
        };
        coverMesh.Build();

        // 끝단의 가는 옥색 선: 깔개의 마법진 선처럼 아주 약하게 빛남
        Material hem = Lit("Mat_Mid_ClothHem", new Color(0.36f, 0.62f, 0.58f), 0f, 0.5f);
        SetEmission(hem, new Color(0.36f, 0.62f, 0.58f) * 0.4f);
        Ring("ClothHem", root, 0.62f, 0.008f, Vector3.zero, hem);
        root.Find("ClothHem").localPosition = new Vector3(0f, 0.02f, 0f);

        return top + 0.012f;
    }

    // 필수 ① 메시 하나 더 + 선택 ④ 움직임 하나 더: 테이블 위에 떠서 도는 자수정 무리
    static void BuildAmethyst(Transform props, float tableTop)
    {
        Transform root = Reset(props, "Mid_Amethyst", new Vector3(0f, tableTop + 0.4f, 0f));
        Spin(root.gameObject, Vector3.zero, Vector3.up, 25f, 0.04f, 1.2f);

        Material amethyst = Lit("Mat_Mid_Amethyst", new Color(0.45f, 0.15f, 0.75f), 0f, 0.9f);
        SetEmission(amethyst, new Color(0.55f, 0.2f, 1f) * 1.5f);

        // (위치, 기울기, 반지름, 몸통 높이, 끝 높이)
        (Vector3 pos, Vector3 euler, float r, float body, float tip)[] crystals =
        {
            (new Vector3( 0.00f, -0.10f,  0.00f), new Vector3(  0f,  0f,   0f), 0.045f, 0.20f, 0.07f),
            (new Vector3( 0.04f, -0.10f,  0.01f), new Vector3(  5f,  0f, -32f), 0.030f, 0.13f, 0.05f),
            (new Vector3(-0.04f, -0.10f,  0.02f), new Vector3( 10f, 20f,  35f), 0.032f, 0.15f, 0.05f),
            (new Vector3( 0.00f, -0.10f, -0.04f), new Vector3(-38f, 40f,   0f), 0.025f, 0.11f, 0.04f),
            (new Vector3(-0.01f, -0.10f,  0.04f), new Vector3( 30f, 70f,   5f), 0.022f, 0.09f, 0.035f),
        };
        for (int i = 0; i < crystals.Length; i++)
        {
            var c = crystals[i];
            GameObject go = MeshObject<Mid_CrystalMesh>($"Crystal{i + 1}", root, c.pos, c.euler, amethyst);
            Mid_CrystalMesh mesh = go.GetComponent<Mid_CrystalMesh>();
            mesh.radius = c.r;
            mesh.bodyHeight = c.body;
            mesh.tipHeight = c.tip;
            mesh.Build();
        }

        Light glow = AddLight("Light_Amethyst", root, new Vector3(0f, 0.25f, 0f), new Color(0.82f, 0.7f, 1f), 0.6f, 3f);
        glow.shadows = LightShadows.None;
    }

    // 선택 ② 래스터화: 우리 래스터라이저로 그린 블랙홀이 비치는 타원형 청동 거울
    static void BuildMirror(Transform props, Material brass, Material bronze)
    {
        Transform root = Reset(props, "Mid_Mirror", new Vector3(1.0f, 0f, 1.2f), Quaternion.Euler(0f, 24f, 0f));
        const float halfW = 0.42f, halfH = 0.68f, standTop = 0.58f;
        float center = standTop + halfH + 0.02f;

        // 푸른 녹이 슨 청동
        Material patina = Lit("Mat_Mid_Patina", new Color(0.40f, 0.55f, 0.54f), 0.75f, 0.5f);

        GameObject stand = MeshObject<Mid_PedestalMesh>("Stand", root, Vector3.zero, Vector3.zero, patina);
        Mid_PedestalMesh standMesh = stand.GetComponent<Mid_PedestalMesh>();
        standMesh.profile = new[]
        {
            new Vector2(0.18f, 0f), new Vector2(0.18f, 0.03f), new Vector2(0.05f, 0.07f),
            new Vector2(0.035f, standTop - 0.03f), new Vector2(0.06f, standTop),
        };
        standMesh.Build();

        Material mirrorMat = Lit("Mat_Mid_Mirror", new Color(0.12f, 0.12f, 0.12f), 0f, 0.6f);
        SetEmission(mirrorMat, Color.white * 1.3f);
        mirrorMat.SetFloat("_Cull", 0f);   // 뒤에서도 보이게
        EditorUtility.SetDirty(mirrorMat);
        GameObject surface = new GameObject("Mirror_Surface");
        surface.transform.SetParent(root, false);
        surface.transform.localPosition = new Vector3(0f, center, 0f);
        surface.AddComponent<MeshFilter>();
        surface.AddComponent<MeshRenderer>().sharedMaterial = mirrorMat;
        Mid_MirrorRasterizer raster = surface.AddComponent<Mid_MirrorRasterizer>();
        raster.halfWidth = halfW;
        raster.halfHeight = halfH;
        raster.canvasWidth = 320;           // 캔버스 비율을 타원 비율에 맞춰야 블랙홀이 둥글게 보임
        raster.canvasHeight = 512;
        raster.enabled = false;             // 바꾼 크기로 메시와 캔버스를 다시 만듦
        raster.enabled = true;

        // 타원 테두리 두 겹: 고리 메시를 세워서(x축 90°) 가로·세로를 다르게 늘림
        Ring("Frame", root, 1f, 0.05f, new Vector3(90f, 0f, 0f), patina);
        Transform frame = root.Find("Frame");
        frame.localPosition = new Vector3(0f, center, 0f);
        frame.localScale = new Vector3(halfW + 0.02f, 0.6f, halfH + 0.02f);
        Ring("FrameOuter", root, 1f, 0.015f, new Vector3(90f, 0f, 0f), patina);
        Transform outer = root.Find("FrameOuter");
        outer.localPosition = new Vector3(0f, center, 0f);
        outer.localScale = new Vector3(halfW + 0.065f, 0.6f, halfH + 0.065f);

        // 꼭대기 꼭지(뉴)와, 옆으로 늘어진 하늘빛 매듭 술
        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knob.name = "Knob";
        Object.DestroyImmediate(knob.GetComponent<Collider>());
        knob.transform.SetParent(root, false);
        knob.transform.localPosition = new Vector3(0f, center + halfH + 0.09f, 0f);
        knob.transform.localScale = Vector3.one * 0.06f;
        knob.GetComponent<MeshRenderer>().sharedMaterial = patina;

        Material cord = Lit("Mat_Mid_Silk", Sky, 0f, 0.55f);
        Material cordDark = Lit("Mat_Mid_Cord", Indigo, 0f, 0.4f);
        float a = -25f * Mathf.Deg2Rad;
        Vector3 knotPos = new Vector3(Mathf.Cos(a) * (halfW + 0.08f), center + Mathf.Sin(a) * (halfH + 0.08f), -0.02f);
        GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot.name = "Maedeup";
        Object.DestroyImmediate(knot.GetComponent<Collider>());
        knot.transform.SetParent(root, false);
        knot.transform.localPosition = knotPos;
        knot.transform.localScale = new Vector3(0.05f, 0.06f, 0.03f);
        knot.GetComponent<MeshRenderer>().sharedMaterial = cordDark;
        GameObject tassel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tassel.name = "Tassel";
        Object.DestroyImmediate(tassel.GetComponent<Collider>());
        tassel.transform.SetParent(root, false);
        tassel.transform.localPosition = knotPos + Vector3.down * 0.13f;
        tassel.transform.localScale = new Vector3(0.035f, 0.1f, 0.035f);
        tassel.GetComponent<MeshRenderer>().sharedMaterial = cord;
    }

    // 화로가 빠진 자리를 채우는 은은한 방 조명
    static void BuildRoomLight(Transform props)
    {
        Transform root = Reset(props, "Mid_Light_Room", new Vector3(0f, 2.4f, 0.3f));
        Light light = root.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.9f, 0.82f);
        light.intensity = 1.5f;
        light.range = 5f;
        light.shadows = LightShadows.Soft;
    }

    static Light AddLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    // 선택 ③: GL로 그리는 별자리 4개 + 달. 정자 밖 하늘에 흩어 둠 (위치는 Mid_Props 기준)
    static Transform BuildSky(Transform props)
    {
        // 북두칠성: Mid_GLConstellation의 기본값 그대로
        Transform dipper = Constellation(props, "Mid_Constellation", new Vector3(-6f, 9f, 12f), null, null, 1f);

        // 카시오페이아: W 모양
        Constellation(props, "Mid_Constellation_Cassiopeia", new Vector3(7f, 12f, 13f),
            new[] { V(-2.4f, 0.6f), V(-1.2f, -0.5f), V(0f, 0.3f), V(1.1f, -0.7f), V(2.3f, 0.4f) },
            new[] { L(0, 1), L(1, 2), L(2, 3), L(3, 4) }, 0.9f);

        // 백조자리: 큰 십자
        Constellation(props, "Mid_Constellation_Cygnus", new Vector3(1.5f, 11.5f, 16f),
            new[] { V(0f, 2.6f), V(0f, 1f), V(0f, -0.4f), V(0f, -2.2f), V(-1.8f, 0.7f), V(1.8f, 1.3f) },
            new[] { L(0, 1), L(1, 2), L(2, 3), L(4, 1), L(1, 5) }, 0.85f);

        // 오리온: 머리, 어깨, 세 별의 허리띠, 발
        Constellation(props, "Mid_Constellation_Orion", new Vector3(13f, 6.5f, 9f),
            new[] { V(0f, 2.9f), V(-1.3f, 2f), V(1.4f, 1.8f), V(-0.45f, 0f), V(0f, 0.12f), V(0.45f, 0.24f), V(-1.2f, -2.1f), V(1.3f, -1.9f) },
            new[] { L(0, 1), L(0, 2), L(1, 3), L(2, 5), L(3, 4), L(4, 5), L(3, 6), L(5, 7) }, 0.9f);

        BuildMoon(props);
        return dipper;
    }

    static Vector3 V(float x, float y) => new Vector3(x, y, 0f);
    static Vector2Int L(int a, int b) => new Vector2Int(a, b);

    // 같은 이름이 있으면 그 위치를 지키고, 없으면 offset 자리에 정자 쪽을 향하게 둠
    static Transform Constellation(Transform props, string name, Vector3 offset, Vector3[] stars, Vector2Int[] lines, float starScale)
    {
        Vector3 position = props.position + props.rotation * offset;
        Quaternion rotation = Quaternion.LookRotation(position - (props.position + Vector3.up * 1.5f));
        GameObject old = GameObject.Find(name);
        if (old != null)
        {
            position = old.transform.position;
            rotation = old.transform.rotation;
            Object.DestroyImmediate(old);
        }

        GameObject go = new GameObject(name);
        go.transform.SetPositionAndRotation(position, rotation);
        Mid_GLConstellation c = go.AddComponent<Mid_GLConstellation>();
        if (stars != null) c.stars = stars;
        if (lines != null) c.lines = lines;
        c.starSize *= starScale;
        return go.transform;
    }

    // 달: 스스로 빛나는 큰 구. Bloom이 둘레를 번지게 함
    static void BuildMoon(Transform props)
    {
        const string name = "Mid_Moon";
        Vector3 position = props.position + props.rotation * new Vector3(9f, 13f, 22f);
        GameObject old = GameObject.Find(name);
        if (old != null)
        {
            position = old.transform.position;
            Object.DestroyImmediate(old);
        }

        Material moonMat = Lit("Mat_Mid_Moon", new Color(0.9f, 0.88f, 0.8f), 0f, 0.2f);
        SetEmission(moonMat, new Color(1f, 0.95f, 0.8f) * 2.5f);
        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = name;
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        moon.transform.position = position;
        moon.transform.localScale = Vector3.one * 2.4f;
        MeshRenderer r = moon.GetComponent<MeshRenderer>();
        r.sharedMaterial = moonMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // 영상용 카메라: 정자 입구 쪽 눈높이에서 테이블, 자수정, 혼천의, 거울이 함께 보이게. 평소에는 꺼 둠
    static void BuildShotCamera(Transform props, float tableTop)
    {
        const string name = "Mid_ShotCamera";
        foreach (Camera c in Resources.FindObjectsOfTypeAll<Camera>())
            if (c.name == name && c.gameObject.scene.IsValid())
                return;   // 이미 있으면 직접 맞춘 구도를 지킴

        GameObject go = new GameObject(name);
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60f;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        go.transform.position = props.TransformPoint(new Vector3(-0.5f, 1.6f, -1.6f));
        go.transform.LookAt(props.TransformPoint(new Vector3(0.1f, tableTop + 0.3f, 0.7f)));
        go.SetActive(false);
    }

    // ---- 도우미 ----

    // 같은 이름이 있으면 위치를 기억해 두고 지운 뒤 새로 만듦
    static Transform Reset(Transform parent, string name, Vector3 defaultPosition, Quaternion? defaultRotation = null)
    {
        Vector3 position = defaultPosition;
        Quaternion rotation = defaultRotation ?? Quaternion.identity;
        Transform old = parent.Find(name);
        if (old != null)
        {
            position = old.localPosition;
            rotation = old.localRotation;
            Object.DestroyImmediate(old.gameObject);
        }

        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localRotation = rotation;
        return t;
    }

    static Transform Empty(string name, Transform parent, Vector3 position)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        return t;
    }

    static GameObject MeshObject<T>(string name, Transform parent, Vector3 position, Vector3 euler, Material mat) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.AddComponent<T>();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void Ring(string name, Transform parent, float radius, float thickness, Vector3 euler, Material mat)
    {
        GameObject go = MeshObject<Mid_RingMesh>(name, parent, Vector3.zero, euler, mat);
        Mid_RingMesh ring = go.GetComponent<Mid_RingMesh>();
        ring.radius = radius;
        ring.thickness = thickness;
        ring.Build();
    }

    static void Spin(GameObject go, Vector3 baseEuler, Vector3 axis, float speed, float bobHeight = 0f, float bobSpeed = 1f)
    {
        Mid_MatrixSpin spin = go.AddComponent<Mid_MatrixSpin>();
        spin.baseEuler = baseEuler;
        spin.axis = axis;
        spin.degreesPerSecond = speed;
        spin.bobHeight = bobHeight;
        spin.bobSpeed = bobSpeed;
        go.transform.localRotation = Quaternion.Euler(baseEuler);
    }

    static Material Lit(string name, Color baseColor, float metallic, float smoothness)
    {
        string path = $"{Folder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void SetEmission(Material mat, Color color)
    {
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
    }
}
