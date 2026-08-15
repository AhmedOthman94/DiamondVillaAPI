using System.Text;
using DiamondVillaAPI.Data;
using DiamondVillaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(opts =>
{
	opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opts => 
{
	opts.RequireHttpsMetadata = false;
	opts.SaveToken = true;
	opts.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(key),
		ValidateIssuer = true,
		ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
		ValidateAudience = true,
		ValidAudience = builder.Configuration["JwtSettings:Audience"],
		ValidateLifetime = true,
		ClockSkew = TimeSpan.Zero
	};
});

builder.Services.AddDbContext<ApplicationDbContext>(opts => 
{
	opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAutoMapper(config => { },
		AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

await DatabaseSeeder.SeedDataAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference(opts => 
	{
		opts.WithTitle("Diamond Villa API")
			.WithTheme(ScalarTheme.Solarized)
			.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
	});
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
