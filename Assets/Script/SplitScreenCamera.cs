using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    public class SplitScreenCamera : MonoBehaviour
    {
        GameObject target;
        [SerializeField] int id;
        #region getterSetters
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
#endregion


    }

}

