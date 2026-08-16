using AutoMapper;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;

namespace DiamondVillaAPI.Profiles
{
	public class VillaAmenitiesProfile :Profile
	{
		public VillaAmenitiesProfile() 
		{
			CreateMap<VillaAmenities, VillaAmenitiesDto>()
			.ForMember(dest => dest.VillaName, opt => opt.MapFrom(src => src.Villa != null ? src.Villa.Name : null));

			CreateMap<VillaAmenitiesDto, VillaAmenities>();

			CreateMap<CreateVillaAmenitiesDto, VillaAmenities>();
			CreateMap<UpdateVillaAmenitiesDto, VillaAmenities>();

			CreateMap<VillaAmenities, UpdateVillaAmenitiesDto>();

		}
	}
}
