using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoronoiSplitScreen
{
   
    public class SplitScreenCamera : MonoBehaviour
    {
        new Camera camera;
        [SerializeField] int id;
        private List<TargetData> targetsData;

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
        public void Init(TargetData targetData, int _Id)
        {
            targetsData = new List<TargetData>();
            camera = GetComponent<Camera>();
            targetsData.Add(targetData);
            SetID(_Id);
            InitCommmandBuffer();
            targetData.camera = this;
        }
        public void AddTarget(TargetData _targetData)
        {
            targetsData.Add(_targetData);
            _targetData.camera = this;
        }
        public void RemoveTarget(TargetData _targetData)
        {
            targetsData.Remove(_targetData);
        }
        public TargetData GetFarthestTarget()
        {
            TargetData farthestTarget = null;
            float maxDist = 0;
            for (int i = 1; i < targetsData.Count; i++)
            {
                float distance = Vector3.Distance(targetsData[i].target.position, transform.position);
                if (distance > maxDist)
                {
                    maxDist = distance;
                    farthestTarget = targetsData[i];
                }
            }
            return farthestTarget;
        }
        public void GetAllTargetInDeadZone()
        {
        }
        // a revoir mal nommé et on peut faire d'autre algo
        public void UpdateTargets()
        {
            Vector3 targetScreenPos = camera.ViewportToWorldPoint(targetsData[0].voronoiRegionCenter) - transform.position;
            targetScreenPos.z = 0;
            List<TargetData> allTargets = SplitScreenManager.Singleton.targetsData;
            for (int i = 0; i < allTargets.Count; i++)
            {
                if (allTargets[i] == targetsData[0])
                    continue;
                Vector3 otherScreenPos = camera.ViewportToWorldPoint(allTargets[i].voronoiRegionCenter) - transform.position;
                otherScreenPos.z = 0;
                float screenDistance = Vector3.Distance(targetScreenPos, otherScreenPos);
                float worldDistance = Vector3.Distance(targetsData[0].target.position, allTargets[i].target.position);
                if (worldDistance < screenDistance)
                {
                    if (!targetsData.Contains(allTargets[i]))  // Merge
                    {
                        SplitScreenManager.Singleton.Merge(allTargets[i], this);
                        break;
                    }
                }
                else 
                {
                    if (targetsData.Contains(allTargets[i])) // split
                    {
                        SplitScreenManager.Singleton.Split(this);
                        break;
                    }
                }
            }
        }

        public float ComputeScreenDistance(List<TargetData> otherTarget)
        {
            Vector3 VoronoiScreenOffset = targetsData[0].voronoiRegionCenter;
            Vector3 targetScreenPos = camera.ViewportToWorldPoint(VoronoiScreenOffset) - transform.position;
            Vector3 otherVoronoiScreenOffset = otherTarget[0].voronoiRegionCenter;
            Vector3 otherScreenPos = camera.ViewportToWorldPoint(otherVoronoiScreenOffset) - transform.position;
            float screenDistance = Vector3.Distance(targetScreenPos, otherScreenPos);
            float worldDistance = Vector3.Distance(targetsData[0].target.position, otherTarget[0].target.position);
            return screenDistance - worldDistance;
        }

        public Vector3 GetPosition(List<TargetData> groupTargetList = null)
        {
            Vector2 screenOffset = Vector3.zero;
            if (groupTargetList == null)
                groupTargetList = targetsData;
            for (int i = 0; i < groupTargetList.Count; i++)
                screenOffset += groupTargetList[i].voronoiRegionCenter;
            screenOffset /= groupTargetList.Count;
            Vector3 targetOffset = camera.ViewportToWorldPoint(screenOffset) - transform.position;

            Vector3 targetCenter = Vector3.zero;
            for (int i = 0; i < groupTargetList.Count; i++)
                targetCenter += groupTargetList[i].target.position;
            targetCenter /= (groupTargetList.Count);

            Vector3 cameraPos = targetCenter - targetOffset;
            return new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
        }

        public void FollowMultiplePlayer()
        {
            Vector3 cameraPos = GetPosition();
            FeatherCameraPosition(new Vector3(cameraPos.x, cameraPos.y, transform.position.z));
        }
        /*
         *  Autre type de lerp : 
            J'ai la position que devrait avoir la camera par
            rapport à ses targets.
            La position que devrait avoir la camera si elle avait aussi une autre target.
            La distance à laquelle devrait être l'autre target pour être dans le meme ecran.
            Le lerp de 0 à +5.
                OU 
            je lerp entre le centre d'une camera et le centre d'une autre camera.(mauvais ratio entre gens)
                OU
            Ou je lerp entre ma camera et une camera qui contiendrait l'ensemble des targets de l'autre camera.
            (actuel)
         */

        public void FeatherCameraPosition(Vector3 cameraPosition)
        {
            SplitScreenCamera otherCamera = null;
            float t = 0;
            List<SplitScreenCamera> splitCameraList = SplitScreenManager.Singleton.splitCameraList;
            Vector3 lerpOffset = Vector3.zero;
            Vector3 cameraCenter = Vector3.zero;
            List<TargetData> twoCameraList = new List<TargetData>();
            for (int i = 0; i < splitCameraList.Count; i++)
            {
                if (splitCameraList[i] != this)
                {
                    twoCameraList.Clear();
                    twoCameraList.AddRange(targetsData);
                    twoCameraList.AddRange(splitCameraList[i].targetsData);
                    cameraCenter = GetPosition(twoCameraList);

                    float minDistance = ComputeScreenDistance(splitCameraList[i].targetsData);
                    t = (minDistance + 5) / 5;
                    t = t < 0 ? 0 : t;
                    t = t > 1 ? 1 : t;
                    lerpOffset += (cameraCenter - cameraPosition) * t;
                }
            }
            Vector3 finalPosition = cameraPosition + lerpOffset;
            transform.position = finalPosition;
        }

        /*Compute 2 distance
         *          ScreenStuff : 
         *                  Le viewport place of current camera
         *                  Le viewport place of merged group camera
         *                  --> What distance does it make in world space.
         *          WorldStuff: 
         *                  The current world Position of camera.
         *                  The world position of merged groupe camera.
         *                  --> What is this distance ? 
         */


       
        public void DrawPolyMask(Camera targetCamera,Material stencilDrawerTab)
        {
            //targetCamera = camera;
            for (int i = 0;i < targetsData.Count;i++)
            {
                Vector3 position = targetCamera.transform.position + targetCamera.transform.forward;
                Graphics.DrawMesh(targetsData[i].polyMask, position, Quaternion.identity, stencilDrawerTab, 0, targetCamera);
                Material debugMat = new Material(Shader.Find("Sprites/Default"));
                debugMat.color = SplitScreenManager.debugColor[i];
                Graphics.DrawMesh(targetsData[i].polyMask, position, Quaternion.identity, debugMat, 0, targetCamera);
            }
        }

        public void Update()
        {
            FollowMultiplePlayer();
            UpdateTargets();
        }
        
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

