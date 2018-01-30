using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    using Voronoi2;
    public class SplitScreenManager : MonoBehaviour
    {
        List<SplitScreenCamera> splitCameraList;
        [SerializeField] GameObject[] targets;
        [SerializeField] Material debugMat;
        [SerializeField] Material stencilDrawer;
        [SerializeField] Material[] stencilDrawerTab;
        Color[] debugColor = new Color[6] { Color.white, Color.black,Color.magenta, Color.red,  Color.cyan,Color.grey};
        List <Mesh> polyMaskList;
        public GameObject[] Targets
        {
            get{return targets;}

            set{targets = value;}
        }
        
        public void ComputeWorldBounds(out Bounds worldBounds, out Vector3[] voronoiSitePos)
        {
            worldBounds = new Bounds();
            voronoiSitePos = new Vector3[targets.Length];
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0; i < targets.Length; i++)
            {
                playerAveragePosition += new Vector2(targets[i].transform.position.x, targets[i].transform.position.y);
            }
            worldBounds.center = playerAveragePosition /= targets.Length;
            for (int i = 0; i < targets.Length; i++)
            {
                voronoiSitePos[i] = (targets[i].transform.position - worldBounds.center);
                voronoiSitePos[i].z = 0;
                Vector2 newExtents = worldBounds.extents;
                if (newExtents.x < Mathf.Abs(voronoiSitePos[i].x))
                {
                    newExtents.x = Mathf.Abs(voronoiSitePos[i].x + 0.1f);
                    newExtents.y = newExtents.x * Screen.height / Screen.width;
                }
                if (newExtents.y < Mathf.Abs(voronoiSitePos[i].y))
                {
                    newExtents.y = Mathf.Abs(voronoiSitePos[i].y + 0.1f);
                    newExtents.x = newExtents.y * Screen.width / Screen.height;
                }
                worldBounds.extents = newExtents;
            }
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

        public void Awake()
        {
            polyMaskList = new List<Mesh>();
            splitCameraList = new List<SplitScreenCamera>();
            stencilDrawerTab = new Material[3];
            stencilDrawerTab[1] = new Material(Shader.Find("Stencils/StencilDrawer"));
            stencilDrawerTab[1].SetFloat("_StencilMask", 0);
            stencilDrawerTab[0] = new Material(Shader.Find("Stencils/StencilDrawer"));
            stencilDrawerTab[0].SetFloat("_StencilMask", 1);


            //for (int i = 0; i < stencilDrawerTab.Length;i++ )
            //{
            //    stencilDrawerTab[i] = new Material(Shader.Find("Stencils/StencilDrawer"));
            //    stencilDrawerTab[i].SetFloat("_StencilMask", Mathf.Pow(2,i));
            //}

        }
        public void Start()
        {
            SplitScreenCamera splitCamera = gameObject.GetComponentInChildren<SplitScreenCamera>();
            if (splitCamera != null)
            {
                splitCameraList.Add(splitCamera);
                splitCamera.Init(targets[0].transform);
            }
        }

        public void Update()
        {
            Bounds worldBounds;
            Vector3[] targetVoronoiPos;
            ComputeWorldBounds(out worldBounds, out targetVoronoiPos);
            ResizeStencilPolygoneMesh(worldBounds, targetVoronoiPos);

            for (int i = 0; i < splitCameraList.Count; i++)
            {
                splitCameraList[i].targetVoronoiScreenPos.x = targetVoronoiPos[i].x / worldBounds.size.x;
                splitCameraList[i].targetVoronoiScreenPos.y = targetVoronoiPos[i].y / worldBounds.size.y;
            }

            for (int i= 0; i < polyMaskList.Count;i++)
            {
                //MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                //propertyBlock.SetColor("color", debugColor[i]);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, debugMat, 0, Camera.main, 0, propertyBlock);
                //MaterialPropertyBlock propertyBlock2 = new MaterialPropertyBlock();
                //propertyBlock2.SetFloat("_StencilMask", i);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, stencilDrawer, 0, Camera.main, 0, propertyBlock2);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, stencilDrawer,0);
                Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, stencilDrawerTab[i],0,Camera.main,0);
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

                Gizmos.DrawSphere(boundsGizmo.center, 0.2f);
            }
        }
    }

    
}

