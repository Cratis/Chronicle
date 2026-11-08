// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_event_type_carries_protected_data : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("customer-registered", EventTypeVisibility.Public, protectedData: true);
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        Append("customer-registered", 0);
        Append("order-placed", 1);
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_never_forward_the_protected_event() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(1UL);
    [Fact] void should_never_convert_the_protected_content() => _converter.ReceivedWithAnyArgs(1).ToJsonObject(default!, default!);
    [Fact] void should_advance_past_the_protected_event() => _batch.NextSequenceNumber.ShouldEqual(2UL);
}
