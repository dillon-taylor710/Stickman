﻿using Fusion;
using FusionGame.Stickman;
using FusionHelpers;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static FusionGame.Stickman.CountdownManager;

public class GameLogicManager : MonoBehaviour, ISceneManagerScript
{
    public App Manager;
    
    // WTF SINGLETONS HOW DARE Y-
    #region Singleton
    public static GameLogicManager singleton;
    private void Awake()
    {
        if (singleton != this)
            singleton = this;
    }
    #endregion

    [Header("Game Settings")]
    public int minPlayers = 1;
    public float timeTrialLength = 30.0f;

    [Header("UI Elements")]
    public GameObject winnerText;
    public GameObject WinnerParticle;

    public GameObject Navigation;
    public Animator CountAnim;

    public GameObject objPos;
    public Text txtPos;
    public FusionLauncher.ConnectionStatus _status { get; set; } = FusionLauncher.ConnectionStatus.Disconnected;

    public void OnBackPressed()
    {
        Manager.BackToLevelScene();
    }

    public void StartNavigation()
    {
        StartCoroutine(SetNavigation());
    }

    IEnumerator SetNavigation()
    {
        yield return null;

        Navigation.SetActive(true);
    }

    public void OnNavigationEnd(ORNavigation navigation)
    {
        navigation.gameObject.SetActive(false);

        FusionGame.Stickman.Player player = Manager.runner.GetPlayerObject(Manager.runner.LocalPlayer).GetComponent<FusionGame.Stickman.Player>();

        if (player != null)
        {
            //player.ready = true;
            InputController.readyChanged = true;
        }
    }

    /// <summary>
    /// Ends the current game session.
    /// </summary>
    public void RpcEndGame(Player winner)
    {
        StartCoroutine(Celebrate(winner));
    }

    IEnumerator Celebrate(Player winner)
    {
        FusionGame.Stickman.Player player = Manager.runner.GetPlayerObject(Manager.runner.LocalPlayer).GetComponent<FusionGame.Stickman.Player>();
        if (winner == player) // me
        {
            WinnerParticle.SetActive(true);

            yield return new WaitForSeconds(3f);
            winnerText.SetActive(true);

            yield return new WaitForSeconds(3f);
            //SceneManager.LoadScene(1);

            //NetworkClient.localPlayer.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(3f);
            winner.gameObject.SetActive(false);
        }
    }

    public IEnumerator Countdown(Callback callback)
    {
        CountAnim.enabled = true;

        yield return new WaitForSeconds(4f);

        callback?.Invoke();
    }

    void Update()
    {
        //if (NetworkClient.localPlayer == null)
        //    return;

        int pos = 1;
        //float z = NetworkClient.localPlayer.transform.position.z;

        //foreach (NetworkIdentity player in NetworkClient.spawned.Values)
        //{
        //    if (player != NetworkClient.localPlayer && player.GetComponent<Player>() != null)
        //    {
        //        if (z < player.transform.position.z)
        //            pos++;
        //    }
        //}

        if (pos == 1)
            txtPos.text = pos + "st";
        else if (pos == 2)
            txtPos.text = pos + "nd";
        else if (pos == 3)
            txtPos.text = pos + "rd";
        else
            txtPos.text = pos + "th";
    }

    public void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
    {
        if (status != _status)
        {
            switch (status)
            {
                case FusionLauncher.ConnectionStatus.Disconnected:
                case FusionLauncher.ConnectionStatus.Failed:
                    Manager.BackToLevelScene();
                    break;
                case FusionLauncher.ConnectionStatus.Connecting:
                case FusionLauncher.ConnectionStatus.Connected:
                    break;
            }
        }

        _status = status;
    }

    public void SetConnectionStatus(FusionLauncher.ConnectionStatus status)
    {
        _status = status;
    }
}