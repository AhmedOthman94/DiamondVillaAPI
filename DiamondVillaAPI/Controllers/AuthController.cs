using DiamondVillaDTO;
using DiamondVillaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiamondVillaAPI.Controllers
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController (IAuthService authService)
	: ControllerBase
	{
		[HttpPost("register")]
		[ProducesResponseType(typeof(ApiResponse<UserDTO>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<UserDTO>>> Register(
							[FromBody]RegisterationRequestDTO regDto)
		{
			try 
			{
				if (regDto is null)
				{
					return BadRequest(ApiResponse<object>.BadRequest("Registeration data is required."));
				}

				if (await authService.IsEmailExistsAsync(regDto.Email))
				{
					return Conflict(ApiResponse<object>.Conflict(
							$"User with email: {regDto.Email} already exists."));
				}

				var user = await authService.RegisterAsync(regDto);
				if (user is null)
				{
					return BadRequest(ApiResponse<object>.BadRequest("Registeration failed."));
				}

				var response = ApiResponse<UserDTO>.Ok(user, "User Registered successfully.");
				return CreatedAtAction(nameof(Register), response);
			}
			catch(Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
							500, "An Error occurred while registeration.", ex.Message
				);

				return StatusCode(500, errorResponse);
			}
		}

		[HttpPost("login")]
		[ProducesResponseType(typeof(ApiResponse<UserDTO>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<TokenDto>>> Login(
						[FromBody]LoginRequestDto logDto
		)
		{
			try 
			{
				if (logDto is null)
				{
					return BadRequest(ApiResponse<object>.BadRequest("Login data is required."));
				}

				var loginResponse = await authService.LoginAsync(logDto);
				if (loginResponse is null)
				{
					return BadRequest(ApiResponse<object>.BadRequest("Login failed."));
				}

				var response = ApiResponse<TokenDto>.Ok(loginResponse, "Login successfully.");
				return Ok(response);
			}
			catch(Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
							500, "An Error ocuured while user login.", ex.Message
				);

				return StatusCode(500, errorResponse);
			}
		}
	}
}
