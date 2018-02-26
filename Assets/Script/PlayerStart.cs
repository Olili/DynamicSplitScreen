using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VoronoiSplitScreen;

public class PlayerStart : MonoBehaviour {

    [Range(1,8)][SerializeField] int nbPlayers;
    [SerializeField] GameObject playerModel;
    GameObject[] playerRef;

	// Use this for initialization
	void Awake () {
        playerRef = new GameObject[nbPlayers];
        // to refacto
        SplitScreenManager splitManager = FindObjectOfType<SplitScreenManager>();
        for (int i = 0; i < nbPlayers;i++)
        {
            Transform playerPos = transform.GetChild(i);
            playerRef[i] = Instantiate(playerModel, playerPos.position,Quaternion.identity);
            playerRef[i].name = "player " + i;
            playerRef[i].GetComponent<SpriteRenderer>().color = SplitScreenManager.debugColor[i];
            playerRef[i].GetComponent<Player>().ID = i;
        }
        for (int i = 0; i < nbPlayers; i++)
        {
            splitManager.AddTarget(playerRef[i].transform);
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
    public void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.49f, 0.313f);
        if (playerRef!=null)
            for (int i = 0; i < playerRef.Length;i++)
                for (int j = 0; j < playerRef.Length; j++)
                    Gizmos.DrawLine(playerRef[i ].transform.position, playerRef[j].transform.position);
    }
}
