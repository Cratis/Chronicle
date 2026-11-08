// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_another_revision_wins_the_first_write : given.an_event_with_revision_history
{
    given.before_revision_write _collision;
    void Establish()
    {
        _collision = new(() => Revise("winner"));
        _interceptors = [_collision];
    }
    async Task Because()
    {
        await Revise();
        await Read();
    }

    [Fact] void should_retry_the_stale_snapshot() => _collision.Attempts.ShouldEqual(3);
    [Fact] void should_keep_both_revisions() => _read.Revisions.Count().ShouldEqual(2);
    [Fact] void should_keep_the_first_original() => _read.OriginalContent.ShouldContain("original");
    [Fact] void should_apply_the_retried_revision_last() => _read.Context.Hash.Value.ShouldEqual("hash-recorded");
}
