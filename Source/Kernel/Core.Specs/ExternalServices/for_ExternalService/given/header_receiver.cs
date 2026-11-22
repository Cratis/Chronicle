// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices.for_ExternalService.given;

public class header_receiver : HttpMessageHandler
{
    public IReadOnlyDictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Headers = request.Headers.ToDictionary(_ => _.Key, _ => string.Join(',', _.Value));
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
