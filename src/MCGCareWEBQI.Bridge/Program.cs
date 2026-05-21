using MCGCareWEBQI.Bridge.Components;
using MCGCareWEBQI.Bridge.Endpoints;
using MCGCareWEBQI.Bridge.Services;
using MCGCareWEBQI.Data;
using MCGCareWEBQI.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

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

// ----------------------------------------------------------------------------
// Same-origin reverse proxy so the docked-mode iframe can load MCG safely.
//
//   /__mcg/*  ->  http://localhost:7080/*
//
// WHY: when the iframe lives in the bridge (origin :7090) but loads pages
// from the separate mock process (origin :7080), Chrome treats it as a
// third-party context and blocks the cookies Blazor needs for SignalR.
// Routing through the bridge keeps the iframe same-origin with the parent
// page; the inner Blazor circuit works normally.
// ----------------------------------------------------------------------------
var mcgUpstream = builder.Configuration["Mcg:UpstreamHost"] ?? "http://localhost:7080";
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        routes: new[]
        {
            new RouteConfig
            {
                RouteId   = "mcg",
                ClusterId = "mcg",
                Match     = new RouteMatch { Path = "/__mcg/{**catch-all}" },
            }
        },
        clusters: new[]
        {
            new ClusterConfig
            {
                ClusterId    = "mcg",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["mock"] = new DestinationConfig { Address = mcgUpstream }
                }
            }
        })
    .AddTransforms(ctx =>
    {
        if (ctx.Route.RouteId != "mcg") return;

        // Route inbound /__mcg/<path> → upstream /<path>
        ctx.AddPathRemovePrefix("/__mcg");

        // Tell upstream the public path base (mock honors this so its NavigationManager
        // and Location redirects include /__mcg). YARP's built-in X-Forwarded-* doesn't
        // include Prefix; we use our own header.
        ctx.RequestTransforms.Add(new XForwardedPrefixTransform("/__mcg"));

        // ---- PRODUCTION HARDENING ----
        // Real MCG (and many SaaS apps) send headers that prevent iframe embedding.
        // The whole dock-in-panel feature depends on iframing, so we strip them at
        // the proxy boundary. Without this the iframe would render a blank page in prod.
        ctx.ResponseTransforms.Add(new RemoveResponseHeaderTransform("X-Frame-Options"));
        ctx.ResponseTransforms.Add(new RemoveResponseHeaderTransform("Content-Security-Policy"));
        ctx.ResponseTransforms.Add(new RemoveResponseHeaderTransform("Content-Security-Policy-Report-Only"));
        ctx.ResponseTransforms.Add(new RemoveResponseHeaderTransform("Cross-Origin-Opener-Policy"));
        ctx.ResponseTransforms.Add(new RemoveResponseHeaderTransform("Cross-Origin-Embedder-Policy"));
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");

app.UseWebSockets();                // required so YARP can proxy Blazor SignalR
app.MapReverseProxy();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapReceiver();
app.MapTransactionApi();
app.MapPopupFrame();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
