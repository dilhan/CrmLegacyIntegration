using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CrmLegacyIntegration.Core.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="HttpResponseData"/> fake. The isolated worker gives no
/// first-party test double for this abstract type, so this exists purely to
/// let tests read back the <see cref="StatusCode"/> and <see cref="Body"/>
/// a trigger actually wrote — which is exactly what a unit test needs to
/// catch a WriteAsJsonAsync overload silently resetting the status to 200.
/// </summary>
public sealed class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies => null!;

    public string ReadBodyAsString()
    {
        Body.Position = 0;
        using var reader = new StreamReader(Body, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
