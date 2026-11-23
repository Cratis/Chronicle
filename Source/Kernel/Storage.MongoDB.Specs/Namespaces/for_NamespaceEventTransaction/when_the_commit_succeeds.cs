// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_commit_succeeds : given.a_namespace_transaction
{
    Task Because() => Execute();

    [Fact] void should_return_only_the_committed_result() => _result.ShouldEqual(42);
    [Fact] void should_write_the_fence_before_events_and_commit() => _steps.SequenceEqual(["fence", "write", "commit"]).ShouldBeTrue();
    [Fact] void should_supply_the_same_session_to_the_work() => _writeSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_use_snapshot_reads() => _options.ReadConcern.ShouldEqual(ReadConcern.Snapshot);
    [Fact] void should_read_from_the_primary() => _options.ReadPreference.ShouldEqual(ReadPreference.Primary);
    [Fact] void should_require_majority_commit_acknowledgement() => _options.WriteConcern.ShouldEqual(WriteConcern.WMajority);
    [Fact] void should_pass_activation_cancellation_to_the_marker() => _fenceCancellation.ShouldEqual(_activation.Token);
    [Fact] void should_pass_activation_cancellation_to_the_events() => _writeCancellation.ShouldEqual(_activation.Token);
    [Fact] void should_pass_activation_cancellation_to_commit() => _commitCancellation.ShouldEqual(_activation.Token);
    [Fact] void should_not_abort() => _aborts.ShouldEqual(0);
    [Fact] void should_dispose_the_session() => _sessions.Single().Received(1).Dispose();
}
