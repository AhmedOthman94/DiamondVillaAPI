using System.Diagnostics;
using AutoMapper;
using DiamondVillaDTO;
using DiamondVillaWeb.Models;
using DiamondVillaWeb.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DiamondVillaWeb.Controllers
{
	public class HomeController (IVillaService villaService,
						IMapper mapper	
	)
	: Controller
	{
		public async Task<IActionResult> Index()
		{
			var villaList = new List<VillaDto>();
			try
			{
				var response = await villaService.GetAllAsync<ApiResponse<List<VillaDto>>>("");
				if (response != null && response.Success && response.Data != null)
				{
					villaList = response.Data;
				}
			}
			catch(Exception ex)
			{
				TempData["error"] = $"An error occurred: {ex.Message}";
			}

			return View(villaList);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
