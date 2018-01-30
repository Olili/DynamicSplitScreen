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
