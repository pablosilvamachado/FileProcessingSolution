using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FileProcessing.API.Configurations;

public static class SwaggerConfiguration
{
    public static void AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "File Processing API",
                Version = "v1"
            });

            // 🔐 JWT Autorização
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Insira: Bearer {seu_token_jwt}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Insira: Bearer {seu token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
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
        Array.Empty<string>()
    }
});


            c.OperationFilter<FileUploadOperationFilter>();

            // 📁 Suporte a Upload de Arquivo
            c.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
        });
    }
}
