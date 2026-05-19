using Yarp.ReverseProxy.Transforms;

namespace MCGCareWEBQI.Bridge.Services;

/// Strips a specified response header before it reaches the browser.
/// Used to remove iframe-blocking headers (X-Frame-Options, CSP frame-ancestors,
/// Cross-Origin-Opener-Policy, etc.) that real MCG would otherwise send,
/// which would break the dock-in-panel iframe.
public sealed class RemoveResponseHeaderTransform(string headerName) : ResponseTransform
{
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context.ProxyResponse is not null)
        {
            context.ProxyResponse.Headers.Remove(headerName);
            context.ProxyResponse.Content?.Headers.Remove(headerName);
        }
        // Also remove if the response has already been set on HttpContext.Response.
        context.HttpContext.Response.Headers.Remove(headerName);
        return default;
    }
}
