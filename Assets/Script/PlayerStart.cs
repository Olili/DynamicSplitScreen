using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStart : MonoBehaviour {

    [SerializeField]Color[] playerColors;
    [Range(1,8)][SerializeField] int nbPlayers;
    [SerializeField] GameObject playerModel;
    Player[] playerRef;

	// Use this for initialization
	void Start () {
        playerRef = new Player[nbPlayers];
        for (int i = 0; i < nbPlayers;i++)
        {
            Transform playerPos = transform.GetChild(i);
            playerRef[i] = Instantiate(playerModel, playerPos.position,Quaternion.identity).GetComponent<Player>();
            playerRef[i].GetComponent<SpriteRenderer>().color = playerColors[i];
            playerRef[i].ID = i;
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
