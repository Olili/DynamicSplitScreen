using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoronoiSplitScreen;

public class Player : MonoBehaviour {

    // Use this for initialization
    int id;
    [SerializeField]float speed;
    static int activePlayer = 0;

    public int ID
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
        }
    }
	
	void Update () {

        if (activePlayer == ID)
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            Vector3 velocity = new Vector3(x, y, 0).normalized * speed;
            transform.position += velocity * Time.deltaTime;
            if (velocity !=Vector3.zero)
            {
                float nextAngle = Vector3.SignedAngle(Vector3.up, velocity, transform.forward);
                //transform.eulerAngles = new Vector3(0, 0, Mathf.Lerp(transform.eulerAngles.z, nextAngle,0.1f)); 
                transform.eulerAngles = new Vector3(0, 0, nextAngle);
            }
        }
        if (Input.GetKey(KeyCode.Keypad0 + ID))
        {
            activePlayer = ID;
        }
		
	}
}
