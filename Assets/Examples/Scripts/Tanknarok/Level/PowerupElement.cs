using UnityEngine;

namespace FusionGame.Stickman
{
	public enum PowerupType
	{
        DEFAULT = 0,
        SPEED_UP = 1,
        EMPTY = 5
	}
	
	[CreateAssetMenu(fileName = "PE_", menuName = "ScriptableObjects/PowerupElement")]
	public class PowerupElement : ScriptableObject
	{
		public PowerupManager.PowerupInstallationType powerupInstallationType;
		public PowerupType powerupType;
		public Mesh powerupSpawnerMesh;
		public AudioClipData pickupSnd;
		public float value;
	}
}