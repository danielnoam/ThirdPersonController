using UnityEngine;

namespace IacRaycasting.Aiming
{
	public class EnemyCannon : MonoBehaviour
	{
		[SerializeField] private GameObject _explosionPrefab;
		[SerializeField] private int _health = 100;

		public void Damage(int damageToTake)
		{
			_health -= damageToTake;
			Debug.Log($"[EnemyCannon] Took damage: {damageToTake}, remaining health: {_health}");
			if (_health <= 0)
			{
				Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
				Destroy(gameObject);
			}
		}
	}
}