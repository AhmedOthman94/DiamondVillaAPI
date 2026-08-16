using System.ComponentModel.DataAnnotations;

namespace DiamondVillaDTO
{
	public class VillaDto
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public string? Details { get; set; }
		public double Rate { get; set; }
		public int Sqft { get; set; }
		public int Occupancy { get; set; }
		public string? ImageUrl { get; set; }
		public ICollection<VillaAmenitiesDto> Amenities { get; set; } = new List<VillaAmenitiesDto>();
	}
}
