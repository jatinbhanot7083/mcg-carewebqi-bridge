using Yarp.ReverseProxy.Transforms;

namespace MCGCareWEBQI.Bridge.Services;

/// Adds X-Mcg-Prefix to every proxied request so the upstream knows the
/// public path base it should generate URLs under (e.g. /__mcg → MCG mock UI).
public sealed class XForwardedPrefixTransform(string prefix) : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            context.ProxyRequest.Headers.Remove("X-Mcg-Prefix");
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Mcg-Prefix", prefix);
        }
        return default;
    }
}
