// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_released_redaction_left_history : given.an_event_with_revision_history
{
    async Task Establish()
    {
        await Revise();
        await using var context = CreateContext();
        await context.Events.ExecuteUpdateAsync(setters => setters.SetProperty(_ => _.Type, GlobalEventTypes.Redaction).SetProperty(_ => _.RevisedGeneration, (uint?)null).SetProperty(_ => _.Content, "{\"1\":{}}"));
    }
    async Task Because() => await Read();

    [Fact] void should_ignore_the_original() => _read.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_ignore_the_revisions() => _read.Revisions.Count().ShouldEqual(0);
    [Fact] void should_not_merge_original_generations() => _read.GenerationalContent[1].ShouldEqual("{}");
    [Fact] void should_leave_stale_history_for_the_startup_sweep() => _entry.Revisions.ShouldNotBeNull();
}
