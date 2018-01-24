using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
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
        }

        public void ComputeStencilPolyMask()
        {
            Debug.Assert(Targets.Length == 2, "Must have 2 target");
            Vector3 playerToPlayer = targets[1].transform.position - targets[0].transform.position;


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

