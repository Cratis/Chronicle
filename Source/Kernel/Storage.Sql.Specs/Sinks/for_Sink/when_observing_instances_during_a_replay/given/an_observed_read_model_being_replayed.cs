// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay.given;

/// <summary>
/// A read model holding one instance, being rebuilt by a replay through another sink that has already written
/// a different state for that instance and an instance the read model does not have yet.
/// </summary>
public class an_observed_read_model_being_replayed : for_Sink.given.two_sinks_for_one_read_model
{
    readonly ObservedCounts _observed = new();

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _otherSink.BeginReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _otherSink.ApplyChanges(new Key("counter-2", ArrayIndexers.NoIndexers), ChangesetSettingCountTo(5), 43UL);
    }

    void Destroy() => _observed.Dispose();

    /// <summary>
    /// Starts observing the read model through the sink queries use.
    /// </summary>
    protected void Observe() => _observed.Observe(_sink);

    /// <summary>
    /// Waits for the first page whose counts satisfy a condition.
    /// </summary>
    /// <param name="condition">The condition the counts on the page must satisfy.</param>
    /// <returns>The counts on the page, or null if no such page arrived in time.</returns>
    protected Task<int[]?> PageWhere(Func<int[], bool> condition) => _observed.PageWhere(condition);
}
