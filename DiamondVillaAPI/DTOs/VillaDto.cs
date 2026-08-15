using System.ComponentModel.DataAnnotations;

namespace DiamondVillaAPI.DTOs
{
	public class VillaDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Details { get; set; }
		public double Rate { get; set; }
		public int Sqft { get; set; }
		public int Occupancy { get; set; }
		public string? ImageUrl { get; set; }
	}
}
