using GS.Core.Extensions;
using GS.TenantService.Data;
using GS.TenantService.Grpc;
using GS.TenantService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureKestrelForGrpc(httpPort: 5000);

builder.AddObservability(configureTracing: static tracing =>
    Npgsql.TracerProviderBuilderExtensions.AddNpgsql(tracing));

builder.Services.AddGrpcServer(builder.Configuration, options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TenantDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TenantDb"));
});

builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpStatusExceptionHandling();
app.UseObservability();
app.MapControllers();
app.MapGrpcService<TenantResolverGrpcService>();

app.RunWithObservability();
