// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_appending_a_batch : given.a_fenced_append_storage
{
    async Task Because() => (await _storage.AppendMany(_context, [_event, _event with { SequenceNumber = 8 }])).IsSuccess.ShouldBeTrue();

    [Fact] void should_commit_both_events_under_one_fence() => _steps.SequenceEqual(["fence", "insert", "commit"]).ShouldBeTrue();
    [Fact] void should_write_both_events_on_the_same_session() => _writeSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_persist_every_event() => _inserted.Length.ShouldEqual(2);
    [Fact] void should_ignore_publication_identities() => _inserted.All(item => item.PublicationId is null).ShouldBeTrue();
}
