using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionExamples.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FusionGame.Stickman
{
	/// <summary>
	/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
	/// that input struct in the Fusion Simulation loop.
	/// </summary>
	public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
	{
		[SerializeField] private LayerMask _mouseRayMask;

		public static bool fetchInput = false;
		public static bool readyChanged = false;

		private Player _player;
		private NetworkInputData _inputData = new NetworkInputData(0, Vector2.zero, -1, 0);
		private Vector2 _moveDelta;
		private Vector2 _aimDelta;
		private Vector2 _leftPos;
		private Vector2 _leftDown;
		private Vector2 _rightPos;
		private Vector2 _rightDown;
		private bool _leftTouchWasDown;
		private bool _rightTouchWasDown;

		private uint _buttonReset;
		private uint _buttonSample;

        [SerializeField] private InputAction jump_action;
        private bool _jumpPressed;

        private void JumpPressed(InputAction.CallbackContext ctx) => _jumpPressed = true;
        private static bool ReadBool(InputAction action) => action.ReadValue<float>() != 0;

        /// <summary>
        /// Hook up to the Fusion callbacks so we can handle the input polling
        /// </summary>
        public override void Spawned()
		{
			_player = GetComponent<Player>();
            jump_action = jump_action.Clone();
            jump_action.Enable();
            jump_action.started += JumpPressed;

            // Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
            // but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
            if (Object.HasInputAuthority)
			{
				Runner.AddCallbacks(this);
			}

			Debug.LogError("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " IsInputSrc=" + Object.HasInputAuthority + " IsStateSrc=" + Object.HasStateAuthority);
		}
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            DisposeInputs();

            if (Object.HasInputAuthority)
                Runner.RemoveCallbacks(this);
        }

        private void OnDestroy()
        {
            DisposeInputs();
        }

        private void DisposeInputs()
        {
            jump_action.Dispose();
            //drift.started -= DriftPressed;
        }

        /// <summary>
        /// Get Unity input and store them in a struct for Fusion
        /// </summary>
        /// <param name="runner">The current NetworkRunner</param>
        /// <param name="input">The target input handler that we'll pass our data to</param>
        public void OnInput(NetworkRunner runner, NetworkInput input)
		{
			if (_player!=null && _player.Object!=null && _player.stage == Player.Stage.Active)
			{
				_inputData.moveDirection = _moveDelta.normalized;
				_inputData.Buttons = _buttonSample;

                if (readyChanged)
                {
                    _inputData.Buttons |= NetworkInputData.BUTTON_TOGGLE_READY;
                    readyChanged = false;
                }
				if (_jumpPressed)
				{
					_inputData.Buttons |= NetworkInputData.BUTTON_JUMP;
                    _jumpPressed = false;
                }
                //if (ReadBool(jump_action)) _inputData.Buttons |= NetworkInputData.BUTTON_JUMP;// For continuous jumping
                _buttonReset |= _buttonSample; // This effectively delays the reset of the read button flags until next Update() in case we're ticking faster than we're rendering

                _player.skin_id = PlayerPrefs.GetInt("Char_SKIN_ID", 0);
				_player.prev_skin = _player.skin_id;
                _inputData.Skin = _player.skin_id;
            }

			if (_player.playerCamera != null)
				_inputData.cameraRotation = _player.playerCamera.transform.eulerAngles.y;

            // Hand over the data to Fusion
            input.Set(_inputData);
			_inputData.Buttons = 0;
        }
		
		private void Update()
		{
			_buttonSample &= ~_buttonReset;

            if (Input.mousePresent)
			{
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                float verticalInput = Input.GetAxisRaw("Vertical");

                _moveDelta = new Vector2(horizontalInput, verticalInput);
            }
        }

		public void ToggleReady()
		{
			_buttonSample |= NetworkInputData.BUTTON_TOGGLE_READY;
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
		
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}

		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}

	public struct NetworkInputData : INetworkInput
	{
		public const uint BUTTON_JUMP = 1 << 0;
		public const uint BUTTON_TOGGLE_READY = 1 << 1;

		public uint Buttons;
		public Vector2 moveDirection;
		public int Skin;
		public float cameraRotation;

		public NetworkInputData(uint b, Vector2 m, int s, float c)
		{
			Buttons = b;
			moveDirection = m;
			Skin = s;
			cameraRotation = c;
		}

		public bool IsUp(uint button)
		{
			return IsDown(button) == false;
		}

		public bool IsDown(uint button)
		{
			return (Buttons & button) == button;
		}

		public bool WasPressed(uint button, NetworkInputData oldInput)
		{
			return (oldInput.Buttons & button) == 0 && (Buttons & button) == button;
		}
        public bool IsJumpPressed => IsDown(BUTTON_JUMP);
    }
}