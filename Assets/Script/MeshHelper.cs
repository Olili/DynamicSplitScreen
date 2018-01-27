using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voronoi2;
namespace VoronoiSplitScreen
{
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

        /*
        --> Voronoi sort des edges.
        --> j'ajoute les edges manquants pour créer mes polygones. 
        --> Je selectionne les edges définissant un player.
        --> Je cree une liste de vertex à partir de ces edges
        --> J'ordonne la liste de vertex
        --> Je convertis ma liste de vertex en polygone
        --> j'affiche le polygone avec le material requis. 
        */
        public static Mesh GetPolygon(int site, List<GraphEdge> listEdges, Vector3[] sitePosList)
        {
                    // get Vertices around a site
            List<Vector3> sitePolyVerticesList = ComputeSiteVertices(site,listEdges, sitePosList);
                    // sort vertices around Site
            Vector3[] sortedvertices = SortSiteVertices(sitePolyVerticesList);
                    // Convert Vertices in 2d
            Vector2[] sitePolyVertices2D = new Vector2[sortedvertices.Length];
            for (int i = 0; i < sitePolyVertices2D.Length; i++)
                sitePolyVertices2D[i] = (new Vector2(sortedvertices[i].x, sortedvertices[i].y));
                    // build mesh  with Vertices in 2d
            Triangulator tr = new Triangulator(sitePolyVertices2D);
            int[] indices = tr.Triangulate();

            Vector3[] vertices = new Vector3[sitePolyVertices2D.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = (new Vector3((sitePolyVertices2D[i].x - Screen.width / 2) / Screen.width,
                                            (sitePolyVertices2D[i].y - Screen.height / 2) / Screen.height));


            Mesh polygone = new Mesh();
            polygone.vertices = vertices;
            polygone.triangles = indices;
            polygone.RecalculateNormals();
            polygone.RecalculateBounds();
            return polygone;
        }
        static List<Vector3> ComputeSiteVertices(int site, List<GraphEdge> listEdges, Vector3[] sitePosList )
        {
            List<Vector3> borderVertices = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(Screen.width,0 , 0),
                new Vector3(0, Screen.height, 0),
                new Vector3(Screen.width, Screen.height, 0),
            };
            List<Vector3> sitePolyVertices = new List<Vector3>();
                // On regroupe les vertex en fonction de leur sites : 
            AddGraphVerticesToSiteVertices(site, listEdges, sitePolyVertices);
            AddBorderVerticeToSiteVertices(site, borderVertices, sitePosList, sitePolyVertices);

            //Debug.Log("site : " + site);
            //for (int  i = 0;i < sitePolyVertices.Count;i++)
            //{
            //    Debug.Log(sitePolyVertices[i]);
            //}
            return sitePolyVertices;
        }

        static Vector3[] SortSiteVertices(List<Vector3> polyVertices)
        {
            // je pose les vertex déjà liés. 
            // j'ajoute les vertex sur une meme ligne.
            Vector3[] sortedPolyVertices = new Vector3[polyVertices.Count];


            // je commence sur un vertex arbitraire
            // je récupère le plus proche. 
            // Je récupère son plus proche différent des autres et ainsi de suite 
            for (int i = 0; i < polyVertices.Count; i++)
                sortedPolyVertices[i] = polyVertices[i];
            return sortedPolyVertices;
        }

        static void AddGraphVerticesToSiteVertices(int curSite, List<GraphEdge> listEdges,List<Vector3> siteVertices)
        {
            Debug.Assert(siteVertices != null, "list must not be null");
            for (int i = 0; i < listEdges.Count; i++)
            {
                if (listEdges[i].site1 == curSite || listEdges[i].site2 == curSite)
                {
                    Vector3 vertex = new Vector3((float)listEdges[i].x1, (float)listEdges[i].y1, 0);
                    if (!siteVertices.Contains(vertex))
                        siteVertices.Add(vertex);
                    vertex = new Vector3((float)listEdges[i].x2, (float)listEdges[i].y2, 0);
                    if (!siteVertices.Contains(vertex))
                        siteVertices.Add(vertex);
                }
            }
        }
        static void AddBorderVerticeToSiteVertices(int curSite, List<Vector3> borderVertices, Vector3[] sitePosList, List<Vector3> siteVertices)
        {
            Debug.Assert(siteVertices != null, "list must not be null");
            List<Vector3> tempBorderVertices =new List<Vector3>();
            for (int i = 0; i < borderVertices.Count; i++)
            {
                float shortestDistance = float.MaxValue;
                float shortestSiteId = 0;
                for (int j = 0; j < sitePosList.Length;j++)
                {
                    float distance = Vector3.Distance(sitePosList[j], borderVertices[i]);
                    if (distance < shortestDistance)
                    {
                        shortestSiteId = j;
                        shortestDistance = distance;
                    }
                }
                if (shortestSiteId == curSite)
                {
                    //siteVertices.Add(borderVertices[i]);
                    tempBorderVertices.Add(borderVertices[i]);
                }
            }
            int nbGraph = siteVertices.Count;
            int nbBorderAdded = 0;
            for (int i = 0; nbBorderAdded < tempBorderVertices.Count; i = (i + 1) % tempBorderVertices.Count) 
            {
                if ((siteVertices[nbGraph + nbBorderAdded - 1].x == tempBorderVertices[i].x ||
                    siteVertices[nbGraph + nbBorderAdded - 1].y == tempBorderVertices[i].y) &&
                    !siteVertices.Contains(tempBorderVertices[i]))
                {
                    nbBorderAdded++;
                    siteVertices.Add(tempBorderVertices[i]);
                }
            }
        }
        public static void CreatePolygoneFromVertices(Vector3 vertices)
        {
        }
    }

}

