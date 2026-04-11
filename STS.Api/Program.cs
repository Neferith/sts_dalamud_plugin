using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sts.Api.DataSources;
using Sts.Api.Endpoints;
using Sts.Api.Repositories;
using Sts.Api.Services;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
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

//builder.Services.AddSingleton<DataService>();
//builder.Services.AddSingleton<RulesService>();

// Remplace : builder.Services.AddSingleton<RulesService>();
// Par :

builder.Services.AddSingleton<IRulesDataSource, JsonRulesDataSource>();
builder.Services.AddSingleton<IRulesRepository, RulesRepository>();
builder.Services.AddSingleton<IGetRulesUseCase, GetRulesUseCase>();
builder.Services.AddSingleton<ICreateSectionUseCase, CreateSectionUseCase>();
builder.Services.AddSingleton<IUpdateSectionUseCase, UpdateSectionUseCase>();
builder.Services.AddSingleton<IDeleteSectionUseCase, DeleteSectionUseCase>();
builder.Services.AddSingleton<ICreatePostUseCase, CreatePostUseCase>();
builder.Services.AddSingleton<IUpdatePostUseCase, UpdatePostUseCase>();
builder.Services.AddSingleton<IDeletePostUseCase, DeletePostUseCase>();

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

// ─── Endpoints ────────────────────────────────────────────────────────────────

app.MapAuthEndpoints();     // POST  /api/auth/login   (public)
app.MapDataEndpoints();     // GET   /api/data          (public — plugin)
app.MapRulesEndpoints();    // GET   /api/rules         (public — web)
app.MapJobEndpoints();      // CRUD  /api/jobs          (auth requis)
app.MapTraitEndpoints();    // CRUD  /api/traits        (auth requis)
app.MapAbilityEndpoints();  // CRUD  /api/abilities     (auth requis)
app.MapActionEndpoints();   // CRUD  /api/actions       (auth requis)

app.MapFallbackToFile("index.html");

app.Run();
