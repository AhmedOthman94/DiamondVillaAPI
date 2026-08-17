using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DiamondVillaDTO;
using DiamondVillaWeb.Models;
using DiamondVillaWeb.Services.IServices;

namespace DiamondVillaWeb.Services
{
	public class BaseService : IBaseService
	{
		public IHttpClientFactory _httpClient { get; set; }

		private static readonly JsonSerializerOptions jsonOption = new()
		{
			PropertyNameCaseInsensitive = true,
		};

		public ApiResponse<object> ResponseModel { get; set; }

		public BaseService(IHttpClientFactory httpClient)
		{
			this.ResponseModel = new();
			_httpClient = httpClient;
		}

		public async Task<T?> SendAsync<T>(ApiRequest apiRequest)
		{
			try
			{
				var client = _httpClient.CreateClient("DiamondVillaAPI");
				var message = new HttpRequestMessage
				{
					RequestUri = new Uri(apiRequest.Url!, uriKind:UriKind.Relative),
					Method = GetHttpMethod(apiRequest.ApiType),
				};

				if(apiRequest.Data != null)
				{
					message.Content = JsonContent.Create(apiRequest.Data, options : jsonOption);
				}

				var apiResponse = await client.SendAsync(message);

				return await apiResponse.Content.ReadFromJsonAsync<T>(jsonOption);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected Error: {ex.Message}");
				return default;
			}
		}

		public async Task <T?> Sendo<T>(ApiRequest req)
		{
			try 
			{
				var client = _httpClient.CreateClient();
				var message = new HttpRequestMessage
				{
					RequestUri = new Uri(req.Url!),
					Method = GetHttpMethod(req.ApiType)
				};

				if (req.Data != null)
				{
					message.Content = JsonContent.Create(req.Data, options: jsonOption);
				}

				var apiResponse = await client.SendAsync(message);

				return await apiResponse.Content.ReadFromJsonAsync<T>(jsonOption);
			}
			catch (Exception ex) 
			{
				Console.WriteLine($"Unexpected error: {ex.Message}");
				return default;
			}
		}

		private static HttpMethod GetHttpMethod(SD.ApiType apiType)
		{
			return apiType switch
			{
				SD.ApiType.POST => HttpMethod.Post,
				SD.ApiType.PUT => HttpMethod.Put,
				SD.ApiType.DELETE => HttpMethod.Delete,
				_=> HttpMethod.Get
			};
		}
	}
}
