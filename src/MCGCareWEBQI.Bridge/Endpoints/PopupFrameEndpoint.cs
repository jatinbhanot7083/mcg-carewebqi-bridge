namespace MCGCareWEBQI.Bridge.Endpoints;

/// Serves a thin wrapper page (header bar + inner iframe) used when launching MCG into
/// a popup window. The header gives the clinician a "Dock back to EvokeConnect" button
/// so they can pull the session back into the docked panel without context-switching.
/// All messages from the inner MCG iframe are forwarded up to the parent (window.opener).
public static class PopupFrameEndpoint
{
    public static void MapPopupFrame(this IEndpointRouteBuilder app)
    {
        app.MapGet("/popup-frame", (HttpContext ctx, string launchUrl) =>
        {
            // Defensively encode for HTML attribute context.
            var safeUrl = System.Net.WebUtility.HtmlEncode(launchUrl ?? "");

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <title>MCG CareWebQI &mdash; popup session</title>
  <link rel=""stylesheet"" href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap"" />
  <link rel=""stylesheet"" href=""https://fonts.googleapis.com/icon?family=Material+Symbols+Rounded"" />
  <style>
    html, body {{ margin:0; height:100%; font-family:'Inter',sans-serif; background:#0f172a; }}
    .hd {{
      background:#0f172a; color:#e2e8f0; padding:9px 16px;
      display:flex; align-items:center; gap:12px; font-size:0.88rem;
      border-bottom:1px solid #1e293b;
    }}
    .title {{ color:#5eead4; font-weight:600; display:flex; align-items:center; gap:6px; }}
    .sub   {{ color:#94a3b8; font-size:0.78rem; }}
    .spacer{{ flex:1; }}
    .btn {{
      background:rgba(255,255,255,0.08); color:#ffffff;
      border:1px solid rgba(255,255,255,0.18); padding:6px 12px;
      border-radius:6px; cursor:pointer; display:inline-flex;
      align-items:center; gap:6px; font-size:0.82rem; font-family:inherit;
    }}
    .btn:hover {{ background:rgba(255,255,255,0.15); }}
    .btn .ms {{ font-family:'Material Symbols Rounded'; font-size:1rem; line-height:1; }}
    .btn.dock {{
      background:linear-gradient(135deg,#22d3ee,#6366f1); border:none; color:#ffffff;
      box-shadow:0 4px 12px rgba(99,102,241,0.30);
    }}
    .btn.close {{ background:transparent; color:#fca5a5; border-color:#ef4444; }}
    .btn.close:hover {{ background:rgba(239,68,68,0.10); color:#fecaca; }}
    iframe {{ flex:1; width:100%; border:none; background:#ffffff; }}
    .layout {{ display:flex; flex-direction:column; height:100vh; }}
  </style>
</head>
<body>
  <div class=""layout"">
    <div class=""hd"">
      <span class=""title""><span class=""ms"">cable</span>MCG CareWebQI</span>
      <span class=""sub"">running in popup &middot; session preserved if you dock back</span>
      <span class=""spacer""></span>
      <button class=""btn dock"" onclick=""dockBack()"">
        <span class=""ms"">dock_to_right</span>
        Dock back to EvokeConnect
      </button>
      <button class=""btn close"" onclick=""closeWindow()"">
        <span class=""ms"">close</span>
        Close
      </button>
    </div>
    <iframe id=""inner"" src=""{safeUrl}"" title=""MCG CareWebQI""></iframe>
  </div>

  <script>
    // Primary channel: BroadcastChannel works between same-origin tabs/windows
    // even if window.opener is lost. postMessage stays as a fallback.
    var bc = (typeof BroadcastChannel !== 'undefined') ? new BroadcastChannel('mcg-dock') : null;

    function readInnerUrl() {{
      try {{
        var f = document.getElementById('inner');
        return (f && f.contentWindow) ? f.contentWindow.location.href : null;
      }} catch (e) {{ console.warn('[popup] cannot read inner iframe url', e); return null; }}
    }}
    function dockBack() {{
      var url = readInnerUrl();
      var payload = {{ source:'mcg-bridge-popup', action:'dock-back', url:url }};
      console.log('[popup] dockBack, url=', url);
      // 1) Broadcast (primary)
      if (bc) {{ try {{ bc.postMessage(payload); }} catch (e) {{ console.warn('[popup] bc.postMessage failed', e); }} }}
      // 2) Opener postMessage (fallback)
      if (window.opener && !window.opener.closed) {{
        try {{ window.opener.postMessage(payload, '*'); }}
        catch (e) {{ console.warn('[popup] opener.postMessage failed', e); }}
      }} else {{
        console.warn('[popup] no window.opener — relying on BroadcastChannel only');
      }}
      setTimeout(function () {{ try {{ window.close(); }} catch (e) {{}} }}, 200);
    }}
    function closeWindow() {{ try {{ window.close(); }} catch (e) {{}} }}

    // 1) Parent can ASK us to dock back (via either channel)
    // 2) Forward MCG completion messages from the inner iframe up to the opener
    function maybeHandleAsk(d) {{
      if (d && d.source === 'mcg-bridge-opener' && d.action === 'request-dock-back') {{ dockBack(); return true; }}
      return false;
    }}
    if (bc) bc.addEventListener('message', function (e) {{ maybeHandleAsk(e.data); }});
    window.addEventListener('message', function (e) {{
      if (!e.data) return;
      if (maybeHandleAsk(e.data)) return;
      if (e.data.source === 'mcg-bridge') {{
        if (bc) {{ try {{ bc.postMessage(e.data); }} catch (err) {{}} }}
        if (window.opener && !window.opener.closed) {{
          try {{ window.opener.postMessage(e.data, '*'); }} catch (err) {{}}
        }}
        setTimeout(function () {{ try {{ window.close(); }} catch (e) {{}} }}, 1500);
      }}
    }});
  </script>
</body>
</html>";

            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}
