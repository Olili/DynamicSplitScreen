using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoronoiSplitScreen;



public class CameraCommandStencil : MonoBehaviour {

   
    Mesh quadPerso;
    [SerializeField]private Mesh unityQuad;

    private CommandBuffer cmdBuffer;
    public Material stencilRenderer;
    [SerializeField] private int id;
#region getterSetters

    public int Id
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
        }
    }
#endregion

    void Start () {

        List<Vector3> vertices = new List<Vector3>();
        unityQuad.GetVertices(vertices);
        foreach (Vector3 vertex in vertices)
        {
            Debug.Log("v : "+vertex);
        }
        List<Vector2> Uvs = new List<Vector2>();
        unityQuad.GetUVs(0,Uvs);
        foreach (Vector2 uv in Uvs)
        {
            Debug.Log("Uv :" + uv);
        }
        int[] indices = unityQuad.GetIndices(0);
        foreach (int id in indices)
        {
            Debug.Log("id :" + id);
        }
        int[] triangles = unityQuad.GetTriangles(0);
        foreach (int triangle in triangles)
        {
            Debug.Log("triangle :" + triangle);
        }
        unityQuad.RecalculateNormals();
        unityQuad.RecalculateBounds();

    }
    void OnEnable()
    {
        if (quadPerso == null)
            quadPerso = MeshHelper.GetQuad();
        if (cmdBuffer == null)
        {
            cmdBuffer = new CommandBuffer();
            cmdBuffer.name = "Camera Stencil Mask";

            // Je prends une texture temporaire(MyCameraRd) qui est liée à la propriété de mon shader.
            int MyCameraRdID = Shader.PropertyToID("MyCameraRd");
            cmdBuffer.GetTemporaryRT(MyCameraRdID, -1, -1, 0, FilterMode.Bilinear);
            // je blit la cameraTarget dans cette MyCameraRd.
                // donc texID deviens la renderTarget
            cmdBuffer.Blit(BuiltinRenderTextureType.CameraTarget, MyCameraRdID);
                // la renderTarget devient CameraTarget
            cmdBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cmdBuffer.ClearRenderTarget(false, true, Color.black);
                // je draw dans MyCameraRd dans  MyCameraRd qui est de nouveau la renderTarget
            cmdBuffer.DrawMesh(quadPerso, Matrix4x4.identity, stencilRenderer, 0, 0);
                // je draw de renderTarget vers CameraRD avec le tempBuffer Comme texture.
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
        }
    }

}
