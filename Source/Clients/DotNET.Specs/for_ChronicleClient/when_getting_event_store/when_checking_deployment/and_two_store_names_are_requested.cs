// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_two_store_names_are_requested : given.a_serverless_client
{
    bool _noConstructorQuery;

    void Establish()
    {
        CreateClient();
        _noConstructorQuery = !_entered.Task.IsCompleted;
    }

    async Task Because()
    {
        await Task.WhenAll(_client.GetEventStore("first", "one"), _client.GetEventStore("second", "one"));
        await _bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        CompleteQuery(1, "deployed");
    }

    [Fact] void should_not_query_in_the_constructor() => _noConstructorQuery.ShouldBeTrue();
    [Fact] void should_query_each_store_once() => _services.EventStores.Received(2).GetEventStoreDeployment(Arg.Any<GetEventStoreDeploymentRequest>(), Arg.Any<CallContext>());
}
