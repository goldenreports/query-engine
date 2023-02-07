using System.IdentityModel.Tokens.Jwt;
using GoldenReports.QueryEngine.Configuration;
using GoldenReports.QueryEngine.Middlewares;
using GraphQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EngineSettings>(builder.Configuration);

builder.Services.AddGraphQL(builder => builder
        .AddSystemTextJson()
    // .AddErrorInfoProvider((opts, serviceProvider) =>
    // {
    //     var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
    //     opts.ExposeExceptionDetails = settings.Value.ExposeExceptions;
    // })
    // .AddSchema<StarWarsSchema>()
    // .AddGraphTypes(typeof(StarWarsQuery).Assembly)
    // .UseMiddleware<CountFieldMiddleware>(false) // do not auto-install middleware
    // .UseMiddleware<InstrumentFieldsMiddleware>(false) // do not auto-install middleware
    // .ConfigureSchema((schema, serviceProvider) =>
    // {
    //     // install middleware only when the custom EnableMetrics option is set
    //     var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
    //     if (settings.Value.EnableMetrics)
    //     {
    //         var middlewares = serviceProvider.GetRequiredService<IEnumerable<IFieldMiddleware>>();
    //         foreach (var middleware in middlewares)
    //             schema.FieldMiddleware.Use(middleware);
    //     }
    // })
);

builder.Services.AddSingleton<QueryEngineMiddleware>();

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        var corsSettings = builder.Configuration
            .GetSection($"{nameof(EngineSettings.Security)}:{nameof(SecuritySettings.Cors)}")
            .Get<CorsSettings>();
        
        policy.WithOrigins(corsSettings?.AllowedOrigins ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
        builder.Configuration.Bind($"{nameof(EngineSettings.Security)}:{nameof(SecuritySettings.Jwt)}", opts)
    );

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();

app.UseHttpsRedirection();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<QueryEngineMiddleware>();

var appSettings = builder.Configuration.Get<EngineSettings>();
if (appSettings?.EnableAltair == true)
{
    app.UseGraphQLAltair("/altair");
}

app.Run();