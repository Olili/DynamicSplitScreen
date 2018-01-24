﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VoronoiSplitScreen;

public class PlayerStart : MonoBehaviour {

    [SerializeField]Color[] playerColors;
    [Range(1,8)][SerializeField] int nbPlayers;
    [SerializeField] GameObject playerModel;
    GameObject[] playerRef;

	// Use this for initialization
	void Start () {
        playerRef = new GameObject[nbPlayers];
        for (int i = 0; i < nbPlayers;i++)
        {
            Transform playerPos = transform.GetChild(i);
            playerRef[i] = Instantiate(playerModel, playerPos.position,Quaternion.identity);
            playerRef[i].GetComponent<SpriteRenderer>().color = playerColors[i];
            playerRef[i].GetComponent<Player>().ID = i;
        }
        // to refacto
        SplitScreenManager splitManager = FindObjectOfType<SplitScreenManager>();
        splitManager.Targets = playerRef;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
