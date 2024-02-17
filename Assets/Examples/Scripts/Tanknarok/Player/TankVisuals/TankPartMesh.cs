using UnityEngine;

namespace FusionGame.Stickman
{
	public class TankPartMesh : MonoBehaviour
	{
		public void SetMaterial(Material material)
		{
			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

			meshRenderer.sharedMaterial = material;
		}
	}
}