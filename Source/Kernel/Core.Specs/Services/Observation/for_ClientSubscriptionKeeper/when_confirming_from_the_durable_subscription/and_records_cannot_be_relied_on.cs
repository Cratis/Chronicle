// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_durable_subscription;

public class and_records_cannot_be_relied_on : given.a_confirmation_from_the_durable_subscription
{
    void Establish()
    {
        _records.ConfirmsClientSubscriptions.Returns(false);
        RecordNamesTheClient(true);
    }

    Task Because() => Confirm();

    [Fact] void should_ask_the_client_grain_as_before() => _grainCalls.ShouldEqual(1);
    [Fact] void should_not_read_the_record() => _records.DidNotReceive().Names(Arg.Any<ObserverKey>(), Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>());
}
