using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace FusionGame.Stickman
{
	public class PowerupManager : MonoBehaviour
	{
		public enum PowerupInstallationType
		{
			PRIMARY,
			SECONDARY,
			BUFF
		};

		private List<PowerupElement> _powerups = new List<PowerupElement>();
		[SerializeField] private Player _player;

		public void InstallPowerup(PowerupElement powerup)
		{
			int weaponIndex = GetWeaponIndex(powerup.powerupType);
			
			if (weaponIndex == -1)
			{
				_powerups.Add(powerup);
			}
		}

		public bool IsInstalled(PowerupType powerupType)
		{
            int weaponIndex = GetWeaponIndex(powerupType);

			if (weaponIndex == -1)
				return false;
			else
				return true;
        }

		private int GetWeaponIndex(PowerupType powerupType)
		{
			for (int i = 0; i < _powerups.Count; i++)
			{
				if (_powerups[i].powerupType == powerupType)
					return i;
			}

			return -1;
		}

        private static List<PowerupElement> _powerupElements;
        public static PowerupElement GetPowerup(int powerupIndex)
        {
            LoadPowerup();

            if (powerupIndex >= 0 && powerupIndex < _powerupElements.Count)
				return _powerupElements[powerupIndex];
			else
				return null;
        }
        public static int GetPowerupIndex(PowerupType powerupType)
        {
            LoadPowerup();

            for (int i = 0; i < _powerupElements.Count; i++)
			{
				if (_powerupElements[i].powerupType == powerupType)
					return i;
			}

			return -1;
        }

		private static void LoadPowerup()
		{
            if (_powerupElements == null)
            {
                _powerupElements = new List<PowerupElement>();
                _powerupElements.Add(Resources.Load<PowerupElement>("PowerupElements/PE_speedup"));
            }
        }
    }
}