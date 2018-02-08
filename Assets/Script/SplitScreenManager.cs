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

        Color[] debugColor = new Color[6] { Color.magenta,Color.white, Color.black, Color.red,  Color.cyan,Color.grey};
        List <Mesh> polyMaskList;

        public Vector2[] voronoiRegionPoints; // map [0,1]
        public Vector2[] voronoiRegionCenter; // map [0,1]
        public Vector2[] voronoiCameraIndication; // map [0,1]
        public Bounds testVoronoiBounds;

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
       
        void ComputePlayerScreenPosition()
        {
            voronoiRegionCenter = new Vector2[polyMaskList.Count];
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < polyMaskList.Count; i++)
            {
                //polyMaskList[i].
                // Compute average
                polyMaskList[i].GetVertices(vertices);
                voronoiRegionCenter[i] = Vector3.zero;
                for (int j = 0; j < vertices.Count; j++)
                {
                    vertices[j] = new Vector3(vertices[j].x, vertices[j].y * -1, 0);
                }
                voronoiRegionCenter[i] = MeshHelper.Compute2DPolygonCentroid(vertices);
                Debug.DrawLine(Vector3.zero, voronoiRegionCenter[i], Color.yellow);
                voronoiRegionCenter[i] = (voronoiRegionCenter[i] + Vector2.one) * 0.5f;
            }

        }
        public void ComputeWorldBounds(out Bounds worldBounds, out Vector3[] voronoiSitePos)
        {
            worldBounds = new Bounds();
            voronoiSitePos = new Vector3[Targets.Length];
            voronoiRegionPoints = new Vector2[Targets.Length];
            Vector2 newExtents = Vector2.zero;
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0; i < targets.Length; i++)
            {
                playerAveragePosition += new Vector2(targets[i].transform.position.x, targets[i].transform.position.y);
            }
            worldBounds.center = (playerAveragePosition /= targets.Length);

            for (int i = 0; i < targets.Length; i++)
            {
                voronoiSitePos[i] = targets[i].transform.position - worldBounds.center;
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
            worldBounds.extents = newExtents*2f;
            for (int i = 0; i < targets.Length;i++)
            {
                voronoiRegionPoints[i].x = voronoiSitePos[i].x / worldBounds.extents.x;
                voronoiRegionPoints[i].y = voronoiSitePos[i].y / worldBounds.extents.y;
                voronoiRegionPoints[i] = (voronoiRegionPoints[i] + Vector2.one) * 0.5f;
            }
            testVoronoiBounds = worldBounds;
        }
        public void ResizeStencilPolygoneMesh(Bounds worldBounds ,Vector3[] targetVoronoiPos)
        {
            Voronoi voronoi = new Voronoi(0.1f);

            double[] x = new double[targetVoronoiPos.Length];
            double[] y = new double[targetVoronoiPos.Length];

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
            ComputePlayerScreenPosition();
        }
        public SplitScreenCamera GetSplitCamera(Transform target)
        {
            for (int i = 0; i < splitCameraList.Count;i++)
            {
                for (int j = 0; j < splitCameraList[i].TargetInDeadZone.Count;j++)
                {
                    if (splitCameraList[i].TargetInDeadZone[j] == target)
                    {
                        return splitCameraList[i];
                    }
                }
            }
            return null;
        }
        public void Merge(Transform targetTwo, SplitScreenCamera firstCam)
        {
            SplitScreenCamera toRemove = GetSplitCamera(targetTwo);
            if (toRemove == splitCameraList[0])
                return;
            Debug.Log(firstCam + " Merging " + targetTwo);
            RemoveCamera(toRemove);
            firstCam.TargetInDeadZone.Add(targetTwo);

            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            //UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);
        }
        public void Split(SplitScreenCamera splitOne)
        {
                // on récupère le connard qui s'en va

            Transform farthestTarget = null;
            float maxDist = 0;
            for (int i = 1;i < splitOne.TargetInDeadZone.Count;i++)
            {
                float distance = Vector3.Distance(splitOne.TargetInDeadZone[i].position, splitOne.transform.position);
                if (distance > maxDist)
                {
                    maxDist = distance;
                    farthestTarget = splitOne.TargetInDeadZone[i];
                }
            }
            Debug.Log(splitOne + " Splitting " + farthestTarget);
            if (splitOne.TargetInDeadZone.Count == 1)
            {
                Debug.Log("Stop");
            }
            splitOne.TargetInDeadZone.Remove(farthestTarget);
                // Actualise target et position des 2 cameras.
            SplitScreenCamera newCamera = AddCamera(farthestTarget, splitOne.transform.position);
            newCamera.TargetInDeadZone.Add(farthestTarget);


            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            //UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);

            newCamera.FollowOnePlayer();
            splitOne.FollowOnePlayer();
                // FOR DEBUG : 
            //newCamera.UpdateTargets();
            //splitOne.UpdateTargets();

        }
        
        public SplitScreenCamera AddCamera(Transform primaryTarget,Vector3 position)
        {
            SplitScreenCamera newCamera = Instantiate(splitCameraModel, position, splitCameraList[0].transform.rotation, transform);
            splitCameraList.Add(newCamera);
            newCamera.Init(primaryTarget, splitCameraList.Count-1);
            newCamera.name = "splitCamera" + (splitCameraList.Count - 1);
            return newCamera;
        }
        public void RemoveCamera(SplitScreenCamera cam)
        {
            if (cam == null || cam == splitCameraList[0])
                return;
            splitCameraList.Remove(cam);
            DestroyImmediate(cam.gameObject);

            for (int i = 0; i < splitCameraList.Count;i++)
                splitCameraList[i].SetID(i);
        }
       

        //public void UpdateCameraTargetOffset(Vector3[] targetVoronoiPos,Bounds voronoiBounds)
        //{
        //    for (int i = 0; i < splitCameraList.Count; i++)
        //    {
        //        SplitScreenCamera splitCamera = splitCameraList[i];
        //        if (voronoiBounds.extents != Vector3.zero)
        //        {
        //            for (int j = 0; j < Targets.Length; j++)
        //            {
        //                Transform target = Targets[j].transform;
        //                if (splitCamera.TargetInDeadZone[0] == target)
        //                {
        //                    splitCamera.targetVoronoiScreenOffset.x = targetVoronoiPos[j].x / voronoiBounds.extents.x;
        //                    splitCamera.targetVoronoiScreenOffset.y = targetVoronoiPos[j].y / voronoiBounds.extents.y;
        //                    targetVoronoiOffsetTest[j] = splitCamera.targetVoronoiScreenOffset;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            splitCamera.targetVoronoiScreenOffset = Vector2.zero;
        //        }
        //    }
        //}
        public void UpdateCameraTargetOffset(Vector3[] targetVoronoiPos, Bounds voronoiBounds)
        {
            for (int i = 0; i < splitCameraList.Count; i++)
            {
                SplitScreenCamera splitCamera = splitCameraList[i];
                if (voronoiBounds.extents != Vector3.zero)
                {
                    for (int j = 0; j < Targets.Length; j++)
                    {
                        Transform target = Targets[j].transform;
                        if (splitCamera.TargetInDeadZone[0] == target)
                        {
                            splitCamera.targetVoronoiScreenOffset = voronoiRegionCenter[j];
                        }
                    }
                }
                else
                {
                    splitCamera.targetVoronoiScreenOffset = Vector2.zero;
                }
                voronoiCameraIndication = voronoiRegionPoints;
            }
        }
        public Vector3 GetPrimaryVoronoiIndication(SplitScreenCamera camera)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (camera.TargetInDeadZone[0] == targets[i].transform)
                    return voronoiCameraIndication[i];
            }
            Debug.LogError("Camera has no targets");
            return Vector3.zero;
        }

        public void Awake()
        {
            singleton = this;
            polyMaskList = new List<Mesh>();
            splitCameraList = new List<SplitScreenCamera>();
            stencilDrawerTab = new Material[7];
            for (int i = 0; i < stencilDrawerTab.Length; i++)
            {
                stencilDrawerTab[i] = new Material(Shader.Find("Stencils/StencilDrawer"));
                stencilDrawerTab[i].SetFloat("_StencilMask", i);
            }
            
        }
        public void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                SplitScreenCamera splitCamera = transform.GetChild(i).GetComponentInChildren<SplitScreenCamera>();
                splitCamera.Init(targets[i].transform, i);
                splitCameraList.Add(splitCamera);
                splitCamera.GetAllTargetInDeadZone();
            }
        }
        
        public void Update()
        {
            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);

            //if (splitCameraList.Count > 1)
                ResizeStencilPolygoneMesh(worldBounds, targetVoronoiPos);

            UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);

            if (splitCameraList.Count > 1)
            {
                for (int i = 0; i < splitCameraList.Count; i++)
                {
                    SplitScreenCamera splitCamera = splitCameraList[i];
                    for (int j = 0; j < Targets.Length; j++)
                    {
                        Transform target = Targets[j].transform;
                        if (splitCamera.TargetInDeadZone.Contains(target))
                        {
                            Vector3 position = splitCameraList[0].transform.position + splitCameraList[0].transform.forward;
                            Graphics.DrawMesh(polyMaskList[j], position, Quaternion.identity, stencilDrawerTab[i], 0, splitCameraList[0].GetComponent<Camera>(), 0);
                        }
                    }
                }


                //    for (int i = 0; i < Targets.Length; i++)
                //{
                //    Vector3 position = splitCameraList[0].transform.position + splitCameraList[0].transform.forward;
                //    Graphics.DrawMesh(polyMaskList[i], position, Quaternion.identity, stencilDrawerTab[i], 0, splitCameraList[0].GetComponent<Camera>(), 0);
                //}
            }
            
            //for (int i = splitCameraList.Count-1; i >=0 ;i--)
            //{
            //    splitCameraList[i].UpdateTest();
            //}
        }


        Bounds boundsGizmo;
        public void OnDrawGizmos()
        {
            if (boundsGizmo!=null)
            {
                Gizmos.color = Color.blue;
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

