using AutoMapper;
using DiamondVillaAPI.DTOs;
using DiamondVillaAPI.Entity;

namespace DiamondVillaAPI.Profiles
{
	public class VillaProfile : Profile
	{
		public VillaProfile()
		{
			CreateMap<Villa, VillaDto>().ReverseMap();
			CreateMap<CreateVillaDto, Villa>();
			CreateMap<UpdateVillaDto, Villa>();
		}
	}
}
