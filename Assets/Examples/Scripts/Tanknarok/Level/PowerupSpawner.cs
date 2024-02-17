using Fusion;
using FusionHelpers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FusionGame.Stickman
{
	public class PowerupSpawner : NetworkBehaviourWithState<PowerupSpawner.NetworkState> 
	{
		[Networked] public override ref NetworkState State => ref MakeRef<NetworkState>();
		public struct NetworkState : INetworkStruct
		{
			public TickTimer respawnTimer;
			public int activePowerupIndex;
		}

		[SerializeField] private GameObject _game_obj;
        [SerializeField] private ParticleSystem _touch_particle;
        [SerializeField] private MeshFilter _meshFilter;

		const float RESPAWN_TIME = 12f;
		public PowerupType _powerupType;

		private AudioEmitter _audio;

		public override void Spawned()
		{
			_audio = GetComponent<AudioEmitter>();
			if (Object.HasStateAuthority)
				SetNextPowerup();
			InitPowerupVisuals();
		}

		public override void Render()
		{
			if ( TryGetStateChanges(out var old, out var current) )
			{
				if(old.respawnTimer.TargetTick>0) // Avoid triggering sound effect on initial init
					_audio.PlayOneShot(PowerupManager.GetPowerup(old.activePowerupIndex).pickupSnd);
				InitPowerupVisuals();
			}

			float progress = 0;
			if (!State.respawnTimer.Expired(Runner))
				progress = 1.0f - (State.respawnTimer.RemainingTime(Runner) ?? 0) / RESPAWN_TIME;
			else
				_game_obj.transform.localScale = Vector3.Lerp(_game_obj.transform.localScale, Vector3.one, Time.deltaTime * 5f);
		}

		private void OnTriggerStay(Collider collisionInfo)
		{
			if (!State.respawnTimer.Expired(Runner))
				return;

			Player player = collisionInfo.gameObject.GetComponent<Player>();
			if (!player)
				return;

			int powerup = State.activePowerupIndex;
			player.RaiseEvent( new TickAlignedEventRelay.PickupEvent { powerup = powerup} );
			_touch_particle.Play();

            if (Object.HasStateAuthority)
                SetNextPowerup();
		}

		private void InitPowerupVisuals()
		{
			PowerupElement powerup = PowerupManager.GetPowerup(State.activePowerupIndex);
			_game_obj.transform.localScale = Vector3.zero;
			_meshFilter.mesh = powerup.powerupSpawnerMesh;
		}

		private void SetNextPowerup()
		{
			State.respawnTimer = TickTimer.CreateFromSeconds(Runner, RESPAWN_TIME);
			State.activePowerupIndex = PowerupManager.GetPowerupIndex(_powerupType);
        }
	}
}