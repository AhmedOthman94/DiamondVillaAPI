using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DiamondVillaAPI.Services
{
	public class AuthService (ApplicationDbContext context,
					IMapper mapper,
					IConfiguration configuration
	)
	: IAuthService
	{
		public async Task<bool> IsEmailExistsAsync(string email)
		{
			return await context.Users
							.AnyAsync(u => u.Email.ToLower() == email.ToLower());
		}

		public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto logDto)
		{
			try 
			{
				var user = await context.Users.FirstOrDefaultAsync(
							u => u.Email.ToLower() == logDto.Email.ToLower()
			);
				if (user is null && user.Password != logDto.Password)
				{
					return null;
				}

				// Generate token 

				var token = GenerateToken(user);

				return new LoginResponseDto
				{
					UserDTO = mapper.Map<UserDTO>(user),
					Token = token
				};
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(
					"An expected error occurred during user login", ex
				);
			}
		}

		public async Task<UserDTO?> RegisterAsync(RegisterationRequestDTO regDto)
		{
			try 
			{
				if (await IsEmailExistsAsync(regDto.Email.ToLower()))
				{
					throw new InvalidOperationException(
							$"User with email {regDto.Email} already exists."
					);
				}

				User user = new()
				{
					Name = regDto.Name,
					Email = regDto.Email,
					Password = regDto.Password,
					Role = string.IsNullOrWhiteSpace(regDto.Role) ? "Customer" : regDto.Role,
					CreatedDate = DateTime.UtcNow
				};

				await context.Users.AddAsync(user);
				await context.SaveChangesAsync();

				var userToReturn = mapper.Map<UserDTO>(user);

				return userToReturn;
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(
					"An expected error occurred during user registeration", ex
				);
			}
		}

		private string GenerateToken(User user)
		{
			var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!);
			var securityKey = new SymmetricSecurityKey(key);
			var credentials = new SigningCredentials(
						securityKey,
						SecurityAlgorithms.HmacSha256
			);

			var claims = new List<Claim>() 
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Role, user.Role)
			};

			var expiresInMinutes = Convert.ToDouble(
						configuration["JwtSettings:ExpiresInMinutes"]		
			);

			var token = new JwtSecurityToken(
				issuer: configuration["JwtSettings:Issuer"],
				audience: configuration["JwtSettings:Audience"],
				expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
				claims: claims,
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
