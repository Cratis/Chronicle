// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_an_observation_reads_between_the_table_renames : given.a_replay_with_an_interleaved_reader
{
    Exception? _error;
    IEnumerable<ExpandoObject> _observed;
    ReadModelInstances _promoted;
    ReadModelInstances _revert;

    void Establish() => _afterPrimaryRename = async () =>
        _observed = await _reader.ObserveInstances().FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _promoted = await _reader.GetInstances();
        _revert = await _reader.GetInstances(ReplayContext().RevertContainerName);
    }

    [Fact] void should_complete_the_promotion() => _error.ShouldBeNull();
    [Fact] void should_observe_the_previously_committed_state() => ((IDictionary<string, object?>)_observed.Single())["count"].ShouldEqual(1);
    [Fact] void should_publish_the_rebuilt_state() => ((IDictionary<string, object?>)_promoted.Instances.Single())["count"].ShouldEqual(2);
    [Fact] void should_preserve_the_previous_state_for_revert() => ((IDictionary<string, object?>)_revert.Instances.Single())["count"].ShouldEqual(1);
}
