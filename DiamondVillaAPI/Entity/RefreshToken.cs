namespace DiamondVillaAPI.Entity
{
	public class RefreshToken
	{
		public int Id { get; set; }
		public string UserId { get; set; } = string.Empty;
		public string Token { get; set; } = string.Empty;
		public DateTime ExpiresOn { get; set; }
		public bool IsExpired => DateTime.UtcNow >= ExpiredOn;
		public DateTime CreatedOn { get; set; }
		public DateTime? RevokedOn { get; set; }
		public bool IsActive => RevokedOn == null && !IsExpired;

		public ApplicationUser ApplicationUser { get; set; } = null!;
	}
}
