// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics.given;

public class CapturingHttpMessageHandler : HttpMessageHandler
{
    readonly ConcurrentQueue<(HttpMethod Method, Uri? Uri, string? ContentType, byte[] Body)> _requests = new();

    public (HttpMethod Method, Uri? Uri, string? ContentType, byte[] Body)[] Snapshot() => _requests.ToArray();

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var body = new MemoryStream();
        request.Content.ReadAsStream(cancellationToken).CopyTo(body);
        _requests.Enqueue((request.Method, request.RequestUri, request.Content.Headers.ContentType?.MediaType, body.ToArray()));
        return new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        _requests.Enqueue((request.Method, request.RequestUri, request.Content.Headers.ContentType?.MediaType, body));
        return new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
    }
}
