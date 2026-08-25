using AutoMapper;
using DiamondVillaAPI.Data;
using DiamondVillaDTO;
using DiamondVillaAPI.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using DiamondVillaAPI.Services;

namespace DiamondVillaAPI.Controllers
{
	[Route("api/v1/villa")]
	[ApiExplorerSettings(GroupName = "v1")]
	[ApiController]
	[Produces("application/json")]
	public class VillaController(ApplicationDbContext context, 
						IImageService imageService,
						IMapper mapper) 
	: ControllerBase
	{
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaDto>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<IEnumerable<VillaDto>>>> GetVillas(
								[FromQuery] string? filterBy,
								[FromQuery] string? filterQuery,
								[FromQuery] string? sortBy,
								[FromQuery] string? sortOrder = "asc",
								[FromQuery] int pageNum = 1,
								[FromQuery] int pageSize = 10
		)
		{
			if (pageNum < 1) pageNum = 1;
			if (pageSize < 10) pageSize = 10;

			var villasQuery = context.Villas.AsQueryable();
			if (!string.IsNullOrEmpty(filterBy) && !string.IsNullOrEmpty(filterQuery))
			{
				switch (filterBy.Trim().ToLower())
				{
					case "name":
						villasQuery = villasQuery
							.Where(v => v.Name.ToLower().Contains(filterQuery.ToLower()));
						break;
					case "details":
						villasQuery = villasQuery
							.Where(v => v.Details!.ToLower().Contains(filterQuery.ToLower()));
						break;
					case "rate":
						if (double.TryParse(filterQuery, out double rate))
						{
							villasQuery = villasQuery
								.Where(v => v.Rate == rate);
						}
						break;
					case "minrate":
						if (double.TryParse(filterQuery, out double minRate))
						{
							villasQuery = villasQuery
								.Where(v => v.Rate >= minRate);
						}
						break;
					case "maxrate":
						if (double.TryParse(filterQuery, out double maxRate))
						{
							villasQuery = villasQuery
								.Where(v => v.Rate <= maxRate);
						}
						break;
					case "occupancy":
						if (int.TryParse(filterQuery, out int occupancy))
						{
							villasQuery = villasQuery
								.Where(v => v.Occupancy == occupancy);
						}
						break;
				};
			}

			if (!string.IsNullOrEmpty(sortBy))
			{
				var isDescending = sortOrder?.Trim().ToLower() == "desc";
				villasQuery = sortBy.ToLower() switch
				{
					"name" => isDescending ? villasQuery.OrderByDescending(v => v.Name)
						: villasQuery.OrderBy(v => v.Name),
					"occupancy" => isDescending ? villasQuery.OrderByDescending(v => v.Occupancy)
						: villasQuery.OrderBy(v => v.Occupancy),
					"rate" => isDescending ? villasQuery.OrderByDescending(v => v.Rate)
						: villasQuery?.OrderBy(v => v.Rate),
					"sqft" => isDescending ? villasQuery.OrderByDescending(v => v.Sqft)
						: villasQuery.OrderBy(v => v.Sqft),
					"id" => isDescending ? villasQuery.OrderByDescending(v => v.Id)
						: villasQuery.OrderBy(v => v.Id),
					_=> villasQuery.OrderBy(v => v.Id)
				};
			}
			else 
			{
				villasQuery = villasQuery.OrderBy(v => v.Id);
			}

			var skip = (pageNum - 1) * pageSize;
			var totalCount = await villasQuery!.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

			var villas = await villasQuery!
									.AsSplitQuery()
									.Include(x => x.Amenities)
									.AsNoTracking()
									.Skip(skip)
									.Take(pageSize)
									.ToListAsync();
			

			var villasToReturn = mapper.Map<IEnumerable<VillaDto>>(villas);
			

			Response.Headers.Append("X-Pagination-CurrentPage", pageNum.ToString());
			Response.Headers.Append("X-Pagination-PageSize", pageSize.ToString());
			Response.Headers.Append("X-Pagination-TotalCount", totalCount.ToString());
			Response.Headers.Append("X-Pagination-TotalPages", totalPages.ToString());

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
		[Authorize]
		[Consumes("multipart/form-data")]
		[ProducesResponseType(typeof(ApiResponse<VillaDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<VillaDto>>> CreateVilla([FromForm] CreateVillaDto createVillaDto)
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

			if (villa.Image != null)
			{
				if (!imageService.ValidateImage(villa.Image))
				{
					return BadRequest(
						ApiResponse<object>.BadRequest(
									"Invalid image, Allowed foramtes .jpeg, .jpg, .png, MaxSize = 5Mb")
					);
				}
				villa.ImageUrl = await imageService.UploadImageAsync(villa.Image);
			}

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
		[Authorize]
		[Consumes("multipart/form-data")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<object>>> UpdateVilla(int id, 
						[FromForm] UpdateVillaDto updateVillaDto)
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

				if (updateVillaDto.Image != null && ! imageService.ValidateImage(updateVillaDto.Image))
				{
					return BadRequest(
						ApiResponse<object>.BadRequest(
									"Invalid image, Allowed foramtes .jpeg, .jpg, .png, MaxSize = 5Mb")
					);
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

				var oldImageUrl = existingVilla.ImageUrl;

				mapper.Map(updateVillaDto, existingVilla);
				existingVilla.UpdatedDate = DateTime.UtcNow;

				if (updateVillaDto.Image != null)
				{
					existingVilla.ImageUrl = await imageService.UploadImageAsync(updateVillaDto.Image);
					updateVillaDto.ImageUrl = existingVilla.ImageUrl;
					if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != existingVilla.ImageUrl)
					{
						await imageService.DeleteImageAsync(oldImageUrl);
					}
				}

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
		[Authorize]
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

				if (!string.IsNullOrEmpty(existingVilla.ImageUrl))
				{
					await imageService.DeleteImageAsync(existingVilla.ImageUrl);
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