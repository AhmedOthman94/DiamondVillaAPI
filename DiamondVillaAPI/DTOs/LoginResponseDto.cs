namespace DiamondVillaAPI.DTOs
{
	public class LoginResponseDto
	{
		public string? Token { get; set; }
		public UserDTO? UserDTO { get; set; }
	}
}
