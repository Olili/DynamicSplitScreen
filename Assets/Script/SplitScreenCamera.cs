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
        public void UpdateTargetWithDeadZone()
        {
            GameObject[] targets = SplitScreenManager.Singleton.Targets;
            for (int i = 0; i < targets.Length;i++)
            {
                if (targets[i].transform == targetInDeadZone[0])
                    continue;
                Vector3 screenPosition = camera.WorldToViewportPoint(targets[i].transform.position);
                bool onScreen = (screenPosition.x > 0.25 && screenPosition.x < 0.75
                    && screenPosition.y > 0.25 && screenPosition.y < 0.75);
                if (onScreen)
                {
                    if (!targetInDeadZone.Contains(targets[i].transform))
                    {
                        SplitScreenManager.Singleton.Merge(targets[i].transform, this);
                        break;
                    }
                }
                else
                {
                    if (targetInDeadZone.Contains(targets[i].transform))
                    {
                        SplitScreenManager.Singleton.Split(this);
                        break;
                    }
                }
            }
        }
        public float ComputeScreenDistance(Transform otherTarget,int otherTargetId)
        {
            Vector3 VoronoiScreenOffset = SplitScreenManager.Singleton.GetPrimaryVoronoiIndication(this);

            Vector3 targetScreenPos = camera.ViewportToWorldPoint(VoronoiScreenOffset) - transform.position;
            Vector3 otherVoronoiScreenOffset = SplitScreenManager.Singleton.voronoiCameraIndication[otherTargetId];
            Vector3 otherScreenPos = camera.ViewportToWorldPoint(otherVoronoiScreenOffset) - transform.position;
            float screenDistance = Vector3.Distance(targetScreenPos, otherScreenPos);
            float worldDistance = Vector3.Distance(TargetInDeadZone[0].position, otherTarget.transform.position);
            return  screenDistance- worldDistance;
        }
        public void UpdateTargets()
        {
            GameObject[] target = SplitScreenManager.Singleton.Targets;

            Vector3 VoronoiScreenOffset = SplitScreenManager.Singleton.GetPrimaryVoronoiIndication(this);
            Vector3 targetScreenPos = camera.ViewportToWorldPoint(VoronoiScreenOffset) - transform.position;
            targetScreenPos.z = 0;
            
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i].transform == targetInDeadZone[0])
                    continue;
                Vector3 otherScreenPos = camera.ViewportToWorldPoint(SplitScreenManager.Singleton.voronoiCameraIndication[i]) - transform.position;

                otherScreenPos.z = 0;
                float screenDistance = Vector3.Distance(targetScreenPos, otherScreenPos);
                float worldDistance = Vector3.Distance(TargetInDeadZone[0].position, target[i].transform.position);
                    
                if (worldDistance < screenDistance) // Merge
                    {
                    if (!targetInDeadZone.Contains(target[i].transform))
                    {
                        SplitScreenManager.Singleton.Merge(target[i].transform, this);
                        break;
                    }
                }
                else // split
                {
                    if (targetInDeadZone.Contains(target[i].transform))
                    {
                        SplitScreenManager.Singleton.Split(this);
                        break;
                    }
                }
            }
        }
        public Vector3 GetPosition()
        {
            Vector3 voronoiScreenOffset = Vector3.zero;
            for (int i = 0; i < TargetInDeadZone.Count; i++)
                for (int j = 0; j < SplitScreenManager.Singleton.Targets.Length; j++)
                    if (targetInDeadZone[i] == SplitScreenManager.Singleton.Targets[j].transform)
                    {
                        Vector3 Vconvert = SplitScreenManager.Singleton.voronoiCameraIndication[j];
                        voronoiScreenOffset += Vconvert;
                    }
            voronoiScreenOffset /= TargetInDeadZone.Count;
            Vector3 playerOffSet = camera.ViewportToWorldPoint(voronoiScreenOffset) - transform.position;
            Vector3 targetCenter = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count; i++)
                targetCenter += targetInDeadZone[i].position;
            targetCenter *= ((float)1 / targetInDeadZone.Count);

            Vector3 cameraPos = targetCenter - playerOffSet;
            return new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        public void FollowMultiplePlayer()
        {
            Vector3 voronoiScreenOffset = Vector3.zero;
            for (int i = 0; i < TargetInDeadZone.Count; i++)
                for (int j = 0; j < SplitScreenManager.Singleton.Targets.Length;j++)
                    if (targetInDeadZone[i] == SplitScreenManager.Singleton.Targets[j].transform)
                    {
                        Vector3 Vconvert = SplitScreenManager.Singleton.voronoiCameraIndication[j];
                        voronoiScreenOffset += Vconvert;
                    }
            voronoiScreenOffset /= TargetInDeadZone.Count;
            Vector3 playerOffSet = camera.ViewportToWorldPoint(voronoiScreenOffset) - transform.position;
            Vector3 targetCenter = Vector3.zero;
            for (int i = 0; i < targetInDeadZone.Count;i++)
                targetCenter+= targetInDeadZone[i].position;
            targetCenter *= ((float)1 / targetInDeadZone.Count);

            Vector3 cameraPos = targetCenter - playerOffSet;
            //transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
            FatherCameraPosition(new Vector3(cameraPos.x, cameraPos.y, transform.position.z));
        }
       
        public void FollowOnePlayer()
        {
            if (targetInDeadZone.Count == 0) return;
            Vector3 voronoiScreenOffset = SplitScreenManager.Singleton.GetPrimaryVoronoiIndication(this);
            Vector3 playerOffSet = camera.ViewportToWorldPoint(voronoiScreenOffset) - transform.position;
            //Vector3 playerOffSet = camera.ViewportToWorldPoint(new Vector3(1,1,0)) - transform.position;
            Vector3 targetCenter = targetInDeadZone[0].position;
            Vector3 cameraPos = targetCenter - playerOffSet;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }
        // je lerp entre la ou je suis et la ou je serais si on filmait
        // 2 joueurs en meme temps. 
        public void FatherCameraPosition(Vector3 cameraPosition)
        {
            SplitScreenCamera otherCamera =null;
            Vector3 cameraCenter = Vector3.zero;
            float t = 0;
            List<SplitScreenCamera> splitCameraList = SplitScreenManager.Singleton.splitCameraList;
            Vector3 lerpOffset = Vector3.zero;
            for  (int i = 0; i < splitCameraList.Count;i++)
            {
                if (splitCameraList[i]!=this)
                {
                    // calculer le centre en les 2 centre de 2 camera Voronoï
                     cameraCenter = (splitCameraList[i].GetPosition() + GetPosition()) * 0.5f;
                    
                    // calculer vrai centre en 2 cameras : 
                    //cameraCenter = (transform.position + splitCameraList[i].transform.position) * 0.5f;
                    Transform otherTarget = splitCameraList[i].targetInDeadZone[0];
                    float minDistance = ComputeScreenDistance(otherTarget, SplitScreenManager.Singleton.GetTargetId(otherTarget));
                    t = (minDistance + 5)/5;
                    t = t < 0 ? 0 : t;
                    t = t > 1 ? 1 : t;
                    lerpOffset += (cameraCenter - cameraPosition) * t;
                }
            }
            Vector3 finalPosition = cameraPosition + lerpOffset;
            transform.position = finalPosition;
        }
       


        public void Update()
        {
            FollowMultiplePlayer();
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

