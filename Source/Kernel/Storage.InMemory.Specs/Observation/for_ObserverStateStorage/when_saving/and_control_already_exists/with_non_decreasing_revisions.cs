// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class with_non_decreasing_revisions : Specification
{
    ObserverStateStorage _storage;
    ObserverTypeSetControl _control;
    ObserverState _sameRevision;
    ObserverState _nextRevision;

    async Task Establish()
    {
        _storage = new();
        _control = new(new(7), new("reactor-subscriber", ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember>(), []);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control });
    }

    async Task Because()
    {
        var updated = _control with { Registrations = [new("connection-new", null, [], EventGenerationDelivery.Pinned)] };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = updated });
        _sameRevision = await _storage.Get("observer");
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = updated with { Revision = new(8) } });
        _nextRevision = await _storage.Get("observer");
    }

    [Fact] void should_allow_capability_changes_without_advancing_membership() => _sameRevision.TypeSetControl.Registrations.Single().ConnectionId.Value.ShouldEqual("connection-new");
    [Fact] void should_keep_the_unchanged_revision() => _sameRevision.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_allow_a_newer_membership_revision() => _nextRevision.TypeSetControl.Revision.Value.ShouldEqual(8);

    void Destroy() => _storage.Dispose();
}
