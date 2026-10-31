// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving.and_control_already_exists;

[Collection(MongoDBCollection.Name)]
public class with_non_decreasing_revisions(MongoDBFixture fixture) : given.a_persisted_observer(fixture)
{
    Chronicle.Storage.Observation.ObserverState _sameRevision;

    async Task Because()
    {
        var updated = _control with { Registrations = [new("$replacement", null, [], EventGenerationDelivery.Pinned)] };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = updated }, Concepts.EventSequences.EventSequenceId.Log);
        _sameRevision = await _storage.Get("observer");
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = updated with { Revision = new(8) } }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_allow_capability_changes_without_advancing_membership() => _sameRevision.TypeSetControl.Registrations.Single().ConnectionId.Value.ShouldEqual("$replacement");
    [Fact] void should_keep_the_unchanged_revision() => _sameRevision.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_allow_a_newer_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(8);
}
