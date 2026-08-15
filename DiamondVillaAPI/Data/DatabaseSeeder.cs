using Microsoft.EntityFrameworkCore;

namespace DiamondVillaAPI.Data
{
	public static class DatabaseSeeder
	{
		public static async Task SeedDataAsync(IServiceProvider service)
		{
			using var scope = service.CreateScope();
			var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await ctx.Database.MigrateAsync();
		}
	}
}
