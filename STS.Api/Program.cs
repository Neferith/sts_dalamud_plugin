using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Sts.Api.Auth;
using Sts.Api.DataSources;
using Sts.Api.Endpoints;
using Sts.Api.Repositories;
using Sts.Api.Services;
using Sts.Discord;
using Sts.Domain.Character;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Repository;
using Sts.Domain.User;
using Sts.Infrastructure.Data;
using Sts.Infrastructure.DataSources;
using STS.Api.Repositories;
using STS.Api.UseCases;
using STS.Export;
using System.Reflection;
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

// ─── Chemins fichiers User / Character ───────────────────────────────────────

var usersPath = builder.Configuration["Data:UsersFilePath"]
    ?? throw new InvalidOperationException("La clé Data:UsersFilePath est manquante dans la configuration.");

var charactersPath = builder.Configuration["Data:CharactersFilePath"]
    ?? throw new InvalidOperationException("La clé Data:CharactersFilePath est manquante dans la configuration.");

// ─── Auth + User ──────────────────────────────────────────────────────────────

builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

builder.Services.AddSingleton<IUserRepository>(
    new UserRepository(usersPath));

builder.Services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();
builder.Services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
builder.Services.AddScoped<IUpdateUserCodeUseCase, UpdateUserCodeUseCase>();
builder.Services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();
builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<ISeedAdminUseCase, SeedAdminUseCase>();

// ─── Characters ───────────────────────────────────────────────────────────────

builder.Services.AddSingleton<ICharacterRepository>(
    new CharacterRepository(charactersPath));

builder.Services.AddScoped<IGetAllCharactersUseCase, GetAllCharactersUseCase>();
builder.Services.AddScoped<IGetCharactersByUserUseCase, GetCharactersByUserUseCase>();
builder.Services.AddScoped<IGetCharacterByIdUseCase, GetCharacterByIdUseCase>();
builder.Services.AddScoped<ICreateCharacterUseCase, CreateCharacterUseCase>();
builder.Services.AddScoped<IUpdateCharacterUseCase, UpdateCharacterUseCase>();
builder.Services.AddScoped<IDeleteCharacterUseCase, DeleteCharacterUseCase>();

var uploadDir = builder.Configuration["Data:CharacterImagesPath"] ?? "/data/uploads/characters";
builder.Services.AddScoped<IUploadCharacterImageUseCase>(
    sp => new UploadCharacterImageUseCase(
        sp.GetRequiredService<ICharacterRepository>(),
        uploadDir));

// ─── Export ───────────────────────────────────────────────────────────────────

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddSingleton<DataServiceDataSource>();

builder.Services.AddScoped<IExportCharacterDiscordUseCase>(sp =>
{
    var ds = sp.GetRequiredService<DataServiceDataSource>();
    return new ExportCharacterDiscordUseCase(
        new DefaultTraitRepository(ds),
        new DefaultJobRepository(ds),
        new DefaultAbilityRepository(ds));
});

builder.Services.AddScoped<IExportCharacterPdfUseCase>(sp =>
{
    var ds = sp.GetRequiredService<DataServiceDataSource>();
    return new ExportCharacterPdfUseCase(
        new DefaultTraitRepository(ds),
        new DefaultJobRepository(ds),
        new DefaultAbilityRepository(ds),
        uploadDir);
});

builder.Services.AddScoped<IExportJobSheetPdfUseCase>(sp =>
{
    var ds = sp.GetRequiredService<DataServiceDataSource>();
    return new ExportJobSheetPdfUseCase(
        new DefaultTraitRepository(ds),
        new DefaultJobRepository(ds),
        new DefaultAbilityRepository(ds),
        uploadDir); // même variable que IExportCharacterPdfUseCase
});

ExportJobSheetPdfUseCase.RegisterFonts();




// Chemins fichiers
var quickLinksPath = builder.Configuration["Data:QuickLinksFilePath"]
    ?? throw new InvalidOperationException("La clé Data:QuickLinksFilePath est manquante dans la configuration.");

var siteSettingsPath = builder.Configuration["Data:SiteSettingsFilePath"]
    ?? throw new InvalidOperationException("La clé Data:SiteSettingsFilePath est manquante dans la configuration.");

var homeCardsPath = builder.Configuration["Data:HomeCardsFilePath"]
    ?? throw new InvalidOperationException("La clé Data:HomeCardsFilePath est manquante dans la configuration.");

builder.Services.AddSingleton<IHomeCardDataSource>(_ => new JsonHomeCardDataSource(homeCardsPath));
builder.Services.AddSingleton<IHomeCardRepository, HomeCardRepository>();
builder.Services.AddSingleton<IHomeCardReadRepository>(sp => sp.GetRequiredService<IHomeCardRepository>());
builder.Services.AddScoped<IGetHomeCardsUseCase, GetHomeCardsUseCase>();
builder.Services.AddScoped<IGetVisibleHomeCardsUseCase, GetVisibleHomeCardsUseCase>();
builder.Services.AddScoped<ICreateHomeCardUseCase, CreateHomeCardUseCase>();
builder.Services.AddScoped<IUpdateHomeCardUseCase, UpdateHomeCardUseCase>();
builder.Services.AddScoped<IDeleteHomeCardUseCase, DeleteHomeCardUseCase>();

// Repositories
builder.Services.AddSingleton<IQuickLinksRepository>(
    new QuickLinksRepository(quickLinksPath));


//builder.Services.AddScoped<IRulesDataSource, SqliteRulesDataSource>();
//builder.Services.AddScoped<IRulesRepository, RulesRepository>();

//builder.Services.AddScoped<ISiteSettingsDataSource, JsonSiteSettingsDataSource>();

builder.Services.AddSingleton<ISiteSettingsDataSource>(provider =>
{
    return new JsonSiteSettingsDataSource(siteSettingsPath);
});
builder.Services.AddSingleton<ISiteSettingsRepository, SiteSettingsRepository>();
   
// Repositories lecture seule — pointent vers les mêmes implémentations
builder.Services.AddSingleton<IQuickLinksReadRepository>(sp =>
    sp.GetRequiredService<IQuickLinksRepository>());
builder.Services.AddSingleton<ISiteSettingsReadRepository>(sp =>
    sp.GetRequiredService<ISiteSettingsRepository>());

// Use cases QuickLinks
builder.Services.AddScoped<IGetQuickLinksUseCase, GetQuickLinksUseCase>();
builder.Services.AddScoped<IGetVisibleQuickLinksUseCase, GetVisibleQuickLinksUseCase>();
builder.Services.AddScoped<ICreateQuickLinkUseCase, CreateQuickLinkUseCase>();
builder.Services.AddScoped<IUpdateQuickLinkUseCase, UpdateQuickLinkUseCase>();
builder.Services.AddScoped<IDeleteQuickLinkUseCase, DeleteQuickLinkUseCase>();

// Use cases SiteSettings
builder.Services.AddScoped<IGetSiteSettingsUseCase, GetSiteSettingsUseCase>();
builder.Services.AddScoped<IUpdateSiteSettingsUseCase, UpdateSiteSettingsUseCase>();

builder.Services.AddHostedService<AdminSeedService>();

builder.Services.AddDiscordBot(
    builder.Configuration,
    builder.Configuration["Discord:MappingsFilePath"]);

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("member", policy =>
        policy.RequireRole("member"));
});

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

var logger = app.Logger;
logger.LogError("Fonts embarquées : {Fonts}",
    string.Join(", ", typeof(ExportJobSheetPdfUseCase).Assembly
        .GetManifestResourceNames()));

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

app.MapGet("/api/version", () =>
{
    var version = typeof(Program).Assembly
        .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
        .Split('+')[0]
        ?? "unknown";
    return Results.Ok(new { version });
})
.WithName("GetVersion")
.WithTags("System")
.WithSummary("Retourne la version de l'application.")
.AllowAnonymous();

app.MapUserEndpoints();     // GET/POST /api/users         (auth requis)
app.MapCharacterEndpoints();  // CRUD  /api/characters     (auth requis)
app.MapAuthEndpoints(builder.Configuration);     // POST  /api/auth/login   (public)
app.MapDataEndpoints();     // GET   /api/data          (public — plugin)
app.MapRulesEndpoints();    // GET   /api/rules         (public — web)
app.MapJobEndpoints();      // CRUD  /api/jobs          (auth requis)
app.MapTraitEndpoints();    // CRUD  /api/traits        (auth requis)
app.MapAbilityEndpoints();  // CRUD  /api/abilities     (auth requis)
app.MapActionEndpoints();   // CRUD  /api/actions       (auth requis)
app.MapImageEndpoints();
app.MapQuickLinksEndpoints();   // GET /api/quick-links (public) + CRUD (auth)
app.MapSiteSettingsEndpoints(); // GET /api/site-settings (public) + PUT (auth)
app.MapHomeCardEndpoints(); // CRUD /api/home-cards (auth requis, GET /api/home-cards visible publiquement)
app.MapDiscordMappingsEndpoints(); // ← supprimer si Discord désactivé

app.MapFallbackToFile("index.html");

app.Run();
