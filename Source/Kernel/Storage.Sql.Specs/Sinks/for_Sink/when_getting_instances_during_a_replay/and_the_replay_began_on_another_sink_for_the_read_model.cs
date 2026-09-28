// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_getting_instances_during_a_replay;

/// <summary>
/// A query reaches the read model through its own sink while a replay rebuilds the read model through another.
/// Until the replay is promoted, the query answers with the state the read model had before the replay began.
/// </summary>
public class and_the_replay_began_on_another_sink_for_the_read_model : for_Sink.given.two_sinks_for_one_read_model
{
    ReadModelInstances _instances;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _otherSink.BeginReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _otherSink.ApplyChanges(new Key("counter-2", ArrayIndexers.NoIndexers), ChangesetSettingCountTo(5), 43UL);
    }

    async Task Because() => _instances = await _sink.GetInstances();

    [Fact] void should_answer_with_the_state_before_the_replay() => _instances.Instances.Select(CountOf).ShouldContainOnly(1);
    [Fact] void should_count_the_instances_it_answers_with() => _instances.TotalCount.ShouldEqual(_instances.Instances.Count());

    static int CountOf(ExpandoObject instance) =>
        Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
}
