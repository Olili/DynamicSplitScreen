using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    using Voronoi2;
    public class SplitScreenManager : MonoBehaviour
    {
        static SplitScreenManager singleton;
        List<SplitScreenCamera> splitCameraList;
        [SerializeField] GameObject[] targets;
        [SerializeField] Material debugMat;
        [SerializeField] Material stencilDrawer;
        [SerializeField] Material[] stencilDrawerTab;
        [SerializeField] SplitScreenCamera splitCameraModel;

        Color[] debugColor = new Color[6] { Color.white, Color.black,Color.magenta, Color.red,  Color.cyan,Color.grey};
        List <Mesh> polyMaskList;
        public GameObject[] Targets
        {
            get{return targets;}

            set{targets = value;}
        }

        static public SplitScreenManager Singleton
        {
            get
            {
                if (singleton == null)
                    Debug.LogError("SplitScreenManager Missing");
                return singleton;
            }
        }

        public void ComputeWorldBounds(out Bounds worldBounds, out Vector3[] voronoiSitePos)
        {
            worldBounds = new Bounds();
            voronoiSitePos = new Vector3[splitCameraList.Count];
            Vector2 newExtents = Vector2.zero;
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0; i < targets.Length; i++)
            {
                playerAveragePosition += new Vector2(targets[i].transform.position.x, targets[i].transform.position.y);
            }
            worldBounds.center = playerAveragePosition /= targets.Length;

            for (int i = 0; i < splitCameraList.Count; i++)
            {
                for (int j = 0; j < splitCameraList[i].TargetInDeadZone.Count; j++)
                {
                    voronoiSitePos[i] += splitCameraList[i].TargetInDeadZone[j].position;
                }
                voronoiSitePos[i] = voronoiSitePos[i] / splitCameraList[i].TargetInDeadZone.Count - worldBounds.center;
                //voronoiSitePos[i] = (targets[i].transform.position );
                voronoiSitePos[i].z = 0;
                newExtents = worldBounds.extents;
             
                    // for testing purpose
                //float aspect = Camera.main.aspect;
                float aspect = 1.0f*Screen.currentResolution.width / Screen.currentResolution.height;
                aspect = 1.0f * Screen.width / Screen.height;

                if (newExtents.x < Mathf.Abs(voronoiSitePos[i].x))
                {
                    newExtents.x = Mathf.Abs(voronoiSitePos[i].x);
                    newExtents.y = newExtents.x / aspect;
                }
                if (newExtents.y < Mathf.Abs(voronoiSitePos[i].y))
                {
                    newExtents.y = Mathf.Abs(voronoiSitePos[i].y);
                    newExtents.x = newExtents.y * aspect;
                }
                    // Min camera Size for player.
                worldBounds.extents = newExtents;
            }
            worldBounds.extents = newExtents*2;
        }
        public void ResizeStencilPolygoneMesh(Bounds worldBounds ,Vector3[] targetVoronoiPos)
        {
            Voronoi voronoi = new Voronoi(0.1f);

            double[] x = new double[targets.Length];
            double[] y = new double[targets.Length];

            boundsGizmo = worldBounds;
            for (int i = 0; i < targetVoronoiPos.Length; i++)
            {
                x[i] = targetVoronoiPos[i].x;
                y[i] = targetVoronoiPos[i].y;
            }
            List<GraphEdge> edges = voronoi.generateVoronoi(x, y, -worldBounds.extents.x, worldBounds.extents.x,
                                                                  -worldBounds.extents.y, worldBounds.extents.y);
            if (edges == null || edges.Count <= 0)
            {
                Debug.Log("Error with Voronoi edge List");
                return;
            }
            polyMaskList.Clear();
            for (int i = 0; i < targetVoronoiPos.Length; i++)
                polyMaskList.Add(MeshHelper.GetPolygon(i, edges, targetVoronoiPos, worldBounds));
        }
        public SplitScreenCamera GetSplitCamera(Transform target)
        {
            for (int i = 0; i < splitCameraList.Count;i++)
            {
                if (splitCameraList[i].PrimaryTarget == target)
                    return splitCameraList[i];
            }
            return null;
        }
        public void Merge(Transform targetOne, Transform targetTwo, SplitScreenCamera firstCam)
        {
            RemoveCamera(GetSplitCamera(targetTwo));
        }
        public void Split(SplitScreenCamera splitOne,Transform exitingTarget)
        {
            SplitScreenCamera newCamera =  AddCamera(exitingTarget, splitOne.transform.position);
            newCamera.TargetInDeadZone.Add(exitingTarget);
            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);
            // J'ajoute une nouvelle camera. 
            // je lui donne les targetsInFrustrum necessaire. 
            // je reRemplis la targetInFrustrum pour la camera1.
            // je calcule son offset. (est-ce vraiment necessaire
        }
        public SplitScreenCamera AddCamera(Transform primaryTarget,Vector3 position)
        {
            SplitScreenCamera newCamera = Instantiate(splitCameraModel, position, splitCameraList[0].transform.rotation, transform);
            splitCameraList.Add(newCamera);
            newCamera.Init(primaryTarget, splitCameraList.Count-1);
            return newCamera;
        }
        public void RemoveCamera(SplitScreenCamera cam)
        {
            if (cam == null || cam == splitCameraList[0])
                return;
            splitCameraList.Remove(cam);
            Destroy(cam.gameObject);

            for (int i = 0; i < splitCameraList.Count;i++)
                splitCameraList[i].SetID(i);
        }

        public void Awake()
        {
            singleton = this;
            polyMaskList = new List<Mesh>();
            splitCameraList = new List<SplitScreenCamera>();
            stencilDrawerTab = new Material[4];
            for (int i = 0; i < stencilDrawerTab.Length; i++)
            {
                stencilDrawerTab[i] = new Material(Shader.Find("Stencils/StencilDrawer"));
                stencilDrawerTab[i].SetFloat("_StencilMask", i);
            }

        }
       
        public void UpdateCameraTargetOffset(Vector3[] targetVoronoiPos,Bounds voronoiBounds)
        {
            
            for (int i = 0; i < splitCameraList.Count; i++)
            {
                if (voronoiBounds.extents != Vector3.zero)
                {
                    splitCameraList[i].targetVoronoiScreenOffset.x = targetVoronoiPos[i].x / voronoiBounds.extents.x;
                    splitCameraList[i].targetVoronoiScreenOffset.y = targetVoronoiPos[i].y / voronoiBounds.extents.y;
                }
                else
                {
                    splitCameraList[i].targetVoronoiScreenOffset = Vector2.zero;
                }
                    
            }
        }
        public void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                SplitScreenCamera splitCamera = transform.GetChild(i).GetComponentInChildren<SplitScreenCamera>();
                splitCamera.Init(targets[i].transform, i);
                splitCameraList.Add(splitCamera);
            }
        }
        public void Update()
        {
            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);

            if (splitCameraList.Count > 1)
                ResizeStencilPolygoneMesh(worldBounds, targetVoronoiPos);

            UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);

            if (splitCameraList.Count > 1)
                for (int i= 0; i <splitCameraList.Count;i++)
                {
                    Vector3 position = splitCameraList[0].transform.position + splitCameraList[0].transform.forward;
                    Graphics.DrawMesh(polyMaskList[i], position, Quaternion.identity, stencilDrawerTab[i],0, splitCameraList[0].GetComponent<Camera>(), 0);
                }
        }


        Bounds boundsGizmo;
        public void OnDrawGizmos()
        {
            if (boundsGizmo!=null)
            {
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(boundsGizmo.size.x, 0, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(boundsGizmo.size.x, 0, 0));

                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(0, boundsGizmo.size.y, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(0, boundsGizmo.size.y, 0));


                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(boundsGizmo.center, 0.2f);
            }
        }
    }

    
}

