using System.Text;
using DiamondVillaAPI.Data;
using DiamondVillaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!);

// Add services to the container.

builder.Services.AddCors();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1", opts => 
{
	opts.AddDocumentTransformer((document, context, CancellationToken) =>
	{
	document.Info = new()
	{
		Title = "Diamond Villa API",
		Version = context.DocumentName,
		Description = "Villas Web App"
	};

	document.Components ??= new OpenApiComponents();
	document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();

	document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme 
	{
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		Description = "Enter your Bearer token to access this API"
	});

		document.Security =
		[
			new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecuritySchemeReference("Bearer"),
					[]
				}
			}
		];

		return Task.CompletedTask;

	});
});

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

app.UseCors(o => 
	o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")
);

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
