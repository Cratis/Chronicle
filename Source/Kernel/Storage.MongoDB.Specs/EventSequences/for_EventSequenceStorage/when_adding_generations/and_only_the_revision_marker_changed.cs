// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_adding_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_only_the_revision_marker_changed(ReplicaSetMongoDBFixture fixture) : given.a_storage_with_stored_generations(fixture)
{
    string _payloadBefore;
    string _payloadAfterRevision;
    string _before;
    string _after;
    bool _added;
    int _revisionCount;

    async Task Establish()
    {
        await Revise();
        _observed = (await _storage.GetStoredGenerations(0))!;
        _payloadBefore = await PayloadFingerprint();
        await Revise();
        _payloadAfterRevision = await PayloadFingerprint();
        _before = await Fingerprint();
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target(sourceIsAppended: false)]);
        _after = await Fingerprint();
        _revisionCount = (await Stored(0)).Revisions.Count();
    }

    [Fact] void should_leave_content_hashes_and_provenance_unchanged_when_revising() => _payloadAfterRevision.ShouldEqual(_payloadBefore);
    [Fact] void should_reject_the_write() => _added.ShouldBeFalse();
    [Fact] void should_preserve_the_stored_event() => _after.ShouldEqual(_before);
    [Fact] void should_preserve_both_revisions() => _revisionCount.ShouldEqual(2);

    async Task<string> PayloadFingerprint()
    {
        var stored = await Stored(0);
        return new { stored.Content, stored.ContentHashes, stored.DerivedGenerations }.ToBsonDocument().ToJson();
    }
}
