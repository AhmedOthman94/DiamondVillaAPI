using DiamondVillaDTO;
using DiamondVillaWeb.Models;

namespace DiamondVillaWeb.Services.IServices
{
	public interface IBaseService
	{
		ApiResponse<object> ResponseModel { get; set; }
		Task<T?> SendAsync<T>(ApiRequest apiRequest);
	}
}
