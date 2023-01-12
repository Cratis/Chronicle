// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Websocket.Client;

namespace Aksio.Cratis.Clients.given;

public class a_responding_kernel : a_websocket_connection
{
    protected int message_count;
    protected List<ClientInformation> client_information;

    void Establish()
    {
        client_information = new();
        websocket_client.Setup(_ => _.Send(IsAny<string>())).Callback((string message) =>
        {
            messages.Add(message);

            if (message.StartsWith('{'))
            {
                client_information.Add(JsonSerializer.Deserialize<ClientInformation>(message));
                received.OnNext(ResponseMessage.TextMessage("kernel-connected"));
            }
        });
    }
}
