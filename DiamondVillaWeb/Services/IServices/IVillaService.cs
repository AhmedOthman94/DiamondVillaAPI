using DiamondVillaDTO;

namespace DiamondVillaWeb.Services.IServices
{
	public interface IVillaService
	{
		Task<T?> GetAllAsync<T>(string token);
		Task<T?> GetAsync<T>(int id, string token);
		Task<T?> CreateAsync<T>(CreateVillaDto createVillaDto, string token);
		Task<T?> UpdateAsync<T>(UpdateVillaDto updateVillaDto, string token);
		Task<T?> DeleteAsync<T>(int id, string token);
	}
}
