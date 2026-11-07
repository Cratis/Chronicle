// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_a_released_revision_follows_recorded_history : given.an_event_with_revision_history
{
    async Task Establish()
    {
        await Revise("first");
        await ReleasedRevise();
    }
    async Task Because()
    {
        await Revise("last");
        await Read();
    }

    [Fact] void should_record_only_the_two_new_node_revisions() => _read.Revisions.Count().ShouldEqual(2);
    [Fact] void should_not_fabricate_the_released_revision() => _read.Revisions.Any(_ => _.Content.Contains("released", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_known_original() => _read.OriginalContent.ShouldContain("original");
    [Fact] void should_serve_the_latest_content_from_the_legacy_column() => _read.Context.Hash.Value.ShouldEqual("hash-last");
}
