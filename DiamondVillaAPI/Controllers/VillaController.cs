using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaAPI.DTOs;
using DiamondVillaAPI.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiamondVillaAPI.Controllers
{
	[Route("/api/villa")]
	[ApiController]
	public class VillaController (ApplicationDbContext context,
			IMapper mapper
	)
	: ControllerBase
	{
		[HttpGet]
		public async Task<ActionResult<IEnumerable<VillaDto>>> GetVillas()
		{
			var villas = await context.Villas
									.AsNoTracking()
									.OrderBy(v => v.Name)
									.ToListAsync();
			var villasToReturn = mapper.Map<IEnumerable<VillaDto>>(villas);

			return Ok(villasToReturn);
		}

		[HttpGet("{id:int}", Name = "GetVillaById")]
		public async Task<ActionResult<VillaDto>> GetVillaById(int id)
		{
			try
			{ 
				if (id <= 0)
				{
					return BadRequest("ID must be greater zan zero.");
				}
				var villa = await context.Villas
										.FirstOrDefaultAsync(v => v.Id == id);

				if (villa is null)
				{
					return NotFound($"Villa with ID: {id} was not found.");
				}

				var villaToReturn = mapper.Map<VillaDto>(villa);

				return Ok(villaToReturn);
			}
			catch(Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError,
					$"An error occurred while retrieving an villa with ID: {id} : {ex.Message}"
					);
			}
		}

		[HttpPost]
		public async Task<ActionResult<VillaDto>> CreateVilla(
							[FromBody]CreateVillaDto createVillaDto)
		{
			if (createVillaDto is null)
			{
				return BadRequest("Villa data is required.");
			}

			var villa = mapper.Map<Villa>(createVillaDto);

			await context.Villas.AddAsync(villa);
			await context.SaveChangesAsync();

			var villaDto = mapper.Map<VillaDto>(villa);

			return CreatedAtAction(nameof(GetVillaById),
						new {id = villa.Id},
						villaDto
						);
		}
	}
}
