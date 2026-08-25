using DiamondVillaDTO;

namespace DiamondVillaAPI.Services
{
	public interface IAuthService
	{
		Task<UserDTO?> RegisterAsync(RegisterationRequestDTO regDto);
		Task<TokenDto?> LoginAsync(LoginRequestDto logDto);
		Task<TokenDto?> RefreshTokenAsync(string accessToken, string refreshToken);
		Task<bool> IsEmailExistsAsync(string email);
	}
}
