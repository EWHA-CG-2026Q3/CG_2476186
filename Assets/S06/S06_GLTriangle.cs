using UnityEngine;

public class S06_GLTriangle : MonoBehaviour
{
    private Material lineMaterial;

    void CreateMaterial()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    void OnPostRender()
    {
        CreateMaterial();
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadOrtho(); // 화면 좌표(0~1)를 그대로 사용

        GL.Begin(GL.TRIANGLES);
        GL.Color(new Color(1f, 0.6f, 0.2f, 1f));
        GL.Vertex3(0.5f, 0.8f, 0f);
        GL.Vertex3(0.2f, 0.2f, 0f);
        GL.Vertex3(0.8f, 0.2f, 0f);
        GL.End();

        GL.PopMatrix();
    }
}
