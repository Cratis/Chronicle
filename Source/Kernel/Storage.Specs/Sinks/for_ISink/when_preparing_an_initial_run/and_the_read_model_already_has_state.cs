// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_preparing_an_initial_run;

/// <summary>
/// An initial run starts from nothing, so whatever a previous run left behind has to be cleared.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_read_model_already_has_state<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;

    async Task Establish() => await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.PrepareInitialRun();
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_clear_what_was_there() => _count.ShouldBeNull();
}
