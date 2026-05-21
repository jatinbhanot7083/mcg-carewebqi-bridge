using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using MCGCareWEBQI.MockServer.Components;
using MCGCareWEBQI.MockServer.Endpoints;
using MCGCareWEBQI.MockServer.Services;
using MCGCareWEBQI.MockServer.Services.Reconcile;
using MCGCareWEBQI.Shared.Configuration;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/mockserver-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.Configure<McgOptions>(builder.Configuration.GetSection(McgOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<McgSessionStore>();
builder.Services.AddSingleton<ReconcileService>();

// CoreWCF for SOAP-based Reconcile.asmx
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");

// When this server is fronted by the bridge's reverse proxy (dock-mode iframe),
// YARP forwards an X-Mcg-Prefix header. Honor it so that NavigationManager,
// <base href>, and Location redirects all carry the /__mcg prefix back to the browser.
app.Use((ctx, next) =>
{
    if (ctx.Request.Headers.TryGetValue("X-Mcg-Prefix", out var prefix) && !string.IsNullOrEmpty(prefix))
    {
        ctx.Request.PathBase = new PathString(prefix.ToString());
    }
    return next();
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapInterfaceLogin();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseServiceModel(svc =>
{
    svc.AddService<ReconcileService>(o => o.DebugBehavior.IncludeExceptionDetailInFaults = true);
    svc.AddServiceEndpoint<ReconcileService, IReconcileService>(
        new BasicHttpBinding(BasicHttpSecurityMode.None),
        "/WebServices/Reconcile.asmx");

    var smb = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    smb.HttpGetEnabled = true;
});

app.Run();
