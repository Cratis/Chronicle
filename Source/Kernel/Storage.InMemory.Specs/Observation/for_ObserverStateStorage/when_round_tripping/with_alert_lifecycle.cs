// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_round_tripping;

public class with_alert_lifecycle : Specification
{
    ObserverStateStorage _storage;
    ObserverState _original;
    ObserverState _saved;
    ObserverState _renamed;

    void Establish()
    {
        _storage = new();
        _original = new()
        {
            Identifier = "observer",
            AlertLifecycleId = Guid.NewGuid(),
            AlertRevision = 42,
            AlertDisposition = AlertDisposition.Removing,
            QuarantineEpisodeId = Guid.NewGuid()
        };
    }

    async Task Because()
    {
        await _storage.Save(_original);
        // Replacing the local record must not mutate durable metadata through a shared mutable child object.
        var changed = _original with { AlertRevision = 99, QuarantineEpisodeId = null };
        _saved = await _storage.Get(changed.Identifier);
        await _storage.Rename(_original.Identifier, "renamed");
        _renamed = await _storage.Get("renamed");
    }

    [Fact] void should_keep_the_saved_revision() => _saved.AlertRevision.ShouldEqual(42);
    [Fact] void should_keep_the_saved_quarantine() => _saved.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
    [Fact] void should_preserve_the_lifecycle_on_rename() => _renamed.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_preserve_the_revision_on_rename() => _renamed.AlertRevision.ShouldEqual(42);
    [Fact] void should_preserve_the_disposition_on_rename() => _renamed.AlertDisposition.ShouldEqual(AlertDisposition.Removing);
    [Fact] void should_preserve_the_quarantine_on_rename() => _renamed.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);

    void Destroy() => _storage.Dispose();
}
