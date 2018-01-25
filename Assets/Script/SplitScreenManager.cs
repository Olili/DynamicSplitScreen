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
        Mesh[] polyMask;
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
        public void Start()
        {
            polyMask = new Mesh[2];
            //polyMask[0] = Mesh.C
            Voronoi voronoi = new Voronoi(0.1f);
            double[] x = new double[] { 1, Screen.width, Screen.width/2 };
            double[] y = new double[] { 1, Screen.height, Screen.height / 2 };
            List<GraphEdge> edges = voronoi.generateVoronoi(x, y, 0, Screen.width, 0, Screen.height);

            for (int i = 0; i < edges.Count;i++)
            {
                Debug.Log(edges[i]);
            }
        }

        public void CreateQuadsFromEdges(List<GraphEdge> edges)
        {

        }
        public void AddBorderEdges(List<GraphEdge> edges)
        {
            GraphEdge up = new GraphEdge { x1 = 0, x2 = Screen.width, y1 = 0, y2 = 0 };
            GraphEdge down = new GraphEdge { x1 = 0, x2 = Screen.width, y1 = Screen.height, y2 = Screen.height };
            GraphEdge left = new GraphEdge { x1 = 0, x2 = 0, y1 = 0, y2 = Screen.height };
            GraphEdge right = new GraphEdge { x1 = Screen.width, x2 = Screen.width, y1 = 0, y2 = Screen.height };

            // cut + give neighbour


        }

        public void ComputeStencilPolyMask()
        {
            Debug.Assert(Targets.Length == 2, "Must have 2 target");
            Vector3 playerToPlayer = targets[1].transform.position - targets[0].transform.position;

            Vector3 perpPlayerToPlayer = new Vector3(-playerToPlayer.y, playerToPlayer.x, playerToPlayer.z);
            float angle = Vector3.Angle(perpPlayerToPlayer, Vector3.up);
            float cutScreenLength = Screen.height / Mathf.Cos(angle);

            //Vector3 centerBeetweenPlayer = 
        }
        
        public void Update()
        {
            ComputeStencilPolyMask();
        }
    }

    static class MeshHelper
    {
        public static Mesh GetQuad()
        {
            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f,  0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f)
            };

            var indices = new[] { 0, 1, 2, 1, 0, 3 };

            Mesh quad = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = indices
            };
            quad.RecalculateNormals();
            quad.RecalculateBounds();
            return quad;
        }
    }
}

