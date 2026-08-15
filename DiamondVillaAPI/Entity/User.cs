using System.ComponentModel.DataAnnotations;

namespace DiamondVillaAPI.Entity
{
	public class User
	{
		[Key]
		public int Id { get; set; }
		[Required]
		[EmailAddress]
		public required string Email { get; set; }
		[Required]
		[MaxLength(100)]
		public required string Name { get; set; }
		[Required]
		public required string Password { get; set; }
		[Required]
		[MaxLength(50)]
		public required string Role = "Customer";
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
	}
}
