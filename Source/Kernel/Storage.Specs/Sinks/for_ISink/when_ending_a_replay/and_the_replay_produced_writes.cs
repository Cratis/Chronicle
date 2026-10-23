// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

/// <summary>
/// A replay writes to the sink's replay container; ending it has to make those writes the ones readers see.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_replay_produced_writes<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;
    int? _primaryCount;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL);
    }

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext());
        _count = await CurrentCountOrNull();
        var instance = await _sink.FindInPrimaryOrDefault(_key);
        _primaryCount = instance is null ? null : Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
    }

    [Fact] public void should_serve_the_promoted_instance_by_key() => _primaryCount.ShouldEqual(2);
    [Fact] public void should_serve_what_the_replay_produced() => _count.ShouldEqual(2);
}
