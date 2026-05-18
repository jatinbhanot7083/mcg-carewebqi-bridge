namespace MCGCareWEBQI.Shared.Configuration;

/// Bound to the "Bridge" section. Controls bridge-side behavior independent of MCG.
public sealed class BridgeOptions
{
    public const string SectionName = "Bridge";

    /// Public base URL of the bridge itself. Used to build the returnUrl sent to MCG.
    /// e.g. https://mcg-bridge.example.com
    public string PublicBaseUrl { get; set; } = "";

    /// Path the bridge exposes to receive MCG's redirect/post-back. Default: /receive
    public string ReceiverPath { get; set; } = "/receive";

    /// Whether the bridge auto-closes its window after handing the result to opener via postMessage.
    public bool AutoCloseOnComplete { get; set; } = true;

    /// Comma-delimited list of allowed caller origins for postMessage(targetOrigin).
    /// Use "*" only for development; set explicit origins in production.
    public string AllowedCallerOrigins { get; set; } = "*";

    /// Whether to attempt a server-to-server callback POST when the launch URL includes callbackUrl.
    public bool EnableServerCallback { get; set; } = true;

    /// Number of retries for the callback POST.
    public int CallbackRetryCount { get; set; } = 3;
}
