using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Analytics;

public class Player : NetworkBehaviour
{
    public float health = 100f;
    public Player opponent;
    private Player playerSelf;

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            playerSelf = this;
            FindEnemy();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Damage()
    {
        Debug.Log("damaged.");
    }

    private void FindEnemy()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();

        foreach (Player player in allPlayers)
        {
            if (player != playerSelf) 
            {
                opponent = player;
                break; 
            }
        }
    }
}
