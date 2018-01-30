using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoronoiSplitScreen
{
    public class SplitScreenCamera : MonoBehaviour
    {
        new Camera camera;
        Transform primaryTarget;
        List <Transform> targetInFrustrum;
        public Vector2 targetVoronoiScreenPos;
        int id;

            // Command Buffer
        Mesh quadPerso;
        private CommandBuffer cmdBuffer;
        private Material stencilRenderer;


        #region getterSetters
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        #region CommandBuffer
        void OnStart()
        {
            if (quadPerso == null)
                quadPerso = MeshHelper.GetQuad();
            if (stencilRenderer== null)
            {
                stencilRenderer = new Material(Shader.Find("Hidden/StencilRenderer"));
                stencilRenderer.SetFloat("_StencilMask", id);
            }

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
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
            }
        }
        void OnDisable()
        {
            if (cmdBuffer != null)
                camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, cmdBuffer);
            cmdBuffer = null;
        }
        #endregion


        public void Init(Transform _primaryTarget, int _Id)
        {
            camera = GetComponent<Camera>();
            id = _Id;
            primaryTarget = _primaryTarget;
            OnStart();
        }
        public void UpdateTargets()
        {
            Vector3 primaryTargetScreenPoint = camera.WorldToViewportPoint(primaryTarget.position);
            bool onScreen = primaryTargetScreenPoint.z > 0 && primaryTargetScreenPoint.x > 0 
                && primaryTargetScreenPoint.x < 1 && primaryTargetScreenPoint.y > 0 && primaryTargetScreenPoint.y < 1;

            for (int i = 0; i < targetInFrustrum.Count;i++)
            {
            }
        }
        public void Merge()
        {
        }
        public void Split()
        {
        }
        public void Follow()
        {
            Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenPos + Vector2.one) * 0.5f) - transform.position;
            Vector3 cameraPos = primaryTarget.transform.position - playerOffSet;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        public void Update()
        {
            Follow();
        }
    }

}

