using System.Collections.Generic;
using UnityEngine;

namespace FusionGame.Stickman
{
	public class TankBelts : MonoBehaviour
	{
		[SerializeField] private Player _tankBehaviour;

		[SerializeField] private Transform _leftBelt;
		[SerializeField] private Transform _rightBelt;
		private List<Transform> _treadTransforms;
		private Vector3[] _prevPositions;

		private const int _numTreads = 2;
		private Renderer _renderer;
		private MaterialPropertyBlock _matProps;
		private List<float> _materialOffsets;

		private Vector3 _lastForward;


		[SerializeField] private float _trackSpeed = 16f;

		public void Awake()
		{
			_renderer = GetComponent<MeshRenderer>();
			_matProps = new MaterialPropertyBlock();

			_treadTransforms = new List<Transform> {_rightBelt, _leftBelt};
			_materialOffsets = new List<float>(_numTreads);
			_prevPositions = new Vector3[_numTreads];
		}


		public void Start()
		{
			for (int i = 0; i < _numTreads; i++)
			{
				_materialOffsets.Add(0f);
				_prevPositions[i] = _treadTransforms[i].position;
			}

			_lastForward = transform.forward;
		}

		private void UpdateTreadOffset(int treadIndex, float newOffset)
		{
			_materialOffsets[treadIndex] += newOffset;

			_renderer.GetPropertyBlock(_matProps, treadIndex);
			_matProps.SetFloat("_Offset", _materialOffsets[treadIndex]);
			_renderer.SetPropertyBlock(_matProps, treadIndex);
		}
	}
}