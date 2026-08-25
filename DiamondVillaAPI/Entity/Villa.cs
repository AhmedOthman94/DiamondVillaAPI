using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamondVillaAPI.Entity
{
	public class Villa
	{
		[Key]
		public int Id { get; set; }
		[Required]
		public required string Name { get; set; }
		public string? Details{ get; set; }
		public double Rate { get; set; }
		public int Sqft { get; set; }
		public int Occupancy { get; set; }
		public string? ImageUrl { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? UpdatedDate { get; set; }

		[NotMapped]
		public IFormFile? Image {  get; set; }

		public ICollection<VillaAmenities>? Amenities { get; set; }
	}
}
