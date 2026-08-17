using DiamondVillaDTO;
using DiamondVillaWeb.Models;
using DiamondVillaWeb.Services.IServices;

namespace DiamondVillaWeb.Services
{
	public class VillaService(IHttpClientFactory httpClient) : BaseService(httpClient), IVillaService
	{
		private const string ApiEndpoint = "/api/villa";

		public Task<T?> CreateAsync<T>(CreateVillaDto createVillaDto, string token)
		{
			return SendAsync<T>(new ApiRequest
			{
				ApiType = SD.ApiType.POST,
				Url = ApiEndpoint,
				Data = createVillaDto ,
				Token = token
			});
		}

		public Task<T?> DeleteAsync<T>(int id, string token)
		{
			return SendAsync<T>(new ApiRequest
			{
				ApiType = SD.ApiType.DELETE,
				Url = $"{ApiEndpoint}/{id}",
				Token = token
			});
		}

		public Task<T?> GetAllAsync<T>(string token)
		{
			return SendAsync<T>(new ApiRequest
			{
				ApiType = SD.ApiType.GET,
				Url = ApiEndpoint,
				Token = token
			});
		}

		public Task<T?> GetAsync<T>(int id, string token)
		{
			return SendAsync<T>(new ApiRequest
			{
				ApiType = SD.ApiType.GET,
				Url = $"{ApiEndpoint}/{id}",
				Token = token
			});
		}

		public Task<T?> UpdateAsync<T>(UpdateVillaDto updateVillaDto, string token)
		{
			return SendAsync<T>(new ApiRequest
			{
				ApiType = SD.ApiType.PUT,
				Url = ApiEndpoint,
				Data = updateVillaDto,
				Token = token
			});
		}
	}
}
