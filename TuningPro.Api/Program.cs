using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TuningPro.Api.Security;
using TuningPro.Business.Config;
using TuningPro.Business.Services;
using TuningPro.Business.Strategies;
using TuningPro.Data;
using TuningPro.Data.Repositories;
using QuestPDF.Infrastructure;
using TuningPro.Api.Reportes;
using TuningPro.Business.Reportes;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();

// Swagger con botón "Authorize" para probar con token
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega solo el token JWT (sin la palabra Bearer)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddCors(o => o.AddPolicy("Angular",
    p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

// ---------- Autenticación JWT ----------
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            RoleClaimType = "role",
            NameClaimType = "unique_name",
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// ---------- Capa de datos ----------
builder.Services.AddDbContext<TuningProDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("TuningProDb"),
        sql => sql.MigrationsAssembly("TuningPro.Data")));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));      // Repository
builder.Services.AddScoped<ICompatibilidadRepository, CompatibilidadRepository>();
builder.Services.AddScoped<IProyectoRepository, ProyectoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IVehiculoRepository, VehiculoRepository>();
builder.Services.AddScoped<IOrdenTrabajoRepository, OrdenTrabajoRepository>();

// ---------- Capa de negocio ----------
builder.Services.AddScoped<ICompatibilidadStrategy, FrenosStrategy>();         // Strategy
builder.Services.AddScoped<ICompatibilidadStrategy, SuspensionStrategy>();
builder.Services.AddScoped<ICompatibilidadStrategy, TurboStrategy>();
builder.Services.AddScoped<ICompatibilidadStrategy, GeneralStrategy>();
builder.Services.AddSingleton<ConfiguracionTaller>();                          // Singleton
builder.Services.AddScoped<ICompatibilidadService, CompatibilidadService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IProyectoService, ProyectoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPortalService, PortalService>();                   // reactivado
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IPiezaService, PiezaService>();
builder.Services.AddScoped<IHistorialService, HistorialService>();
builder.Services.AddSingleton<IReporteExportador, QuestPdfExportador>();       // Adapter
builder.Services.AddScoped<IReporteService, ReporteService>();                 // reactivado (esto causaba el error)
builder.Services.AddScoped<IOrdenTrabajoService, OrdenTrabajoService>();

var app = builder.Build();

// Crea los usuarios de demostración si la tabla está vacía
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await auth.SembrarUsuariosAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();