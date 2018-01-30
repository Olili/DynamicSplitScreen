using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voronoi2;
using System;
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
        public static Mesh GetPolygon(int site, List<GraphEdge> listEdges, Vector3[] sitePosList, Bounds worldBounds)
        {
            Vector3[] sortedvertices = GetPolyVertices(site, sitePosList, listEdges, worldBounds);

            // Convert Vertices in 2d
            Vector2[] sitePolyVertices2D = new Vector2[sortedvertices.Length];
            for (int i = 0; i < sitePolyVertices2D.Length; i++)
                sitePolyVertices2D[i] = (new Vector2(sortedvertices[i].x, sortedvertices[i].y));
            // build mesh  with Vertices in 2d
            Triangulator tr = new Triangulator(sitePolyVertices2D);
            int[] indices = tr.Triangulate();

            Vector3[] vertices = new Vector3[sitePolyVertices2D.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = (new Vector3((sitePolyVertices2D[i].x) / (worldBounds.extents.x),
                                                (sitePolyVertices2D[i].y * -1) / (worldBounds.extents.y)));

            Mesh polygone = new Mesh();
            polygone.vertices = vertices;
            polygone.triangles = indices;
            polygone.RecalculateNormals();
            polygone.RecalculateBounds();
            return polygone;
        }

        /*
            Refacto organisation mesh :
            --> 1er arrivée est l'arrivée du premier edge.
            -->E0 je chope un EndPoint. Je cherche si EndPoint est lié à un edge. 
                -->E01 Si oui je chop cette edge puis retour E0 avec l'autre point de l'edge.
                -->E02 Si non je cherche parmis les bords qui lui appartiennt un bord qui est lié à ce truc.
            --> stop lorsque je retourne a la première arrivée.
        */
        static Vector3[] GetPolyVertices(int curSite, Vector3[] sitePosList, List<GraphEdge> listEdges,Bounds bounds)
        {
            // creation des borderVertices
            List<Vector3> borderVertices = new List<Vector3>
            {
                new Vector3(-bounds.extents.x, -bounds.extents.y, 0),
                new Vector3(-bounds.extents.x,bounds.extents.y , 0),
                new Vector3(bounds.extents.x, -bounds.extents.y, 0),
                new Vector3(bounds.extents.x, bounds.extents.y, 0),
            };

            // récupération des border vertex appartenant à la zone. 
            List<Vector3> tempBorderVertices = new List<Vector3>();
            for (int i = 0; i < borderVertices.Count; i++)
            {
                float shortestDistance = float.MaxValue;
                float shortestSiteId = 0;
                for (int j = 0; j < sitePosList.Length; j++)
                {
                    float distance = Vector3.Distance(sitePosList[j], borderVertices[i]);
                    if (distance < shortestDistance)
                    {
                        shortestSiteId = j;
                        shortestDistance = distance;
                    }
                }
                if (shortestSiteId == curSite)
                    tempBorderVertices.Add(borderVertices[i]);
            }
            // récupération des edges appartenant au site
            List<GraphEdge> templistEdges = new List<GraphEdge>();
            for (int i = 0; i < listEdges.Count; i++)
            {
                if (listEdges[i].x1 == listEdges[i].x2 && listEdges[i].y1 == listEdges[i].y2)
                    continue;
                if ((listEdges[i].site1 == curSite || listEdges[i].site2 == curSite) &&
                    templistEdges.Find(edge => edge.x1 == listEdges[i].x1 && edge.x2 == listEdges[i].x2
                                        && edge.y1 == listEdges[i].y1 && edge.y2 == listEdges[i].y2) == null)
                    templistEdges.Add(listEdges[i]);
            }
            Debug.Assert(templistEdges.Count > 0, "Error with neighbour Voronoi edge");
            // organisation des vertices dans l'ordre
            List<Vector3> sortedvertices = new List<Vector3>();
            Vector3 firstPoint = new Vector3((float)templistEdges[0].x1, (float)templistEdges[0].y1, 0);
            Vector3 curPoint = firstPoint;
            sortedvertices.Add(curPoint);
            int curID = 1;
            do
            {

               
                           

                bool nextPointFound = false;
                for (int j = 0; j < templistEdges.Count; j++)
                {
                    Vector3 edgeVertex1 = new Vector3((float)templistEdges[j].x1, (float)templistEdges[j].y1, 0);
                    Vector3 edgeVertex2 = new Vector3((float)templistEdges[j].x2, (float)templistEdges[j].y2, 0);
                    if (nextPointFound = (edgeVertex1 == curPoint && !sortedvertices.Contains(edgeVertex2)) &&
                           (sortedvertices.Find(edge => edge == edgeVertex2) == Vector3.zero))
                        curPoint = edgeVertex2;
                    else if (nextPointFound = (edgeVertex2 == curPoint && !sortedvertices.Contains(edgeVertex1)) &&
                           (sortedvertices.Find(edge => edge == edgeVertex1) == Vector3.zero))
                        curPoint = edgeVertex1;
                    if (nextPointFound)
                        break;
                }

                if (!nextPointFound)
                {
                    for (int j = 0; j < tempBorderVertices.Count; j++)
                    {
                        if (Mathf.Approximately(curPoint.x, tempBorderVertices[j].x) ||
                            Mathf.Approximately(curPoint.y, tempBorderVertices[j].y))
                        {
                            nextPointFound = true;
                            curPoint = tempBorderVertices[j];
                            tempBorderVertices.Remove(tempBorderVertices[j]);
                            break;
                        }
                    }
                }
                if (!nextPointFound)
                {
                    for (int j = 0; j < templistEdges.Count; j++)
                    {
                        Vector3 edgeVertex1 = new Vector3((float)templistEdges[j].x1, (float)templistEdges[j].y1, 0);
                        Vector3 edgeVertex2 = new Vector3((float)templistEdges[j].x2, (float)templistEdges[j].y2, 0);

                        if (nextPointFound = ((edgeVertex1.x == curPoint.x || edgeVertex1.y == curPoint.y) && !sortedvertices.Contains(edgeVertex1)))
                            curPoint = edgeVertex1;
                        else if (nextPointFound = ((edgeVertex2.x == curPoint.x || edgeVertex2.y == curPoint.y) && !sortedvertices.Contains(edgeVertex2)))
                            curPoint = edgeVertex2;
                        if (nextPointFound)
                            break;
                    }
                }


                    if (!nextPointFound)
                {
                    break;
                }
                else
                {
                    if (curPoint != firstPoint)
                    {
                        sortedvertices.Add(curPoint);
                        curID++;
                        if (curID > 10)
                        {
                            Debug.LogError("To many curSite : " + curSite);
                            break;
                        }
                    }
                }
            }
            while (curPoint != firstPoint);

            Vector3[] sortedVerticeTab = new Vector3[sortedvertices.Count];
            for (int i = 0; i < sortedvertices.Count; i++)
            {
                sortedVerticeTab[i] = sortedvertices[i];
            }
            return sortedVerticeTab;
        }

    }
}

