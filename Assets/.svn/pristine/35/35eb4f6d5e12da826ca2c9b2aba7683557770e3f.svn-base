using FusionGame.Stickman;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GoalBehaviour : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            other.GetComponent<Player>().SetVictory(true);
            GameLogicManager.singleton.RpcEndGame(other.GetComponent<Player>());
        }
    }

}