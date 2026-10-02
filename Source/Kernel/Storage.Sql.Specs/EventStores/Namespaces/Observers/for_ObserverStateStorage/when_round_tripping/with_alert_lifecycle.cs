// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_alert_lifecycle : given.an_observer_state_storage
{
    KernelObserverState _original;
    KernelObserverState _saved;
    KernelObserverState _renamed;

    void Establish() => _original = new()
    {
        Identifier = "observer",
        AlertLifecycleId = Guid.NewGuid(),
        AlertRevision = 42,
        AlertDisposition = AlertDisposition.Retired,
        QuarantineEpisodeId = Guid.NewGuid()
    };

    async Task Because()
    {
        await _storage.Save(_original);
        _saved = await _storage.Get(_original.Identifier);
        await _storage.Rename(_original.Identifier, "renamed");
        _renamed = await _storage.Get("renamed");
    }

    [Fact] void should_save_the_lifecycle() => _saved.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_save_the_revision() => _saved.AlertRevision.ShouldEqual(42);
    [Fact] void should_save_the_disposition() => _saved.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_save_the_quarantine_identity() => _saved.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
    [Fact] void should_preserve_the_lifecycle_on_rename() => _renamed.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_preserve_the_revision_on_rename() => _renamed.AlertRevision.ShouldEqual(42);
    [Fact] void should_preserve_the_disposition_on_rename() => _renamed.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_preserve_the_quarantine_on_rename() => _renamed.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
}
