using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraCommandStencil : MonoBehaviour {

    private CommandBuffer cmdBuffer;
    public  Material postProcessMat; // gère l'affichage partiel de ce que filme une camera selon le stencil
    public  Material material; // gère l'affichage partiel de ce que filme une camera selon le stencil
    public  Material mask2; // gère l'affichage partiel de ce que filme une camera selon le stencil
    public Material stencilRenderer;

    private RenderTargetIdentifier stencilTempTextureID;
    private RenderTexture stencilTempTexture;
    [SerializeField]private Mesh quad;

    void Start () {

        List<Vector3> vertices = new List<Vector3>();
        quad.GetVertices(vertices);
        foreach (Vector3 vertex in vertices)
        {
            Debug.Log(vertex);
        }


    }
    RenderTexture temporaryRt;
    void OnEnable()
    {
        //// Endless Miror
        //if (cmdBuffer == null)
        //{
        //    cmdBuffer = new CommandBuffer();
        //    cmdBuffer.name = "Highlight Occluded Objects";
        //    int texID = Shader.PropertyToID("CameraRd");
        //    cmdBuffer.GetTemporaryRT(texID, -1, -1, 0, FilterMode.Bilinear);
        //    stencilRenderer.SetTexture(texID, RenderTexture.active);
        //    cmdBuffer.DrawMesh(quad, Matrix4x4.identity, stencilRenderer, 0, 0);
        //    cmdBuffer.Blit(BuiltinRenderTextureType.CameraTarget, texID);
        //    cmdBuffer.Blit(texID, BuiltinRenderTextureType.CameraTarget);
        //    GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
        //}
        //// minimum mirror : 
        //if (cmdBuffer == null)
        //{
        //    cmdBuffer = new CommandBuffer();
        //    cmdBuffer.name = "Highlight Occluded Objects";
        //    int texID = Shader.PropertyToID("CameraRd");
        //    cmdBuffer.GetTemporaryRT(texID, -1, -1, 0, FilterMode.Bilinear);
        //    cmdBuffer.DrawMesh(quad, Matrix4x4.identity, stencilRenderer, 0, 0);
        //    cmdBuffer.Blit(BuiltinRenderTextureType.CameraTarget, texID);
        //    GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
        //}
        if (cmdBuffer == null)
        {
            cmdBuffer = new CommandBuffer();
            cmdBuffer.name = "Highlight Occluded Objects";
            int texID = Shader.PropertyToID("CameraRd");
            cmdBuffer.GetTemporaryRT(texID, Screen.width, Screen.height, 32);
            cmdBuffer.DrawMesh(quad, Matrix4x4.identity, stencilRenderer, 0, 0);
            cmdBuffer.Blit(BuiltinRenderTextureType.CameraTarget, texID);
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
        }
        /*
         Algo : 
         
            Je veux récupérer la texture de cameraTarget ? (= ce que la camera a déjà rendu.)
            Je veux afficher une partie de cette target sur un texture temporaire
            Je veux que cette texture temporaire soit la texture à utiliser pour le reste du process

            J'utilise drawMesh pour sélectionner une partie de la renderTexture de la camera. 
            Je peux tenter de dessiner un quad custom avec le material mais meme problème. 
         */
    }
}
