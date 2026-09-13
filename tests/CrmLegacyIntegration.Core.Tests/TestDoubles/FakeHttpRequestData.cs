using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CrmLegacyIntegration.Core.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="HttpRequestData"/> fake carrying just what the
/// triggers under test actually read: <see cref="Body"/> and
/// <see cref="Headers"/>. The isolated worker gives no first-party test
/// double for this abstract type.
/// </summary>
public sealed class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext, string? body = null, HttpHeadersCollection? headers = null)
        : base(functionContext)
    {
        Body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? string.Empty));
        Headers = headers ?? new HttpHeadersCollection();
    }

    public override Stream Body { get; }
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
    public override Uri Url { get; } = new("http://localhost/api/test");
    public override IEnumerable<ClaimsIdentity> Identities { get; } = Array.Empty<ClaimsIdentity>();
    public override string Method { get; } = "POST";

    public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
}
