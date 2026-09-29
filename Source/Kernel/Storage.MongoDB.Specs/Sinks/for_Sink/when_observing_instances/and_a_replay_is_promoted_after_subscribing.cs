// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A replay is promoted after a subscriber has received the first page: ending it renames the collections, and the
/// subscription has to carry on across that and deliver the state the replay produced.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> supplying the replica set the change stream needs.</param>
/// <remarks>
/// The first emission is awaited before the replay begins, so the subscription is known to be established. The
/// promotion may be reported as several changes, some of them seeing the collection between its two renames, so
/// emissions are matched against the replayed state rather than counted.
/// </remarks>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_replay_is_promoted_after_subscribing(ReplicaSetMongoDBFixture fixture) : a_populated_sink<MongoSinkHarness>
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    readonly TaskCompletionSource<string[]> _initial = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<string[]> _replayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string[] _initialNames;
    string[] _replayedNames;

    protected override MongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances(take: 10).Subscribe(Received, Failed);
        _initialNames = await _initial.Task.WaitAsync(_deadline);

        var context = new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            "paged_read_models",
            "paged_read_models-revert",
            DateTimeOffset.UtcNow);
        await _sink.BeginReplay(context);
        await Write("a", "replayed");
        await _sink.EndReplay(context);

        _replayedNames = await _replayed.Task.WaitAsync(_deadline);
    }

    [Fact] void should_emit_the_first_page() => _initialNames.ShouldEqual(["a", "b", "c", "d", "e"]);
    [Fact] void should_emit_the_state_the_replay_produced_after_the_promotion() => _replayedNames.ShouldEqual(["replayed"]);

    void Received(IEnumerable<ExpandoObject> instances)
    {
        var names = Names(instances);
        if (!_initial.Task.IsCompleted)
        {
            _initial.TrySetResult(names);
        }
        else if (names.SequenceEqual(["replayed"]))
        {
            _replayed.TrySetResult(names);
        }
    }

    void Failed(Exception error)
    {
        _initial.TrySetException(error);
        _replayed.TrySetException(error);
    }
}
