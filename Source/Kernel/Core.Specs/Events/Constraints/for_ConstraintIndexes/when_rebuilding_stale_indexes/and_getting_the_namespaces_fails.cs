// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_rebuilding_stale_indexes;

/// <summary>
/// The definitions are already persisted when the rebuild runs, so a failure must not reach the registration: a
/// retried registration would see no change and never start the rebuild.
/// </summary>
public class and_getting_the_namespaces_fails : given.an_event_store_with_a_log_and_an_outbox
{
    Exception _error;

    void Establish() => _namespaces.GetAll().Returns(Task.FromException<IEnumerable<Concepts.EventStoreNamespaceName>>(new InvalidOperationException()));

    async Task Because() => _error = await Catch.Exception(() => _constraintIndexes.RebuildStaleIndexes(
        _eventStore,
        [UniqueEmail(EventSequenceId.Log)],
        [UniqueEmail()]));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_start_any_reindex() => _startedReindexes.ShouldBeEmpty();
}
