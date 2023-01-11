// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Websocket.Client;

namespace Aksio.Cratis.Clients;

public class when_connecting_successfully : given.a_websocket_connection
{
    int message_count;
    ClientInformation client_information;

    async Task Because()
    {
        websocket_client.Setup(_ => _.Send(IsAny<string>())).Callback((string message) =>
        {
            if (message_count++ == 0)
            {
                client_information = JsonSerializer.Deserialize<ClientInformation>(message);
                received.OnNext(ResponseMessage.TextMessage("kernel-connected"));
            }
        });

        await connection.Connect();
    }

    [Fact] void should_be_connected() => connection.IsConnected.ShouldBeTrue();
    [Fact] void should_send_correct_microservice_id_on_connect() => client_information.MicroserviceId.ShouldEqual(microservice_id);
    [Fact] void should_send_correct_endpoint_on_connect() => client_information.AdvertisedUri.ShouldEqual(endpoint.ToString());
}
