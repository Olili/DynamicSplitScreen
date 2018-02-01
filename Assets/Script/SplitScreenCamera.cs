using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoronoiSplitScreen
{
    public enum CameraMode {singleTarget,multipleTarget }
    public class SplitScreenCamera : MonoBehaviour
    {
        new Camera camera;
        Transform primaryTarget;
        [SerializeField]List <Transform> targetInDeadZone = new List<Transform>();
        public Vector2 targetVoronoiScreenOffset;
        [SerializeField] int id;

            // Command Buffer
        Mesh quadPerso;
        private CommandBuffer cmdBufferStencil;
        private CommandBuffer cmdBufferLastCamera;
        private Material stencilRenderer;
        static private RenderTexture lastCameraRender = null;
        static private RenderTargetIdentifier lastCameraRenderId;

        #region getterSetters
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public Transform PrimaryTarget
        {
            get
            {
                return primaryTarget;
            }
        }

        public List<Transform> TargetInDeadZone
        {
            get
            {
                return targetInDeadZone;
            }
        }
        #endregion

        #region CommandBuffer
        void InitCommmandBuffer()
        {
            if (quadPerso == null)
                quadPerso = MeshHelper.GetQuad();
           

            if (lastCameraRender == null)
            {
                lastCameraRender = new RenderTexture(Screen.width, Screen.height, 16);
                lastCameraRenderId = new RenderTargetIdentifier(lastCameraRender);
            }
            // Second command buffer test : 
            if (cmdBufferLastCamera == null)
            {
                cmdBufferLastCamera = new CommandBuffer();
                cmdBufferLastCamera.name = "cmdBufferLastCamera";
                RenderTexture active = RenderTexture.active;
                cmdBufferLastCamera.Blit(BuiltinRenderTextureType.CurrentActive, lastCameraRenderId);
                cmdBufferLastCamera.SetRenderTarget(active);
                camera.AddCommandBuffer(CameraEvent.AfterImageEffects, cmdBufferLastCamera);
            }
            if (cmdBufferStencil == null)
            {
                cmdBufferStencil = new CommandBuffer();
                cmdBufferStencil.name = "Camera Stencil Mask";

                // Je prends une texture temporaire(MyCameraRd) qui est liée à la propriété de mon shader.
                int MyCameraRdID = Shader.PropertyToID("MyCameraRd");
                cmdBufferStencil.GetTemporaryRT(MyCameraRdID, -1, -1, 0, FilterMode.Bilinear);
                // je blit la cameraTarget dans cette MyCameraRd.
                // donc texID deviens la renderTarget
                cmdBufferStencil.Blit(BuiltinRenderTextureType.CameraTarget, MyCameraRdID);
                // la renderTarget devient CameraTarget
                cmdBufferStencil.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                if (id == 0)
                    cmdBufferStencil.ClearRenderTarget(false, true, Color.black);
                else
                    cmdBufferStencil.Blit(lastCameraRenderId, BuiltinRenderTextureType.CameraTarget);
                // je draw dans MyCameraRd dans  MyCameraRd qui est de nouveau la renderTarget
                cmdBufferStencil.DrawMesh(quadPerso, Matrix4x4.identity, stencilRenderer, 0, 0);

                // je draw de renderTarget vers CameraRD avec le tempBuffer Comme texture.
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, cmdBufferStencil);

                  
            }
            
        }
        void OnDestroy()
        {
            if (cmdBufferStencil != null)
                camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, cmdBufferStencil);
            cmdBufferStencil = null;
            if (cmdBufferLastCamera!=null)
                camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, cmdBufferLastCamera);
            cmdBufferLastCamera = null;
        }
        #endregion

        public void SetID(int _id)
        {
            id = _id;
            if (stencilRenderer == null)
                stencilRenderer = new Material(Shader.Find("Hidden/StencilRenderer"));
            stencilRenderer.SetFloat("_StencilMask", id);
            camera.depth = _id; 
        }
        public void Init(Transform _primaryTarget, int _Id)
        {
            camera = GetComponent<Camera>();
            primaryTarget = _primaryTarget;
            SetID(_Id);
            InitCommmandBuffer();
            GameObject[] target = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < target.Length; i++)
            {
                Vector3 viewPortPos = camera.WorldToViewportPoint(target[i].transform.position);
                bool onScreen = viewPortPos.x >= 0.25f && viewPortPos.y >= 0.25f
                && viewPortPos.x <= 0.75f && viewPortPos.y <= 0.75f;
                if (onScreen)
                {
                    targetInDeadZone.Add(target[i].transform); ;
                }
            }
            if (targetInDeadZone.Count == 0)
            {
            }
        }
        public void UpdateTargets()
        {
            GameObject[] target = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < target.Length; i++)
            {
                Vector3 viewPortPos = camera.WorldToViewportPoint(target[i].transform.position);

                bool onScreen = viewPortPos.x >= 0.25f && viewPortPos.y >= 0.25f
                && viewPortPos.x <= 0.75f && viewPortPos.y <= 0.75f;
                if (onScreen)
                {
                        // Merge
                    if (!targetInDeadZone.Contains(target[i].transform) && target[i].transform!= primaryTarget)
                    {
                        targetInDeadZone.Add(target[i].transform);
                        Merge(primaryTarget, target[i].transform);
                    }
                }
                else
                {
                        // split  
                    if (targetInDeadZone.Contains(target[i].transform) && target[i].transform != primaryTarget)
                    {
                        targetInDeadZone.Remove(target[i].transform);
                        SplitScreenManager.Singleton.Split(this, target[i].transform);
                    }
                }
            }
            // je parcours les joueurs.
            // j'ajoute les joueurs qui sont dans ma deadZone. 
            // Si un joueur n'est plus dans ma deadZone. (=> stuff
            // Si un joueur est ajouté à ma deadZone (=>suff
        }
        public void Merge(Transform primary, Transform secondTarget)
        {
            // sur 2 camera il n'en existe plus qu'une et elle est repositionnée.
            SplitScreenManager.Singleton.Merge(primary, secondTarget, this);
        }
        public void FollowOnePlayer()
        {
            Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenOffset + Vector2.one) * 0.5f) - transform.position;
            //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
            Vector3 targetCenter = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count; i++)
            {
                targetCenter += targetInDeadZone[i].position;
            }
            targetCenter /= targetInDeadZone.Count;
            Vector3 cameraPos = targetCenter - playerOffSet;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        public void FollowMultiplePlayer()
        {
            Vector3 playerAverage = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count; i++)
                playerAverage += targetInDeadZone[i].position;
            playerAverage /= targetInDeadZone.Count;
            transform.position = new Vector3(playerAverage.x, playerAverage.y, transform.position.z);
        }
        public void Update()
        {
            UpdateTargets();
            //if (targetInDeadZone.Count < 2)
            //    FollowOnePlayer();
            //else
            //    FollowOnePlayer();
            FollowOnePlayer();

        }
        //public void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.green;
        //    Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenOffset + Vector2.one) * 0.5f) - transform.position;
        //    //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
        //    Vector3 targetCenter = Vector3.zero;
        //    for (int i = 0; i < targetInDeadZone.Count;i++)
        //    {
        //        targetCenter += targetInDeadZone[i].position;
        //    }
        //    targetCenter /= targetInDeadZone.Count;
        //    Vector3 cameraPos = targetCenter - playerOffSet;
        //    Vector3 center = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        //    Gizmos.DrawSphere(center, 0.5f);
        //}
    }

}

