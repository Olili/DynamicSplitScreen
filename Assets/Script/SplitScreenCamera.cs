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
            //targetInDeadZone.Add(_primaryTarget);
            SetID(_Id);
            InitCommmandBuffer();
        }
        public void GetAllTargetInDeadZone()
        {
            GameObject[] target = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < target.Length; i++)
            {
                Vector3 viewPortPos = camera.WorldToViewportPoint(target[i].transform.position);
                bool onScreen = viewPortPos.x >= 0.25f && viewPortPos.y >= 0.25f
                && viewPortPos.x <= 0.75f && viewPortPos.y <= 0.75f;
                if (onScreen)
                {
                    if (!targetInDeadZone.Contains(target[i].transform))
                        targetInDeadZone.Add(target[i].transform);
                }
            }
        }
        public void UpdateTargets()
        {
            GameObject[] target = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < target.Length; i++)
            {
                Vector3 viewPortPos = camera.WorldToViewportPoint(target[i].transform.position);

                bool onScreen = viewPortPos.x >= 0.245f && viewPortPos.y >= 0.245f
                && viewPortPos.x <= 0.755f && viewPortPos.y <= 0.755f;
                if (onScreen)
                {
                    // Merge
                    if (!targetInDeadZone.Contains(target[i].transform))
                    {
                        //Debug.Log(this +" TryMerging " + target[i].transform);
                        //Merge(primaryTarget, target[i].transform);
                        SplitScreenManager.Singleton.Merge(target[i].transform, this);
                        break;
                    }
                }
                else
                {
                    // split  
                    if (targetInDeadZone.Contains(target[i].transform))
                    {
                        //targetInDeadZone.Remove(target[i].transform);

                        SplitScreenManager.Singleton.Split(this);
                        Debug.Log(this + " Splitting " + target[i].transform);
                        //Debug.Break();
                        break;
                    }
                }
            }
            // je parcours les joueurs.
            // j'ajoute les joueurs qui sont dans ma deadZone. 
            // Si un joueur n'est plus dans ma deadZone. (=> stuff
            // Si un joueur est ajouté à ma deadZone (=>suff
        }

        //public void FollowOnePlayer()
        //{
        //    if (targetInDeadZone.Count == 0) return;

        //    Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenOffset + Vector2.one) * 0.5f) - transform.position;
        //    //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
        //    Vector3 targetCenter = Vector3.zero;
        //    for (int i = 0; i < targetInDeadZone.Count; i++)
        //    {
        //        targetCenter += targetInDeadZone[i].position;
        //    }
        //    targetCenter /= targetInDeadZone.Count;
        //    Vector3 cameraPos = targetCenter - playerOffSet;
        //    transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        //}
        public void FollowOnePlayer()
        {
            if (targetInDeadZone.Count == 0) return;

            Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenOffset + Vector2.one) * 0.5f) - transform.position;
            //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
            Vector3 targetCenter = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count; i++)
            //for (int i = 0; i < SplitScreenManager.Singleton.Targets.Length; i++)
            {
                targetCenter += targetInDeadZone[i].transform.position;
                //targetCenter += SplitScreenManager.Singleton.Targets[i].transform.position;
            }
            targetCenter /= targetInDeadZone.Count;
            //targetCenter /= SplitScreenManager.Singleton.Targets.Length;
            Vector3 cameraPos = targetCenter - playerOffSet;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        //public void FollowOnePlayer()
        //{
        //    if (targetInDeadZone.Count == 0) return;

        //    //Vector3 playerOffSet = camera.ViewportToWorldPoint((targetVoronoiScreenOffset + Vector2.one) * 0.5f) - transform.position;
        //    //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
        //    Vector3 targetCenter = Vector3.zero;
        //    for (int i = 0; i < SplitScreenManager.Singleton.Targets.Length; i++)
        //    {
        //        targetCenter += SplitScreenManager.Singleton.Targets[i].transform.position;
        //    }
        //    //targetCenter /= targetInDeadZone.Count;
        //    targetCenter /= SplitScreenManager.Singleton.Targets.Length;



        //    Vector3 cameraPos = targetCenter + Vector3.Scale(targetVoronoiScreenOffset, SplitScreenManager.Singleton.testVoronoiBounds.extents);
        //    transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        //}

        public void Update()
        {
            //if (targetInDeadZone.Count < 2)
            //    FollowOnePlayer();
            //else
            //    FollowOnePlayer();
            FollowOnePlayer();
            UpdateTargets();
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
        public void OnDrawGizmos()
        {

            if (Application.isPlaying)
            {
                Vector3 center = camera.transform.position;
                Vector3 upLeft = camera.ViewportToWorldPoint(new Vector3(0.25f, 0.75f));
                Vector3 upRight = camera.ViewportToWorldPoint(new Vector3(0.75f, 0.75f));
                Vector3 downRight = camera.ViewportToWorldPoint(new Vector3(0.75f, 0.25f));
                Vector3 downLeft = camera.ViewportToWorldPoint(new Vector3(0.25f, 0.25f));
                Gizmos.color = Color.red;
                Gizmos.DrawLine(upLeft, upRight);
                Gizmos.DrawLine(upRight, downRight);

                Gizmos.DrawLine(downRight, downLeft);
                Gizmos.DrawLine(downLeft, upLeft);
            }
        }
    }

}

