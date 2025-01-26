using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Extensions;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Repository;
using APICatalogo.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Adicionar Servi�o do Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "APICatalogo",
        Description = "Cat�logo de Produtos e Categorias",
        TermsOfService = new Uri("https://augusto.net/terms"),
        Contact = new OpenApiContact
        {
            Name = "augusto",
            Email = "augustogc2@gmail.com",
            Url = new Uri("https://www.augusto.net")
        },
        License = new OpenApiLicense
        {
            Name = "Usar sobre LICX",
            Url = new Uri("https://www.augusto.net/licence")
        }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Header de autoriza��o JWT usando o esquema Bearer. \r\n\r\n" +
                      "Informe 'Bearer'[espa�o] e o seu token.\r\n\r\n" +
                      "Exemplo: \'Bearer 12345abcdef\'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);

});

// O m�todo AddJsonOptions com a op��o ReferenceHandler.IgnoreCycles, elimina o erro de refer�ncia c�clica na serializa��o
// de objetos os quais t�m classes que se referenciam mutuamente como uma agrega��o, usada no Entity Framework.
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Adicionando um servi�o
builder.Services.AddTransient<IMeuServico, MeuServico>();

// String de conex�o criada no appsettings.json
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

// Definir o contexto da conex�o (SGBD MySql)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection))
);

// Adicinar o Identity para a autoriza��o de acesso
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

/*
 JWT
 Adiciona o manipulador de autentica��o e define o esquema de autentica��o usado (Bearer).
 Valida o emissor, a audi�ncia e a chave usando a chave secreta v�lida e assinatura.
*/
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme).
        AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudience = builder.Configuration["TokenConfiguration:Audience"],
                ValidIssuer = builder.Configuration["TokenConfiguration:Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            });

// Adicionar os Cors em services
builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("EnableCORS", builder =>
        {
            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().Build();
        });
    });


// Adicionar versionamento (HeaderApiVersionReader -> define que a vers�o vai ser passada pelo Header)
//builder.Services.AddApiVersioning(options =>
//{
//    options.AssumeDefaultVersionWhenUnspecified = true;
//    options.DefaultApiVersion = new ApiVersion(1, 0);
//    options.ReportApiVersions = true;
//    options.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
//});

// Adicionar novo filtro de servi�o
builder.Services.AddScoped<ApiLoggingFilter>();

// Adicionar o provider de Logger
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

// Adicionar inje��o de depend�ncia para acessar os reposit�rios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Cria vari�vel com a configura��o do AutoMapper
var mappingConfig = new MapperConfiguration(mc =>
    {
        mc.AddProfile(new MappingProfile());
    });

// Adiciona o AutoMapper
IMapper mapper = mappingConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

var app = builder.Build();

// Adiciona o middleware de tratamento de erros
app.ConfigureExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "APICatalogo");
    });
}


app.UseCors("EnableCORS");

app.MapControllers();

app.Run();
