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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.Configure<McgOptions>(builder.Configuration.GetSection(McgOptions.SectionName));

builder.Services.AddMemoryCache();
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.MapStaticAssets();
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
