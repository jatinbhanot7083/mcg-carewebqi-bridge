using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using MCGCareWEBQI.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace MCGCareWEBQI.Bridge.Services;

/// Hand-rolled SOAP 1.1 client for MCG's Reconcile.asmx. Hand-rolled (vs generated proxy)
/// keeps the dependency surface small and lets us swap to the stub without WSDL regen.
/// Contract namespace and SOAPAction values mirror the real MCG service.
public sealed class ReconcileSoapClient(
    HttpClient http,
    IOptions<McgOptions> mcgOptions,
    ILogger<ReconcileSoapClient> log)
{
    private const string Ns = "http://www.carewebqi.com/WS/Reconcile";

    public Task<string> AcknowledgeEpisodeAsync(string episodeId, CancellationToken ct = default)
        => CallAsync("AcknowledgeMessageByEpisode",
            new XElement(XName.Get("AcknowledgeMessageByEpisode", Ns),
                new XElement(XName.Get("EpisodeID", Ns), episodeId)),
            ct);

    public Task<string> AcknowledgeTransactionAsync(int transactionId, CancellationToken ct = default)
        => CallAsync("AcknowledgeMessageByTransaction",
            new XElement(XName.Get("AcknowledgeMessageByTransaction", Ns),
                new XElement(XName.Get("TransactionID", Ns), transactionId)),
            ct);

    private async Task<string> CallAsync(string action, XElement bodyContent, CancellationToken ct)
    {
        var endpoint = mcgOptions.Value.WebServicesUrl;
        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("Mcg:WebServicesUrl is not configured.");

        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        var envelope = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XElement(soap + "Body", bodyContent)));

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
        req.Headers.Add("SOAPAction", $"\"{Ns}/{action}\"");

        log.LogInformation("Calling Reconcile.{Action} at {Endpoint}", action, endpoint);
        using var resp = await http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Reconcile.{action} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

        return body;
    }
}
