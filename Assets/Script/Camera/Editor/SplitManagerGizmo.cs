using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoronoiSplitScreen
{
    public static class SplitManagerGizmo  {

        [DrawGizmo(GizmoType.Active | GizmoType.Selected)]
        static void DrawNormalGizmos(SplitScreenManager splitScreenManager, GizmoType drawnGizmoType)
        {
            if (splitScreenManager.showVoronoiCenter)
            {
                for (int i =0; i < splitScreenManager.targetsData.Count;i++)
                {
                    Vector3 center = splitScreenManager.targetsData[i].voronoiRegionCenter;
                    center = splitScreenManager.targetsData[i].camera.GetComponent<Camera>().ViewportToWorldPoint(center);
                    center += Vector3.forward * 5;
                    Gizmos.color = SplitScreenManager.debugColor[i];
                    Gizmos.DrawSphere(center, 0.15f);
                }
            }
            if (splitScreenManager.showVoronoiBounds)
            {
                Bounds boundsGizmo = splitScreenManager.boundsGizmo;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(boundsGizmo.size.x, 0, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(boundsGizmo.size.x, 0, 0));

                Gizmos.DrawLine(boundsGizmo.min, boundsGizmo.min + new Vector3(0, boundsGizmo.size.y, 0));
                Gizmos.DrawLine(boundsGizmo.max, boundsGizmo.max - new Vector3(0, boundsGizmo.size.y, 0));

                //Gizmos.color = Color.magenta;
                //Gizmos.DrawSphere(boundsGizmo.center, 0.2f);
            }
        }
    }

}
