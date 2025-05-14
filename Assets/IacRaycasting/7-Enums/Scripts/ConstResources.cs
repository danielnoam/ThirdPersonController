namespace IacRaycasting.Enums.Scripts
{
	public class ConstResources
	{
		private const int COIN_RESOURCE_TYPE = 0;
		private const int HEALTH_RESOURCE_TYPE = 1;
		private const int HEART_RESOURCE_TYPE = 2;

		private int _coinsAmount;
		private int _healthAmount;
		private int _heartAmount;
		
		
		public void AddResource(int resourceType, int resourceAmount)
		{
			switch (resourceType)
			{
				case COIN_RESOURCE_TYPE:
					_coinsAmount += resourceAmount;
					break;
				case HEALTH_RESOURCE_TYPE:
					_healthAmount += resourceAmount;
					break;
				case HEART_RESOURCE_TYPE:
					_heartAmount += resourceAmount;
					break;
			}
		}
	}
}