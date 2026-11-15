// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_every_compare_and_swap_conflicts : given.an_event_with_revision_history
{
    given.before_revision_write _collision;
    Exception? _error;
    void Establish()
    {
        _collision = new(() => Task.CompletedTask, exhaust: true);
        _interceptors = [_collision];
    }
    async Task Because()
    {
        _error = await Catch.Exception(() => Revise());
        await Read();
    }

    [Fact] void should_stop_after_sixteen_attempts() => _collision.Attempts.ShouldEqual(16);
    [Fact] void should_fail_with_a_specific_concurrency_error() => _error.ShouldBeOfExactType<ConcurrentEventRevision>();
    [Fact] void should_leave_the_event_untouched() => _read.Context.Hash.Value.ShouldEqual("hash-original");
}
