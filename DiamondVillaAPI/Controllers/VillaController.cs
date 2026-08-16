using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiamondVillaAPI.Controllers
{
	[Route("api/villa")]
	[ApiController]
	[Produces("application/json")]
	public class VillaController(ApplicationDbContext context, IMapper mapper) : ControllerBase
	{
		[HttpGet]
		//[Authorize]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDto>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<IEnumerable<VillaDto>>>> GetVillas()
		{
			var villas = await context.Villas
							.Include(v => v.Amenities) 
							.AsNoTracking()
							.OrderBy(v => v.Name)
							.ToListAsync();

			var villasToReturn = mapper.Map<IEnumerable<VillaDto>>(villas);

			var response = ApiResponse<IEnumerable<VillaDto>>.Ok(villasToReturn, "Villas retrieved successfully.");
			return Ok(response);
		}

		[HttpGet("{id:int}", Name = "GetVillaById")]
		[ProducesResponseType(typeof(ApiResponse<VillaDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<VillaDto>>> GetVillaById(int id)
		{
			try
			{
				if (id <= 0)
				{
					var badRequestResponse = ApiResponse<VillaDto>.BadRequest("ID must be greater than zero.");
					return BadRequest(badRequestResponse);
				}

				var villa = await context.Villas
								.Include(v => v.Amenities) 
								.AsNoTracking()
								.FirstOrDefaultAsync(v => v.Id == id);

				if (villa is null)
				{
					var notFoundResponse = ApiResponse<VillaDto>.NotFound($"Villa with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				var villaToReturn = mapper.Map<VillaDto>(villa);
				var okResponse = ApiResponse<VillaDto>.Ok(villaToReturn, "Villa retrieved successfully.");

				return Ok(okResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<VillaDto>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while retrieving a villa with ID: {id} : {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpPost]
		[ProducesResponseType(typeof(ApiResponse<VillaDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<VillaDto>>> CreateVilla([FromBody] CreateVillaDto createVillaDto)
		{
			if (createVillaDto is null)
			{
				var badRequestResponse = ApiResponse<VillaDto>.BadRequest("Villa data is required.");
				return BadRequest(badRequestResponse);
			}

			var duplicateVilla = await context.Villas
											.FirstOrDefaultAsync(v => v.Name.ToLower() == createVillaDto.Name.ToLower());
			if (duplicateVilla is not null)
			{
				var conflictResponse = ApiResponse<VillaDto>.Conflict($"Villa with name {duplicateVilla.Name} already exists.");
				return Conflict(conflictResponse);
			}

			var villa = mapper.Map<Villa>(createVillaDto);

			await context.Villas.AddAsync(villa);
			await context.SaveChangesAsync();

			var villaDto = mapper.Map<VillaDto>(villa);
			var createdResponse = ApiResponse<VillaDto>.CreatedAt(villaDto, "Villa created successfully.");

			return CreatedAtAction(
				nameof(GetVillaById),
				new { id = villa.Id },
				createdResponse
			);
		}

		[HttpPut("{id:int}")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<object>>> UpdateVilla(int id, [FromBody] UpdateVillaDto updateVillaDto)
		{
			try
			{
				if (updateVillaDto is null)
				{
					var badRequestResponse = ApiResponse<object>.BadRequest("Villa data is required.");
					return BadRequest(badRequestResponse);
				}

				if (id != updateVillaDto.Id)
				{
					var badRequestResponse = ApiResponse<object>.BadRequest("Villa ID in URL does not match villa ID in request body.");
					return BadRequest(badRequestResponse);
				}

				var existingVilla = await context.Villas.FirstOrDefaultAsync(v => v.Id == id);
				if (existingVilla is null)
				{
					var notFoundResponse = ApiResponse<object>.NotFound($"Villa with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				var duplicateVilla = await context.Villas
											.FirstOrDefaultAsync(v => v.Name.ToLower() == updateVillaDto.Name.ToLower() && v.Id != id);
				if (duplicateVilla is not null)
				{
					var conflictResponse = ApiResponse<object>.Conflict($"Villa with name {duplicateVilla.Name} already exists.");
					return Conflict(conflictResponse);
				}

				mapper.Map(updateVillaDto, existingVilla);
				existingVilla.UpdatedDate = DateTime.UtcNow;

				await context.SaveChangesAsync();

				var noContentResponse = ApiResponse<object>.NoContent("Villa updated successfully.");
				return Ok(noContentResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while updating the villa : {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpDelete("{id:int}")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<object>>> DeleteVilla(int id)
		{
			try
			{
				var existingVilla = await context.Villas.FirstOrDefaultAsync(v => v.Id == id);
				if (existingVilla is null)
				{
					var notFoundResponse = ApiResponse<object>.NotFound($"Villa with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				context.Villas.Remove(existingVilla);
				await context.SaveChangesAsync();

				var noContentResponse = ApiResponse<object>.NoContent("Villa deleted successfully.");
				return Ok(noContentResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while deleting the villa : {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}
	}
}