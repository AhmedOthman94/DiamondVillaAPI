using AutoMapper;
using DiamondVillaAPI.DTOs;
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
