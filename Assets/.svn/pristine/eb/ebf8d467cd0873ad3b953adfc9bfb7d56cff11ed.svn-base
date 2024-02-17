using Fusion;
using FusionGame.Stickman;
using FusionHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReadySceneManager : MonoBehaviour, ISceneManagerScript
{
    public App Manager;
    public GameLogicManager GameManagerObj;
    public FusionLauncher.ConnectionStatus _status { get; set; } = FusionLauncher.ConnectionStatus.Disconnected;

    // Start is called before the first frame update
    void Start()
    {
        Manager.SetSceneManager(this);
        StartCoroutine(OnRace());
    }

    public void OnBackClicked()
    {
        Manager.BackToLevelScene();
    }

    IEnumerator OnRace()
    {
        //GameManagerObj.StartNavigation();
        yield return new WaitForSeconds(4f);

        while (Manager.runner == null)
        {
            yield return null;
        }
        FusionGame.Stickman.GameManager manager = Manager.runner.GetGameManager();
        if (manager != null)
        {
            manager.GoPlayScene();

            /*while (_status != FusionLauncher.ConnectionStatus.Loaded)
            {
                yield return null;
            }*/
            Manager.SetState(SCENE_STATE.SCENE_GAME);
        }
    }

    public void SetConnectionStatus(FusionLauncher.ConnectionStatus status)
    {
        _status = status;
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
}
