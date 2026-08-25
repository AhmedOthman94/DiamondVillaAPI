using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiamondVillaAPI.Controllers
{
	[Route("api/v2/villa-amenities")]
	[ApiExplorerSettings(GroupName = "v2")]
	[ApiController]
	[Produces("application/json")]
	public class VillaAmenitiesController(ApplicationDbContext context, IMapper mapper) : ControllerBase
	{
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaAmenitiesDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaAmenitiesDto>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<IEnumerable<VillaAmenitiesDto>>>> GetVillaAmenities()
		{
			try
			{
				var amenities = await context.Amenities
												.Include(a => a.Villa)
												.AsNoTracking()
												.OrderBy(a => a.Name)
												.ToListAsync();

				var amenitiesToReturn = mapper.Map<IEnumerable<VillaAmenitiesDto>>(amenities);
				var response = ApiResponse<IEnumerable<VillaAmenitiesDto>>.Ok(amenitiesToReturn, "Villa amenities retrieved successfully.");

				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<IEnumerable<VillaAmenitiesDto>>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while retrieving amenities: {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpGet("{id:int}", Name = "GetAmenityById")]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<VillaAmenitiesDto>>> GetAmenityById(int id)
		{
			try
			{
				if (id <= 0)
				{
					var badRequestResponse = ApiResponse<VillaAmenitiesDto>.BadRequest("ID must be greater than zero.");
					return BadRequest(badRequestResponse);
				}

				var amenity = await context.Amenities
											.Include(a => a.Villa)
											.AsNoTracking()
											.FirstOrDefaultAsync(a => a.Id == id);

				if (amenity is null)
				{
					var notFoundResponse = ApiResponse<VillaAmenitiesDto>.NotFound($"Amenity with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				var amenityToReturn = mapper.Map<VillaAmenitiesDto>(amenity);
				var okResponse = ApiResponse<VillaAmenitiesDto>.Ok(amenityToReturn, "Amenity retrieved successfully.");

				return Ok(okResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<VillaAmenitiesDto>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while retrieving the amenity with ID: {id} : {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpPost]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<VillaAmenitiesDto>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<VillaAmenitiesDto>>> CreateAmenity([FromBody] CreateVillaAmenitiesDto createDto)
		{
			try
			{
				if (createDto is null)
				{
					var badRequestResponse = ApiResponse<VillaAmenitiesDto>.BadRequest("Amenity data is required.");
					return BadRequest(badRequestResponse);
				}

				var villaExists = await context.Villas.AnyAsync(v => v.Id == createDto.VillaId);
				if (!villaExists)
				{
					var notFoundResponse = ApiResponse<VillaAmenitiesDto>.NotFound($"Villa with ID: {createDto.VillaId} does not exist.");
					return NotFound(notFoundResponse);
				}

				var duplicateAmenity = await context.Amenities
													.FirstOrDefaultAsync(a => a.VillaId == createDto.VillaId &&
																			   a.Name.ToLower() == createDto.Name.ToLower());
				if (duplicateAmenity is not null)
				{
					var conflictResponse = ApiResponse<VillaAmenitiesDto>.Conflict($"Amenity with name '{createDto.Name}' already exists for this villa.");
					return Conflict(conflictResponse);
				}

				var amenity = mapper.Map<VillaAmenities>(createDto);
				amenity.CreatedDate = DateTime.UtcNow;

				await context.Amenities.AddAsync(amenity);
				await context.SaveChangesAsync();

				await context.Entry(amenity).Reference(a => a.Villa).LoadAsync();

				var amenityDto = mapper.Map<VillaAmenitiesDto>(amenity);
				var createdResponse = ApiResponse<VillaAmenitiesDto>.CreatedAt(amenityDto, "Amenity created successfully.");

				return CreatedAtAction(
					nameof(GetAmenityById),
					new { id = amenity.Id },
					createdResponse
				);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<VillaAmenitiesDto>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while creating the amenity: {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpPut("{id:int}")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<object>>> UpdateAmenity(int id, [FromBody] UpdateVillaAmenitiesDto updateDto)
		{
			try
			{
				if (updateDto is null)
				{
					var badRequestResponse = ApiResponse<object>.BadRequest("Amenity data is required.");
					return BadRequest(badRequestResponse);
				}

				if (id != updateDto.Id)
				{
					var badRequestResponse = ApiResponse<object>.BadRequest("Amenity ID in URL does not match ID in request body.");
					return BadRequest(badRequestResponse);
				}

				var existingAmenity = await context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
				if (existingAmenity is null)
				{
					var notFoundResponse = ApiResponse<object>.NotFound($"Amenity with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				var villaExists = await context.Villas.AnyAsync(v => v.Id == updateDto.VillaId);
				if (!villaExists)
				{
					var notFoundResponse = ApiResponse<object>.NotFound($"Villa with ID: {updateDto.VillaId} does not exist.");
					return NotFound(notFoundResponse);
				}

				var duplicateAmenity = await context.Amenities
													.FirstOrDefaultAsync(a => a.VillaId == updateDto.VillaId &&
																			   a.Name.ToLower() == updateDto.Name.ToLower() &&
																			   a.Id != id);
				if (duplicateAmenity is not null)
				{
					var conflictResponse = ApiResponse<object>.Conflict($"Amenity with name '{updateDto.Name}' already exists for this villa.");
					return Conflict(conflictResponse);
				}

				mapper.Map(updateDto, existingAmenity);
				existingAmenity.UpdatedDate = DateTime.UtcNow;

				await context.SaveChangesAsync();

				var okResponse = ApiResponse<object>.NoContent("Amenity updated successfully.");
				return Ok(okResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while updating the amenity: {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}

		[HttpDelete("{id:int}")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<object>>> DeleteAmenity(int id)
		{
			try
			{
				var existingAmenity = await context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
				if (existingAmenity is null)
				{
					var notFoundResponse = ApiResponse<object>.NotFound($"Amenity with ID: {id} was not found.");
					return NotFound(notFoundResponse);
				}

				context.Amenities.Remove(existingAmenity);
				await context.SaveChangesAsync();

				var okResponse = ApiResponse<object>.NoContent("Amenity deleted successfully.");
				return Ok(okResponse);
			}
			catch (Exception ex)
			{
				var errorResponse = ApiResponse<object>.Error(
					StatusCodes.Status500InternalServerError,
					$"An error occurred while deleting the amenity: {ex.Message}"
				);
				return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
			}
		}
	}
}