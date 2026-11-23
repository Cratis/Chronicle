// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_appending_a_single_event : given.a_fenced_append_storage
{
    Result<AppendedEvent, DuplicateEventSequenceNumber> _outcome;

    async Task Because() => _outcome = await _storage.Append(_context, _event);

    [Fact] void should_commit_after_the_marker_and_event_write() => _steps.SequenceEqual(["fence", "insert", "commit"]).ShouldBeTrue();
    [Fact] void should_return_the_appended_event() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_on_the_fenced_session() => _writeSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_propagate_activation_cancellation() => _writeCancellation.ShouldEqual(_activation.Token);
    [Fact] void should_not_persist_an_optional_identity_on_an_ordinary_append() => _inserted.Single().PublicationId.ShouldBeNull();
    [Fact] void should_preserve_compensation_links() => _inserted.Single().CompensationFor.ShouldEqual(_event.CompensationFor);
    [Fact] void should_preserve_named_tags() => _inserted.Single().NamedTags.Single().Value.ShouldEqual("east");
    [Fact] void should_normalize_stored_timestamps() => _inserted.Single().Occurred.Ticks.ShouldEqual(StoredTimestamps.Normalize(_event.Occurred).Ticks);
    [Fact] void should_return_the_same_normalized_timestamp() => ((AppendedEvent)_outcome).Context.Occurred.ShouldEqual(_inserted.Single().Occurred);
    [Fact] void should_return_the_stored_hash() => ((AppendedEvent)_outcome).Context.Hash.ShouldEqual(_event.Hash);
}
