// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_grant_lists_specific_event_types : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller("link-a", false, "granted-public", "granted-private", "granted-unspecified"));
        RegisterEventType("granted-public", EventTypeVisibility.Public);
        RegisterEventType("granted-private", EventTypeVisibility.Private);
        RegisterEventType("granted-unspecified", EventTypeVisibility.Unspecified);
        RegisterEventType("not-granted-public", EventTypeVisibility.Public);
        Append("granted-public", 0);
        Append("granted-private", 1);
        Append("granted-unspecified", 2);
        Append("not-granted-public", 3);
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_only_deliver_the_granted_public_event() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(0UL);
    [Fact] void should_never_deliver_a_granted_event_type_that_is_not_public() => _batch.Events.Any(_ => _.EventType != "granted-public").ShouldBeFalse();
}
