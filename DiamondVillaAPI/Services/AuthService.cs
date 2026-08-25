using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

namespace DiamondVillaAPI.Services
{
	public class AuthService (ApplicationDbContext context,
					UserManager<ApplicationUser> userManager,
					RoleManager<IdentityRole> roleManager,
					IMapper mapper,
					IConfiguration configuration
	)
	: IAuthService
	{
		public async Task<bool> IsEmailExistsAsync(string email)
		{
			return await context.ApplicationUsers
							.AnyAsync(u => u.Email!.ToLower() == email.ToLower());
		}

		public async Task<TokenDto?> LoginAsync(LoginRequestDto logDto)
		{
			try 
			{
				var user = await context.ApplicationUsers.FirstOrDefaultAsync(
							u => u.Email!.ToLower() == logDto.Email.ToLower()
			);
				if (user is null)
				{
					return null; // User not found
				}

				var isValid = await userManager.CheckPasswordAsync(user, logDto.Password);
				if (!isValid)
				{
					return null; // Invalid password
				}

				// Generate token 

				var token = await GenerateToken(user);
				var refreshTokenString = GenerateRefreshToken();

				var refreshToken = new RefreshToken
				{
					UserId = user.Id,
					Token = refreshTokenString,
					CreatedOn = DateTime.UtcNow,
					ExpiresOn = DateTime.UtcNow.AddDays(1)
				};

				await context.RefreshTokens.AddAsync(refreshToken);
				await context.SaveChangesAsync();

				TokenDto tokenDto = new()
				{
					AccessToken = token,
					RefreshToken = refreshTokenString,
					RefreshTokenExpiration = refreshToken.ExpiresOn
				};

				return tokenDto;
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

				ApplicationUser user = new()
				{
					Name = regDto.Name,
					Email = regDto.Email,
					UserName = regDto.Email,
					NormalizedEmail = regDto.Email.ToUpper(),
					NormalizedUserName = regDto.Name.ToUpper(),
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(user, regDto.Password);
				if (!result.Succeeded)
				{
					var errors = string.Join(", ", result.Errors.Select(e => e.Description));
					throw new InvalidOperationException($"Failed to register user: {errors}");
				}

				var role = string.IsNullOrEmpty(regDto.Role) ? "Customer" : regDto.Role.Trim().ToLower();

				if(!await roleManager.RoleExistsAsync(role))
				{
					await roleManager.CreateAsync(new IdentityRole(role));
				}

				await userManager.AddToRoleAsync(user, role);
				
				var userToReturn = mapper.Map<UserDTO>(user);
				userToReturn.Role = role;

				return userToReturn;
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(
					"An expected error occurred during user registeration", ex
				);
			}
		}

		private async Task<string> GenerateToken(ApplicationUser user)
		{
			var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!);
			var securityKey = new SymmetricSecurityKey(key);
			var credentials = new SigningCredentials(
						securityKey,
						SecurityAlgorithms.HmacSha256
			);

			var roles = await userManager.GetRolesAsync(user);

			var claims = new List<Claim>() 
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email!),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Role, roles.FirstOrDefault()!)
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

		private string GenerateRefreshToken()
		{
			var randomNumber = new byte[32];
			using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
			rng.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}

		public async Task<TokenDto?> RefreshTokenAsync(string accessToken, string refreshToken)
		{
			var principal = GetPrincipalFromExpiredToken(accessToken);
			if (principal == null)
			{
				return null;
			}

			var email = principal.FindFirstValue(ClaimTypes.Email);
			var user = await context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null)
			{
				return null;
			}

			var storedRefreshToken = await context.RefreshTokens
				.FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == user.Id);

			if (storedRefreshToken == null || !storedRefreshToken.IsActive)
			{
				return null;
			}

			storedRefreshToken.RevokedOn = DateTime.UtcNow;

			var newAccessToken = await GenerateToken(user);
			var newRefreshTokenString = GenerateRefreshToken();

			var newRefreshToken = new RefreshToken
			{
				Token = newRefreshTokenString,
				UserId = user.Id,
				CreatedOn = DateTime.UtcNow,
				ExpiresOn = DateTime.UtcNow.AddDays(7)
			};

			context.RefreshTokens.Add(newRefreshToken);
			await context.SaveChangesAsync();

			return new TokenDto
			{
				AccessToken = newAccessToken,
				RefreshToken = newRefreshTokenString,
				RefreshTokenExpiration = newRefreshToken.ExpiresOn
			};
		}

		private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
		{
			var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!);
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = true,
				ValidAudience = configuration["JwtSettings:Audience"],
				ValidateIssuer = true,
				ValidIssuer = configuration["JwtSettings:Issuer"],
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateLifetime = false
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			try
			{
				var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
				if (securityToken is not JwtSecurityToken jwtSecurityToken ||
					!jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				{
					return null;
				}
				return principal;
			}
			catch
			{
				return null;
			}
		}
	}
}
