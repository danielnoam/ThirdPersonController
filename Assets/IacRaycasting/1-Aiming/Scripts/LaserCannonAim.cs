using UnityEngine;

namespace IacRaycasting.Aiming
{
	public class LaserCannonAim : MonoBehaviour
	{
		[SerializeField] private Transform _muzzleTip;
		[SerializeField] private float _rayLength;

		private void Update()
		{
			RaycastHit hitInfo;
			var isSomethingHit = Physics.Raycast(_muzzleTip.position, _muzzleTip.forward, out hitInfo, _rayLength);
			if (isSomethingHit)
			{
				Debug.Log("Wow! Something hit! Object hit: " + hitInfo.transform.name);
			}
			Debug.DrawRay(_muzzleTip.position, _muzzleTip.forward * _rayLength, Color.red);
		}
	}
}