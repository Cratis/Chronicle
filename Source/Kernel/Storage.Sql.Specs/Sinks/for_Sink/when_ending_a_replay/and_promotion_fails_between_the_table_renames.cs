// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_promotion_fails_between_the_table_renames : given.a_replay_with_an_interleaved_reader
{
    readonly promotion_interrupted _failure = new();
    Exception? _error;
    ReadModelInstances _primary;
    ReadModelInstances _replay;

    void Establish() => _afterPrimaryRename = () => Task.FromException(_failure);

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primary = await _reader.GetInstances();
        _replay = await _reader.GetInstances($"replay-{ContainerName}");
    }

    [Fact] void should_report_the_promotion_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_restore_the_previous_primary_table() => ((IDictionary<string, object?>)_primary.Instances.Single())["count"].ShouldEqual(1);
    [Fact] void should_keep_the_rebuilt_state() => ((IDictionary<string, object?>)_replay.Instances.Single())["count"].ShouldEqual(2);

    sealed class promotion_interrupted : Exception;
}
