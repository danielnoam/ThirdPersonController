using UnityEngine;

namespace IacRaycasting.Enums.Scripts
{
	public class HighlightAndCollectBehavior : MonoBehaviour
	{
		[SerializeField, Range(1, 100)] private float _rayLength = 10;
		[SerializeField] private bool _debugRay = true;

		private Camera _camera;
		private Outline _currentHighlight;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
			var ray = _camera.ScreenPointToRay(Input.mousePosition);
			DebugRay(ray, _rayLength);
			HandleHighlightLogic(ray, _rayLength);
			HandleCollectLogic(ray, _rayLength);
		}

		private void DebugRay(Ray ray, float rayLength)
		{
			if (_debugRay)
			{
				Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);
			}
		}

		private void HandleHighlightLogic(Ray ray, float rayLength)
		{
			ClearCurrentOutline();
			if (Physics.Raycast(ray, out var hit, rayLength))
			{
				if (hit.collider.TryGetComponent<Outline>(out var outline))
				{
					SetCurrentOutline(outline);
				}
			}
		}

		private void HandleCollectLogic(Ray ray, float rayLength)
		{
			if (Input.GetMouseButtonDown(0))
			{
			}
		}

		private void SetCurrentOutline(Outline outline)
		{
			_currentHighlight = outline;
			_currentHighlight.Highlight();
		}

		private void ClearCurrentOutline()
		{
			if (_currentHighlight != null)
			{
				_currentHighlight.ClearHighlight();
				_currentHighlight = null;
			}
		}
	}
}