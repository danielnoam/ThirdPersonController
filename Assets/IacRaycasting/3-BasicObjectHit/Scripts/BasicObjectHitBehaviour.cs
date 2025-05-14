using UnityEngine;

namespace IacRaycasting.BasicObjectHit
{
	[RequireComponent(typeof(Camera))]
	public class BasicObjectHitBehaviour : MonoBehaviour
	{
		[SerializeField, Range(1, 100)] private float _rayLength;
		
		private Camera _camera;
		
		private void Start()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
			var ray = new Ray(_camera.transform.position, _camera.transform.forward);
			if (Physics.Raycast(ray, out var hit, _rayLength))
			{
				Debug.Log($"Hit!!! {hit.transform.gameObject.name}");
			}
			Debug.DrawRay(ray.origin, ray.direction * _rayLength, Color.red);
		}
	}
}