// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_unexpected_dispatch_failure : given.a_receiver
{
    void Establish() => _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns<IEventStore>(_ => throw new InvalidOperationException("connection failed"));
    Task Because() => Receive();
    [Fact] void should_report_failure_not_success() => _outcome.Status.ShouldEqual(DeliveryStatus.Failed);
    [Fact] void should_keep_the_failure_reason() => _outcome.ExceptionMessages.Single().ShouldEqual("connection failed");
}
