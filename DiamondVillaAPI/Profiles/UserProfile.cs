using AutoMapper;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;

namespace DiamondVillaAPI.Profiles
{
	public class UserProfile : Profile
	{
		public UserProfile() 
		{
			CreateMap<User, UserDTO>().ReverseMap();
		}
	}
}
