// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

public class when_connecting_successfully : given.a_responding_kernel
{
    async Task Because() => await connection.Connect();

    [Fact] void should_be_connected() => connection.IsConnected.ShouldBeTrue();
    [Fact] void should_send_correct_microservice_id_on_connect() => client_information[0].MicroserviceId.ShouldEqual(microservice_id);
    [Fact] void should_send_correct_endpoint_on_connect() => client_information[0].AdvertisedUri.ShouldEqual(endpoint.ToString());
    [Fact] void should_send_first_ping() => messages.Last().ShouldEqual("ping");
    [Fact] void should_notify_about_connect() => client_lifecycle.Verify(_ => _.Connected(), Once);
}
