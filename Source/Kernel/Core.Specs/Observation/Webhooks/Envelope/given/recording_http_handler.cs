// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.given;

public class recording_http_handler : HttpMessageHandler
{
    public List<byte[]> Bodies { get; } = [];
    public List<Dictionary<string, string>> Headers { get; } = [];
    public List<string> MediaTypes { get; } = [];
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string? ResponseBody { get; set; }
    public string ResponseMediaType { get; set; } = "application/json";
    public bool WaitForCancellation { get; set; }
    public bool WasCancelled { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Bodies.Add(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
        Headers.Add(request.Headers.ToDictionary(_ => _.Key, _ => string.Join(',', _.Value)));
        MediaTypes.Add(request.Content.Headers.ContentType!.ToString());
        if (WaitForCancellation)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                throw;
            }
        }
        if (ResponseBody is not null)
        {
            return new(Status) { Content = new StringContent(ResponseBody, Encoding.UTF8, ResponseMediaType) };
        }
        return new(Status);
    }
}
