using DiamondVillaAPI.DTOs;

namespace DiamondVillaAPI.Services
{
	public interface IAuthService
	{
		Task<UserDTO?> RegisterAsync(RegisterationRequestDTO regDto);
		Task<LoginResponseDto?> LoginAsync(LoginRequestDto logDto);
		Task<bool> IsEmailExistsAsync(string email);
	}
}
