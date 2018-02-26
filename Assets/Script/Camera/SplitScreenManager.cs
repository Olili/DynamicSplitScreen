using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    using Voronoi2;

    [System.Serializable]
    public class TargetData
    {
        public Transform target; // transform of a followed target
        [HideInInspector] public Vector2 voronoiPos; // [0,1] viewPort Position of Voronoi.
        [HideInInspector] public Vector2 voronoiViewPort; // [0,1] viewPort Position of Voronoi.
        [HideInInspector] public Vector2 voronoiRegionCenter; // [0,1] viewPort position of the center of a voronoi Region
        [HideInInspector] public Vector2 screenOffset; // unused ? 
        [HideInInspector] public Mesh polyMask; // polygone mask used to stencil out part of the screen.
        [HideInInspector] public SplitScreenCamera camera;
        public TargetData(Transform _target)
        {
            target = _target;
            polyMask = null;
        }
    }

    public class SplitScreenManager : MonoBehaviour
    {
        static SplitScreenManager singleton;
        public List<SplitScreenCamera> splitCameraList;
        [SerializeField] public List<TargetData> targetsData;
        Bounds worldBounds;

        // Not Good : 
        [SerializeField] Material stencilDrawer;
        public static  Material[] stencilDrawerTab;

            // A refacto
        [SerializeField] Material debugMat;
        [SerializeField] SplitScreenCamera splitCameraModel;


        //List <Mesh> polyMaskList;
        //public Vector2[] voronoiRegionPoints; // map [0,1]
        //public Vector2[] voronoiRegionCenter; // map [0,1]
        //public Vector2[] voronoiCameraIndication; // map [0,1]
        //public Bounds testVoronoiBounds;

        public void AddTarget(Transform target)
        {
            TargetData targetData = new TargetData(target);
            targetsData.Add(targetData);
        }
        public void RemoveTarget(Transform target)
        {
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
       
       
            //TODO: Compute without using mesh. 
        public void ComputeVoronoiCenter()
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < targetsData.Count; i++)
            {
                // Compute average
                targetsData[i].polyMask.GetVertices(vertices);
                    // Invert Y coordinate.
                for (int j = 0; j < vertices.Count; j++)
                    vertices[j] = new Vector3(vertices[j].x, vertices[j].y * -1, 0);
                Vector2 voronoiRegionCenter = MeshHelper.Compute2DPolygonCentroid(vertices);
                targetsData[i].voronoiRegionCenter = (voronoiRegionCenter + Vector2.one) * 0.5f;
            }
        }
        // calcule les worlds bounds a partir des Voronoi Position.
        public void ComputeWorldBounds()
        {
            worldBounds = new Bounds();
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0; i < targetsData.Count; i++)
            {
                playerAveragePosition += new Vector2(targetsData[i].target.position.x, targetsData[i].target.position.y);
            }
            worldBounds.center = (playerAveragePosition /= targetsData.Count);

            Vector2 newExtents = Vector2.zero;
            for (int i = 0; i < targetsData.Count; i++)
            {
                targetsData[i].voronoiPos = targetsData[i].target.position - worldBounds.center;
             
                newExtents = worldBounds.extents;
                float aspect = 1.0f * Screen.width / Screen.height;
                if (newExtents.x < Mathf.Abs(targetsData[i].voronoiPos.x))
                {
                    newExtents.x = Mathf.Abs(targetsData[i].voronoiPos.x);
                    newExtents.y = newExtents.x / aspect;
                }
                if (newExtents.y < Mathf.Abs(targetsData[i].voronoiPos.y))
                {
                    newExtents.y = Mathf.Abs(targetsData[i].voronoiPos.y);
                    newExtents.x = newExtents.y * aspect;
                }
                    // Min camera Size for player.
                worldBounds.extents = newExtents;
            }
            worldBounds.extents = newExtents*1.1f;
            for (int i = 0; i < targetsData.Count; i++)
            {
                targetsData[i].voronoiViewPort.x = targetsData[i].voronoiPos.x / worldBounds.extents.x;
                targetsData[i].voronoiViewPort.y = targetsData[i].voronoiPos.y / worldBounds.extents.y;
                targetsData[i].voronoiViewPort = (targetsData[i].voronoiViewPort + Vector2.one) * 0.5f;
            }
        }
        public void ResizeStencilPolygoneMesh()
        {
            Voronoi voronoi = new Voronoi(0.1f);

            double[] x = new double[targetsData.Count];
            double[] y = new double[targetsData.Count];

            boundsGizmo = worldBounds;
            for (int i = 0; i < targetsData.Count; i++)
            {
                x[i] = targetsData[i].voronoiPos.x;
                y[i] = targetsData[i].voronoiPos.y;
            }
            List<GraphEdge> edges = voronoi.generateVoronoi(x, y, -worldBounds.extents.x, worldBounds.extents.x,
                                                                  -worldBounds.extents.y, worldBounds.extents.y);
            if (edges == null || edges.Count <= 0)
            {
                Debug.Log("Error with Voronoi edge List");
                return;
            }
                // Convert TargetPos into Vec3[]
            Vector3[] voronoiPos = new Vector3[targetsData.Count];
            for (int i = 0; i < targetsData.Count; i++)
                voronoiPos[i] = targetsData[i].voronoiPos;
            // Compute Mask 
            for (int i = 0; i < targetsData.Count; i++)
                targetsData[i].polyMask = MeshHelper.GetPolygon(i, edges, voronoiPos, worldBounds);

            ComputeVoronoiCenter();
        }
       
        public void Merge(TargetData targetTwo, SplitScreenCamera firstCam)
        {
            SplitScreenCamera toRemove = GetSplitCamera(targetTwo);
            if (toRemove == splitCameraList[0])
                return;
            Debug.Log(firstCam + " Merging " + targetTwo);
            RemoveCamera(toRemove);
            firstCam.AddTarget(targetTwo);

                // need Reposition Camera ? 
            //Bounds worldBounds;
            //Vector3[] targetVoronoiPos;
            //ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            //UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);
        }
        public void Split(SplitScreenCamera splitOne)
        {
            // on récupère le connard qui s'en va (need to be remade.)
            TargetData farthestTarget = splitOne.GetFarthestTarget();
            Debug.Log(splitOne + " Splitting " + farthestTarget);
            splitOne.RemoveTarget(farthestTarget);

                // Actualise target et position des 2 cameras.
            SplitScreenCamera newCamera = AddCamera(farthestTarget, splitOne.transform.position);

                // need reposition ?
            //Bounds worldBounds;
            //Vector3[] targetVoronoiPos;
            //ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            //UpdateCameraTargetOffset(targetVoronoiPos, worldBounds);

            //newCamera.FollowOnePlayer();
            //splitOne.FollowOnePlayer();
                // FOR DEBUG : 
            //newCamera.UpdateTargets();
            //splitOne.UpdateTargets();

        }
        
        public SplitScreenCamera AddCamera(TargetData primaryTargetData,Vector3 position)
        {
            SplitScreenCamera newCamera = Instantiate(splitCameraModel, position, splitCameraList[0].transform.rotation, transform);
            splitCameraList.Add(newCamera);
            newCamera.Init(primaryTargetData, splitCameraList.Count-1);
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

        // replace by predicate ? 
        public TargetData GetTargetData(Transform target)
        {
            for (int i = 0; i < targetsData.Count; i++)
                if (targetsData[i].target == target)
                    return targetsData[i];
            Debug.LogError("target doesn't exist");
            return null;
        }
        // Shoundn't by needed ( reorganise)
        public SplitScreenCamera GetSplitCamera(TargetData targetData)
        {
            for (int i = 0; i < splitCameraList.Count; i++)
                if (targetData.camera!=null && splitCameraList[i] == targetData.camera)
                        return splitCameraList[i];
            return null;
        }
        
        public void DrawPolyMask()
        {
            if (splitCameraList.Count < 2)
                return;
            //for (int i = 0; i < splitCameraList.Count; i++)
            //    splitCameraList[i].DrawPolyMask(splitCameraList[0].GetComponent<Camera>(), stencilDrawerTab);
            //for (int i = 0; i < splitCameraList.Count; i++)
            //    splitCameraList[i].DrawPolyMask(splitCameraList[0].GetComponent<Camera>(), stencilDrawerTab[splitCameraList[i].ID]);
            for (int i = 0; i < splitCameraList.Count; i++)
                for (int j = 0; j < targetsData.Count; j++)
                {
                    TargetData targetData = targetsData[j];
                    Vector3 position = splitCameraList[i].transform.position + splitCameraList[i].transform.forward;
                    Graphics.DrawMesh(targetData.polyMask, position, Quaternion.identity, stencilDrawerTab[targetData.camera.ID], 0, splitCameraList[i].GetComponent<Camera>());
                    //Material debugMat = new Material(Shader.Find("Sprites/Default"));
                    //debugMat.color = debugColor[targetData.camera.ID];
                    //Graphics.DrawMesh(targetData.polyMask, position, Quaternion.identity, debugMat, 0, splitCameraList[i].GetComponent<Camera>());
                    //splitCameraList[i].DrawPolyMask(splitCameraList[j].GetComponent<Camera>(), stencilDrawerTab[splitCameraList[i].ID]);
                }
        }

        public void Awake()
        {
            singleton = this;
            targetsData = new List<TargetData>();
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
                splitCamera.Init(targetsData[i], i);
                splitCameraList.Add(splitCamera);
                //for (int j = 0; j < targetsData.Count;j++)
                targetsData[i].camera = splitCamera;
            }
        }
        
        
        public void Update()
        {
            ComputeWorldBounds();

            ResizeStencilPolygoneMesh();

            DrawPolyMask();
        }

#if UNITY_EDITOR
        public bool showVoronoiCenter;
        public static Color[] debugColor = new Color[7]
        { Color.magenta,Color.cyan,Color.yellow, Color.white, Color.red, Color.black,  Color.grey };
        [HideInInspector]public Bounds boundsGizmo;
        public bool showVoronoiBounds;
#endif
    }
}

