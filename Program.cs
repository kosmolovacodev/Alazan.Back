using Microsoft.Data.SqlClient;
using System.Data;
using Alazan.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. SOPORTE PARA CONTROLADORES
builder.Services.AddControllers();

// 2. CONFIGURACIÓN DE LA BASE DE DATOS
builder.Services.AddScoped<IDbConnection>((sp) => 
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;


// --- CONFIGURACIÓN DE JWT ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// También es recomendable añadir esto para manejar la autorización
builder.Services.AddAuthorization();

// 3. CONFIGURACIÓN DE CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AlazanPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

// 4. SERVICIO EN SEGUNDO PLANO: Auto-autorización de precios por tiempo
builder.Services.AddHostedService<AutoAutorizacionPrecioService>();

var app = builder.Build();

// --- COMPATIBILIDAD CON SUB-APLICACIÓN IIS ---
// PathBase /api aplica siempre: en IIS la sub-app vive en /api,
// y en desarrollo local el frontend también envía rutas con /api/...
app.UsePathBase("/api");

// 4. MIDDLEWARES
app.UseRouting();

// Importante: Si usas UsePathBase, algunos recomiendan repetir el ruteo interno
app.UseCors("AlazanPolicy");

// --- AÑADIR ESTOS DOS ---
app.UseAuthentication(); // Primero: ¿Quién eres? (Valida el JWT)
app.UseAuthorization();  // Segundo: ¿Qué puedes hacer? (Valida Roles/Permisos)
// ------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 5. MAPEO DE RUTAS
app.MapControllers();

app.Run();