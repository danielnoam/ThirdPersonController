using UnityEngine;

namespace IacRaycasting.Aiming
{
	public class LaserCannonShoot : MonoBehaviour
	{
		[SerializeField] private Transform _muzzleTip;
		[SerializeField] private float _rayLength;

		private Outline _currentHighlight;
		
		private void Update()
		{
			AimLogic();
			ShootLogic();
		}

		private void AimLogic()
		{
			RaycastHit hitInfo;
			if (Physics.Raycast(_muzzleTip.position, _muzzleTip.forward, out hitInfo, _rayLength))
			{
				var hitObject = hitInfo.transform;
				_currentHighlight = hitObject.GetComponent<Outline>();
				if (_currentHighlight != null)
				{
					_currentHighlight.Highlight();
				}
			}
			else
			{
				if (_currentHighlight != null)
				{
					_currentHighlight.ClearHighlight();
					_currentHighlight = null;
				}
			}
		}

		private void ShootLogic()
		{
			if (Input.GetMouseButtonDown(0))
			{
				Debug.Log($"[LaserCannon] Shoot!");
				RaycastHit hitInfo;
				if (Physics.Raycast(_muzzleTip.position, _muzzleTip.forward, out hitInfo, _rayLength))
				{
					var enemyCannonScript = hitInfo.transform.GetComponent<EnemyCannon>();
					if (enemyCannonScript != null)
					{
						enemyCannonScript.Damage(20);
					}
				}
			}
		}
	}
}