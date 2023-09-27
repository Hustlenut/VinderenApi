namespace VinderenApi.Authentication
{
	public class LoginAttemptInfo
	{
		public int Attempts { get; set; }
		public DateTime ExpiryDate { get; set; }
	}
}
