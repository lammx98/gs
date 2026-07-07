using GS.Core.Extensions;
using GS.TenantService.Data;
using GS.TenantService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddGsObservability(configureTracing: static tracing =>
    Npgsql.TracerProviderBuilderExtensions.AddNpgsql(tracing));

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
app.UseGsObservability();
app.MapControllers();

app.RunWithObservability();
