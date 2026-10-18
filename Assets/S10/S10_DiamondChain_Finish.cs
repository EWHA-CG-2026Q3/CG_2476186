using UnityEngine;

// Diamond_Mesh에 붙임. DiamondMesh_Rewinded와 함께 사용.
// Chain에 부모부터 순서대로 넣으면, 행렬을 곱해 다이아몬드 꼭짓점에 적용함.
public class S10_DiamondChain_Finish : MonoBehaviour
{
    [SerializeField] Transform[] chain; // 부모부터: DiamondStage, Diamond_Orbit, Diamond_Offset, Diamond_Ball

    DiamondMesh_Rewinded diamond;

    void Awake()
    {
        diamond = GetComponent<DiamondMesh_Rewinded>();
    }

    // Animator가 이번 프레임의 값을 넣은 뒤에 실행됨
    void LateUpdate()
    {
        if (diamond == null || chain == null) return;

        // 1. 부모부터 차례로 오른쪽에 곱해 행렬 하나로 합침
        Matrix4x4 M = Matrix4x4.identity;
        foreach (Transform node in chain)
        {
            if (node == null) continue;
            M = M * Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale);
        }

        // 2. 꼭짓점마다 한 번 곱함
        Vector3[] baseVertices = diamond.BaseVertices;
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = M.MultiplyPoint(baseVertices[i]);

        diamond.SetVertices(verts);
    }
}
