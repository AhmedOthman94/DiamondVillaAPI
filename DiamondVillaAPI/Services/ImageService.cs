namespace DiamondVillaAPI.Services
{
	public class ImageService (IWebHostEnvironment webHostEnvironment)
	: IImageService
	{
		private const long MaxImageSize = 5 * 1024 * 1024;
		private readonly string[] AllowedExtensions = { ".jpg", ".Jpeg", ".png"};

		public async Task<bool> DeleteImageAsync(string imageUrl)
		{
			if(string.IsNullOrEmpty(imageUrl))
			{
				return false;
			}

			var fileName = Path.GetFileName(imageUrl);
			var filePath = Path.Combine(webHostEnvironment.WebRootPath, "images", "villas", fileName);

			if (File.Exists(filePath))
			{
				await Task.Run(() => File.Delete(filePath));
				return true;
			}

			return false;
		}

		public async Task<string> UploadImageAsync(IFormFile file)
		{
			if (!ValidateImage(file))
			{
				throw new InvalidOperationException("Invalid Image.");
			}

			var rootPath = webHostEnvironment.WebRootPath
					?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

			

			var uploadFolder = Path.Combine(rootPath, "images", "villas");
			if (!Directory.Exists(uploadFolder))
			{
				Directory.CreateDirectory(uploadFolder);
			}

			var fileExtension = Path.GetExtension(file.FileName.ToLower());
			var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
			var filePath = Path.Combine(uploadFolder, uniqueFileName);
			
			using (var fileStream = new FileStream(filePath, FileMode.Create))
			{
				await file.CopyToAsync(fileStream);
			}

			return $"/images/villas/{uniqueFileName}";
		}

		public bool ValidateImage(IFormFile file)
		{
			if (file is null || file.Length == 0)
			{
				return false;
			}
			if (file.Length > MaxImageSize)
			{
				return false;
			}
			var extension = Path.GetExtension(file.FileName.ToLower());
			if (!AllowedExtensions.Contains(extension))
			{
				return false;
			}

			return true;
		}
	}
}
