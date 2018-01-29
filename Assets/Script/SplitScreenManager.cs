using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    using Voronoi2;
    public class SplitScreenManager : MonoBehaviour
    {
        [SerializeField] GameObject[] targets;
        [SerializeField] Camera stencilCamera;
        [SerializeField] Material debugMat;
        Color[] debugColor = new Color[6] { Color.magenta, Color.red, Color.white, Color.black, Color.cyan,Color.grey};
        List <Mesh> polyMaskList;
        public GameObject[] Targets
        {
            get
            {
                return targets;
            }

            set
            {
                targets = value;
            }
        }
        public void Awake()
        {
            polyMaskList = new List<Mesh>();
        }
        public void Start()
        {
        }
        public void ComputeWorldBounds(out Bounds worldBounds, out Vector3[] voronoiSitePos)
        {
            worldBounds = new Bounds();
            voronoiSitePos = new Vector3[targets.Length];
            Vector2 playerAveragePosition = Vector2.zero;
            for (int i = 0;i < targets.Length;i++)
            {
                playerAveragePosition += new Vector2(targets[i].transform.position.x, targets[i].transform.position.y);
            }
            worldBounds.center = playerAveragePosition /= targets.Length;
            for (int i = 0; i < targets.Length; i++)
            {
                voronoiSitePos[i] = (targets[i].transform.position - worldBounds.center);
                voronoiSitePos[i].z = 0;
                worldBounds.Encapsulate(voronoiSitePos[i]*1.01f);
            }
        }
        public Vector3[] GetSquizedPlayerPosOnScreen()
        {
          
            Vector3[] playerResizePos = new Vector3[targets.Length];
            
            for (int i = 0; i < playerResizePos.Length; i++)
            {
                playerResizePos[i] = new Vector3(targets[i].transform.position.x, targets[i].transform.position.y, 0);
                playerResizePos[i] = Camera.main.WorldToScreenPoint(playerResizePos[i]);
                if (playerResizePos[i].x > Screen.width)
                    playerResizePos[i].x = Screen.width-1;
                if (playerResizePos[i].y > Screen.height)
                    playerResizePos[i].y = Screen.height-1;
                if (playerResizePos[i].x < 0)
                    playerResizePos[i].x = 0;
                if (playerResizePos[i].y < 0)
                    playerResizePos[i].y = 0;
            }

            return playerResizePos;
        }
        public void ResizeStencilPolygoneMesh()
        {
            Voronoi voronoi = new Voronoi(0.1f);
            Bounds worldBounds ;

            double[] x = new double[targets.Length];
            double[] y = new double[targets.Length];

            Vector3[] specialSitePos;// = GetSquizedPlayerPosOnScreen();

            ComputeWorldBounds(out worldBounds, out specialSitePos);

            for (int i = 0; i < specialSitePos.Length; i++)
            {
                //Debug.Assert(specialSitePos[i].x < Screen.width && specialSitePos[i].y < Screen.height,"Error with playerPos");
                x[i] = specialSitePos[i].x;
                y[i] = specialSitePos[i].y;
            }
            List<GraphEdge> edges = voronoi.generateVoronoi(x, y, -worldBounds.extents.x, worldBounds.extents.x,
                                                                  -worldBounds.extents.y, worldBounds.extents.y);
            if (edges == null || edges.Count <= 0)
            {
                Debug.Log("Stuff");
            }
            Debug.Assert(edges != null && edges.Count > 0, "Error with Voronoi edge List");
            polyMaskList.Clear();
            for (int i = 0; i < specialSitePos.Length; i++)
                polyMaskList.Add(MeshHelper.GetPolygon(i, edges, specialSitePos, worldBounds));

        }
       

        //public void ComputeStencilPolyMask()
        //{
        //    Debug.Assert(Targets.Length == 2, "Must have 2 target");
        //    Vector3 playerToPlayer = targets[1].transform.position - targets[0].transform.position;

        //    Vector3 perpPlayerToPlayer = new Vector3(-playerToPlayer.y, playerToPlayer.x, playerToPlayer.z);
        //    float angle = Vector3.Angle(perpPlayerToPlayer, Vector3.up);
        //    float cutScreenLength = Screen.height / Mathf.Cos(angle);

        //    //Vector3 centerBeetweenPlayer = 
        //}
        
        public void Update()
        {
            ResizeStencilPolygoneMesh();
            for (int i= 0; i < polyMaskList.Count;i++)
            {
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, debugMat, 0);
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor("color", debugColor[i]);
                Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, debugMat, 0, Camera.main, 0, propertyBlock);
                //Graphics.DrawMesh(polyMaskList[i], Vector3.zero, Quaternion.identity, debugMat, 0);


            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
            }
        }
    }

    
}

