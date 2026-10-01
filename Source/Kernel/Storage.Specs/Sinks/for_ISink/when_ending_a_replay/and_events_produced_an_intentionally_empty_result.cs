// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

/// <summary>
/// Successful reducer event processing may intentionally produce no documents and must replace the old model.
/// </summary>
/// <typeparam name="THarness">The sink implementation under specification.</typeparam>
public abstract class and_events_produced_an_intentionally_empty_result<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginReplay(ReplayContext());
    }

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext() with { AllowEmptyResult = true });
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_replace_the_live_model_with_empty_state() => _count.ShouldBeNull();
}
