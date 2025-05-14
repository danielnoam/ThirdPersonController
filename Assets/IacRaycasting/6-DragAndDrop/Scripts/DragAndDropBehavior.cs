using UnityEngine;

namespace IacRaycasting.DragAndDrop.Scripts
{
	[RequireComponent(typeof(Camera))]
	public class DragAndDropBehavior : MonoBehaviour
	{
		[SerializeField, Range(1, 100)] private float _rayLength = 10;
		[SerializeField] private string _highlightableTag;
		[SerializeField] private string _groundPlaneTag;

		private Camera _camera;
		private Outline _currentHighlight;
		private Outline _currentSelection;
		private Transform _currentlyDraggedObject;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
			if (IsInDragAndDrop())
			{
				DetectDragAndDropEnd();
				HandleDragAndDropLogic(_currentlyDraggedObject);
			}
			else
			{
				HandleHighlight();
				HandleSelection();
				DetectDragAndDropStart();
			}
		}

		private bool IsInDragAndDrop()
		{
			return _currentlyDraggedObject != null;
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
					_currentHighlight = null;
				}
				else
				{
					_currentSelection?.Deselect();
				}
			}
		}

		private void DetectDragAndDropStart()
		{
			if (Input.GetMouseButtonDown(1))
			{
				if (_currentHighlight != null)
				{
					_currentlyDraggedObject = _currentHighlight.gameObject.transform;
					_currentHighlight.ClearHighlight();
				}
			}
		}

		private void HandleDragAndDropLogic(Transform draggedObject)
		{
			var ray = _camera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hit, _rayLength))
			{
				if (hit.transform.CompareTag(_groundPlaneTag))
				{
					draggedObject.position = hit.point;
				}
			}
		}

		private void DetectDragAndDropEnd()
		{
			if (_currentlyDraggedObject != null)
			{
				if (Input.GetMouseButtonUp(1))
				{
					_currentlyDraggedObject = null;
					_currentHighlight?.ClearHighlight();
					_currentHighlight = null;
					_currentSelection?.Deselect();
					_currentSelection = null;
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