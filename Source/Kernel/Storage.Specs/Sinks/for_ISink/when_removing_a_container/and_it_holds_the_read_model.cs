// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_container;

/// <summary>
/// Removing the container a read model lives in takes the read model with it. The persistent sinks drop
/// the container outright, so anything that keeps serving the documents afterwards has diverged.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_it_holds_the_read_model<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;

    async Task Establish() => await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.Remove(ContainerName);
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_not_serve_the_read_model_any_more() => _count.ShouldBeNull();
}
