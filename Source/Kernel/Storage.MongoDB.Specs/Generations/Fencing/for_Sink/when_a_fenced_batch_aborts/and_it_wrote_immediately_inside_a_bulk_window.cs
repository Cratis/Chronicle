// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.when_a_fenced_batch_aborts;

/// <summary>
/// A bulk window answers reads from an in-memory cache; a batch that aborts after its writes ran must not leave that cache changed.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/>.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_wrote_immediately_inside_a_bulk_window(ReplicaSetMongoDBFixture fixture) : Contract.given.an_accumulating_read_model<FencedMongoSinkHarness>
{
    readonly FailingProbe _probe = new();
    Exception? _error;
    int? _countSeenThroughTheWindow;

    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString, Probe = _probe };

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginBulk();
        var changeset = ChangesetSettingCountTo(2);
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = 2;
        changeset.CurrentState.Returns(state);
        _probe.FailNextCommit = true;
        _error = await Catch.Exception(() => _sink.ApplyChangesImmediately(_key, changeset, 43UL, SinkWriteMode.Always));
        _countSeenThroughTheWindow = await CurrentCountOrNull();
        await _sink.EndBulk();
    }

    [Fact] void should_fail_the_write() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_answer_reads_with_the_aborted_state() => _countSeenThroughTheWindow.ShouldEqual(1);

    sealed class FailingProbe : IFencedBatchProbe
    {
        public bool FailNextCommit { get; set; }

        public Task BeforeAdmission(FenceBatchId batchId) => Task.CompletedTask;

        public Task AfterAdmission(FenceBatchId batchId) => Task.CompletedTask;

        public Task BeforeCommit(FenceBatchId batchId)
        {
            if (!FailNextCommit)
            {
                return Task.CompletedTask;
            }

            FailNextCommit = false;
            throw new InvalidOperationException("The batch aborts after its writes ran");
        }
    }
}
