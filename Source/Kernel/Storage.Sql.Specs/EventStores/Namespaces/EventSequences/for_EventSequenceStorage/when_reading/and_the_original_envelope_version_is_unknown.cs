// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_the_original_envelope_version_is_unknown : given.an_event_with_revision_history
{
    async Task Establish()
    {
        await Revise();
        await using var context = CreateContext();
        await context.Events.ExecuteUpdateAsync(setters => setters.SetProperty(_ => _.OriginalContent, "{\"format\":2,\"content\":{\"1\":{\"value\":\"original\"}}}"));
    }
    async Task Because() => await Read();

    [Fact] void should_not_decode_a_future_format() => _read.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_still_return_the_revision() => _read.Revisions.Count().ShouldEqual(1);
    [Fact] void should_still_return_the_current_hash() => _read.Context.Hash.Value.ShouldEqual("hash-recorded");
}
