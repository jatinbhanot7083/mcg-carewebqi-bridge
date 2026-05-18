using MCGCareWEBQI.Bridge.Components;
using MCGCareWEBQI.Bridge.Endpoints;
using MCGCareWEBQI.Bridge.Services;
using MCGCareWEBQI.Data;
using MCGCareWEBQI.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bridge-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.Configure<McgOptions>(builder.Configuration.GetSection(McgOptions.SectionName));
builder.Services.Configure<BridgeOptions>(builder.Configuration.GetSection(BridgeOptions.SectionName));

builder.Services.AddDbContext<IntegrationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ReconcileSoapClient>();
builder.Services.AddHttpClient<CallbackService>();
builder.Services.AddScoped<IntegrationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.MapStaticAssets();
app.UseAntiforgery();

app.MapReceiver();
app.MapTransactionApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
