using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;

namespace CrmLegacyIntegration.Functions.OpenApi;

/// <summary>
/// Injects a small script into the Swagger UI page so that calling
/// Login -&gt; Try it out -&gt; Execute automatically authorizes the page with
/// the returned bearer token (via swagger-ui's own preauthorizeApiKey),
/// instead of requiring a manual copy/paste into the Authorize dialog. The
/// padlock icon flips to "locked" and every other operation's Try it out
/// then sends that token — no manual Authorize step needed.
///
/// This pokes at swagger-ui-dist internals (window.ui, fetch interception)
/// that aren't part of the OpenApi extension's public configuration
/// surface, so a future upgrade of
/// Microsoft.Azure.Functions.Worker.Extensions.OpenApi could change or
/// break the exact hook points used here. This is also the single
/// least-verified file in this repo — see the README. The manual
/// Authorize-button flow (paste the accessToken, no "Bearer " prefix)
/// always still works as a fallback regardless.
/// </summary>
public class SwaggerAutoAuthorizeScript : DefaultOpenApiCustomUIOptions
{
    public SwaggerAutoAuthorizeScript(Assembly assembly) : base(assembly)
    {
    }

    public override async Task<string> GetJavaScriptAsync()
    {
        var baseScript = await base.GetJavaScriptAsync();

        const string autoAuthorizeScript = """


            // Auto-authorize Swagger UI after a successful POST /api/login.
            (function () {
                var originalFetch = window.fetch;
                window.fetch = function () {
                    var fetchArgs = arguments;
                    return originalFetch.apply(this, fetchArgs).then(function (response) {
                        try {
                            var requestUrl = (fetchArgs[0] && fetchArgs[0].url) || fetchArgs[0];
                            if (typeof requestUrl === "string" &&
                                requestUrl.indexOf("/api/login") !== -1 &&
                                response.ok) {
                                response.clone().json().then(function (body) {
                                    if (body && body.accessToken && window.ui) {
                                        window.ui.preauthorizeApiKey("bearer_auth", body.accessToken);
                                    }
                                }).catch(function () { /* response wasn't JSON — ignore */ });
                            }
                        } catch (e) { /* never let this break the actual request */ }
                        return response;
                    });
                };
            })();
            """;

        return baseScript + autoAuthorizeScript;
    }
}
