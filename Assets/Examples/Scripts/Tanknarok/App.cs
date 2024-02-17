using Fusion;
using FusionExamples.UIHelpers;
using FusionHelpers;
using Tanknarok.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum SCENE_STATE
{
    SCENE_WAITING,
    SCENE_READY,
    SCENE_GAME
}

namespace FusionGame.Stickman
{
	/// <summary>
	/// App entry point and main UI flow management.
	/// </summary>
	public class App : MonoBehaviour
	{
		[SerializeField] private LevelManager _levelManager;
		[SerializeField] private GameManager _gameManagerPrefab;
        public GameObject WaitingObj;
        public GameObject ReadyObj;
        public GameObject GameObj;

        public SCENE_STATE state;


        private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Disconnected;
		private GameMode _gameMode;
		private int _nextPlayerIndex;

		public NetworkRunner runner = null;
		ISceneManagerScript scene_manager = null;
        public static App Instance;

        private void Awake()
		{
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                if (Instance.gameObject != null)
                {
                    Object.Destroy(Instance.gameObject);
                }
                Instance = this;
            }
            DontDestroyOnLoad(this);
			_levelManager.onStatusUpdate = OnConnectionStatusUpdate;
		}

		private void Start()
		{
			OnConnectionStatusUpdate( null, FusionLauncher.ConnectionStatus.Disconnected, "");
            SetState(SCENE_STATE.SCENE_WAITING);
        }

        private void OnApplicationQuit()
        {
            if (runner != null && runner.IsShutdown) { runner.Shutdown(true);  }
        }

        private void Update()
		{
			if (state == SCENE_STATE.SCENE_WAITING)
			{
				if (Input.GetKeyUp(KeyCode.Escape))
				{
					BackToLevelScene();
				}
			}
		}

		// What mode to play - Called from the start menu
		public void OnHostOptions()
		{
			SetGameMode(GameMode.Host);
		}

		public void OnJoinOptions()
		{
			SetGameMode(GameMode.Client);
		}

		public void OnSharedOptions()
		{
			SetGameMode(GameMode.Shared);
		}

		private void SetGameMode(GameMode gamemode)
		{
			_gameMode = gamemode;
		}

		public void OnEnterRoom()
		{
			FusionLauncher.Launch(_gameMode, Constants.room_name, _gameManagerPrefab, _levelManager, OnConnectionStatusUpdate);
		}

		/// <summary>
		/// Call this method from button events to close the current UI panel and check the return value to decide
		/// if it's ok to proceed with handling the button events. Prevents double-actions and makes sure UI panels are closed. 
		/// </summary>
		/// <param name="ui">Currently visible UI that should be closed</param>
		/// <returns>True if UI is in fact visible and action should proceed</returns>
		private bool GateUI(Panel ui)
		{
			if (!ui.isShowing)
				return false;
			ui.SetVisible(false);
			return true;
		}

		private void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
		{
			if (!this)
				return;

			this.runner = runner;

            if (scene_manager != null)
            {
                scene_manager.OnConnectionStatusUpdate(runner, status, reason);
            }

            if (status != _status)
			{
                _status = status;

                switch (status)
				{
					case FusionLauncher.ConnectionStatus.Disconnected:
					case FusionLauncher.ConnectionStatus.Failed:
						BackToLevelScene();
						break;
				}
			}
		}


		public void BackToLevelScene()
		{
            if (runner != null)
				runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && !runner.IsShutdown)
            {
                // Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
                runner.Shutdown(false);
            }

            SceneManager.LoadScene("LevelScene");
		}

		public void SetSceneManager(ISceneManagerScript manager)
		{
			scene_manager = manager;
			scene_manager.SetConnectionStatus(_status);
		}

        public void SetState(SCENE_STATE st)
        {
            state = st;

            switch (state)
            {
                case SCENE_STATE.SCENE_WAITING:
                    WaitingObj.SetActive(true);
                    ReadyObj.SetActive(false);
                    //GameObj.SetActive(false);
                    break;
                case SCENE_STATE.SCENE_READY:
                    WaitingObj.SetActive(false);
                    ReadyObj.SetActive(true);
                    //GameObj.SetActive(false);
                    break;
                case SCENE_STATE.SCENE_GAME:
                    WaitingObj.SetActive(false);
                    ReadyObj.SetActive(false);
					//GameObj.SetActive(true);

					GameLogicManager game_manager = FindObjectOfType<GameLogicManager>();

					if (game_manager != null)
					{
						game_manager.Manager = this;
						scene_manager = game_manager;

						game_manager.SetConnectionStatus(_status);
						game_manager.StartNavigation();
					}

                    break;
            }
        }
    }
}