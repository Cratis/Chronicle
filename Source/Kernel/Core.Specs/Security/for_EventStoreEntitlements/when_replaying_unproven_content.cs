// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

public class when_replaying_unproven_content : given.an_entitlements_grain
{
    [Theory]
    [InlineData("unsigned")]
    [InlineData("revised")]
    [InlineData("copied-source")]
    [InlineData("tombstone")]
    [InlineData("unreadable")]
    public async Task should_not_grant_from_unproven_slots(string kind)
    {
        await _entitlements.Grant(_grant);
        var original = Recorded<EventStoreAccessGranted>().Single();
        _appended.Clear();
        var source = kind == "copied-source" ? new EventSourceId("other") : StoreEntitlementsHistory.EventSourceId;
        var slot = SeedForged(source, kind == "unsigned" ? original with { Proof = string.Empty } : original);
        if (kind == "revised") ReviseSlot(slot.Context.SequenceNumber, original);
        if (kind == "tombstone")
        {
            var tombstone = slot with { Context = slot.Context with { EventType = new(GlobalEventTypes.Redaction, EventTypeGeneration.First) } };
            _appended[0] = tombstone;
            _contents[tombstone] = original;
        }
        if (kind == "unreadable") _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(_ => throw new FormatException("content"));
        (await CreateEntitlements().GetSnapshot()).Grants.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_remove_local_principals_and_their_grants(bool user)
    {
        var id = Guid.NewGuid();
        var grant = _grant with { Holder = new(PrincipalKey.LocalIssuer, id.ToString()) };
        await _entitlements.Grant(grant);
        SeedForged(id.ToString(), user ? new UserRemoved() : new ApplicationRemoved());
        (await CreateEntitlements().GetSnapshot()).Grants.ShouldBeEmpty();
    }
}
