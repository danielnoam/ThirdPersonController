using UnityEngine;

namespace IacRaycasting.ScreenPointToRay.Scripts
{
	[RequireComponent(typeof(Camera))]
	public class ScreenPointToRayBehavior : MonoBehaviour
	{
		[SerializeField, Range(1, 100)] private float _rayLength = 10;
		[SerializeField] private string _highlightableTag;

		private Camera _camera;
		private Outline _currentHighlight;
		private Outline _currentSelection;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
			HandleHighlight();
			HandleSelection();
		}

		private void HandleHighlight(bool debugRay = true)
		{
			ClearCurrentOutline();
			var ray = _camera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hit, _rayLength))
			{
				if (hit.transform.CompareTag(_highlightableTag))
				{
					SetCurrentOutline(hit.transform);
				}
			}
			if (debugRay)
			{
				Debug.DrawRay(ray.origin, ray.direction * _rayLength, Color.red);
			}
		}

		private void HandleSelection()
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (_currentHighlight != null)
				{
					_currentSelection?.Deselect();
					_currentSelection = _currentHighlight;
					_currentSelection.Select();
				}
				else
				{
					_currentSelection?.Deselect();
				}
			}
		}

		private void SetCurrentOutline(Transform targetTransform)
		{
			var outline = targetTransform.GetComponent<Outline>();
			if (outline != null)
			{
				_currentHighlight = outline;
				_currentHighlight.Highlight();
			}
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