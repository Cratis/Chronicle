// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_appending_with_identities : given.a_fenced_append_storage
{
    async Task Because() => (await _storage.AppendManyWithIdentities(_context, [_event])).IsSuccess.ShouldBeTrue();

    [Fact] void should_commit_the_identity_with_the_event_and_marker() => _steps.SequenceEqual(["fence", "insert", "commit"]).ShouldBeTrue();
    [Fact] void should_persist_the_publication_identity() => _inserted.Single().PublicationId.ShouldEqual(_event.Identity!.Id);
    [Fact] void should_persist_the_publication_fingerprint() => _inserted.Single().PublicationFingerprint.ShouldEqual(_event.Identity!.Fingerprint);
    [Fact] void should_use_the_fenced_session() => _writeSessions.Single().ShouldEqual(_sessions.Single());
}
