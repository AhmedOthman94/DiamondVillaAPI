using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DiamondVillaDTO
{
	public class UpdateVillaAmenitiesDto
	{
		[Key]
		public int Id { get; set; }
		[Required]
		[MaxLength(100)]
		public required string Name { get; set; }
		public string? Description { get; set; }
		[Required]
		public int VillaId { get; set; }
		public IFormFile? Image {  get; set; }
	}
}
