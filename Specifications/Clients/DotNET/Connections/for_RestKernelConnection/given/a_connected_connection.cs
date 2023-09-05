// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients.for_RestKernelConnection.given;

public class a_connected_connection : a_rest_kernel_connection
{
    async Task Establish()
    {
        connection.http_client.Setup(_ => _.SendAsync(IsAny<HttpRequestMessage>(), CancellationToken.None)).Returns((HttpRequestMessage message, CancellationToken _) =>
        {
            var route = message.RequestUri.ToString();

            if (route == connect_route)
            {
                return Task.FromResult(success_message);
            }
            return Task.FromResult(GetMessageForRoute(route));
        });

        await connection.Connect();
        await ping_occurred.Task;
    }

    protected virtual HttpResponseMessage GetMessageForRoute(string route) => not_found_message;
}
