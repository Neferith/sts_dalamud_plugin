using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sts.Api.DataSources;
using Sts.Api.Endpoints;
using Sts.Api.Repositories;
using Sts.Api.Services;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using Sts.Infrastructure.Data;
using Sts.Infrastructure.DataSources;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ─────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("StsWebDev", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7144",
                "http://localhost:5017")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("StsWeb", policy =>
    {
        policy.WithOrigins(
                "https://nlrp.fr",
                "https://www.nlrp.fr",
                "https://admin.nlrp.fr")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ─── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<DataService>();
//builder.Services.AddSingleton<RulesService>();

// Remplace : builder.Services.AddSingleton<RulesService>();
// Par :

builder.Services.AddDbContext<StsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("StsDb")));

builder.Services.AddScoped<IRulesDataSource, SqliteRulesDataSource>();
builder.Services.AddScoped<IRulesRepository, RulesRepository>();
builder.Services.AddScoped<IGetRulesUseCase, GetRulesUseCase>();
builder.Services.AddScoped<ICreateSectionUseCase, CreateSectionUseCase>();
builder.Services.AddScoped<IUpdateSectionUseCase, UpdateSectionUseCase>();
builder.Services.AddScoped<IDeleteSectionUseCase, DeleteSectionUseCase>();
builder.Services.AddScoped<ICreatePostUseCase, CreatePostUseCase>();
builder.Services.AddScoped<IUpdatePostUseCase, UpdatePostUseCase>();
builder.Services.AddScoped<IDeletePostUseCase, DeletePostUseCase>();

builder.Services.AddSingleton<IImageDataSource, FileSystemImageDataSource>();
builder.Services.AddSingleton<IImageRepository, ImageRepository>();
builder.Services.AddSingleton<IUploadImageUseCase, UploadImageUseCase>();
builder.Services.AddSingleton<IGetImagesUseCase, GetImagesUseCase>();
builder.Services.AddSingleton<IDeleteImageUseCase, DeleteImageUseCase>();

// ─── Auth JWT ─────────────────────────────────────────────────────────────────

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("La clé Jwt:Secret est manquante dans la configuration.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "sts-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "sts-admin";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// ─── Swagger ──────────────────────────────────────────────────────────────────

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "STS API",
        Version = "v1",
        Description = "API d'administration du Système Très Simple (STS).",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez votre token JWT : Bearer {token}",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── App ──────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Appliquer les migrations automatiquement au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "STS API v1");
        options.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsDevelopment())
    app.UseCors("StsWebDev");
else
    app.UseCors("StsWeb");
app.UseAuthentication();
app.UseAuthorization();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
var imagesPath = Path.Combine(builder.Environment.ContentRootPath, "images");
Directory.CreateDirectory(imagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "images")),
    RequestPath = "/images"
});

// ─── Endpoints ────────────────────────────────────────────────────────────────

app.MapAuthEndpoints();     // POST  /api/auth/login   (public)
app.MapDataEndpoints();     // GET   /api/data          (public — plugin)
app.MapRulesEndpoints();    // GET   /api/rules         (public — web)
app.MapJobEndpoints();      // CRUD  /api/jobs          (auth requis)
app.MapTraitEndpoints();    // CRUD  /api/traits        (auth requis)
app.MapAbilityEndpoints();  // CRUD  /api/abilities     (auth requis)
app.MapActionEndpoints();   // CRUD  /api/actions       (auth requis)
app.MapImageEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
