using System;
using System.Collections.Generic;
using System.Text;

namespace DiamondVillaDTO
{
	public class RefreshTokenRequestDto
	{
		public required string AccessToken { get; set; }
		public required string RefreshToken { get; set; }
	}
}
