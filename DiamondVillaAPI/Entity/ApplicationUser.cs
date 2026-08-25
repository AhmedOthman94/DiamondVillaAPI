using Microsoft.AspNetCore.Identity;

namespace DiamondVillaAPI.Entity
{
	public class ApplicationUser : IdentityUser
	{
		public string Name { get; set; } = string.Empty;
	}
}
