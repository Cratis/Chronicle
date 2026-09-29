// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_observing_instances;

/// <summary>
/// An instance on the observed page changes after the subscriber has received the first page.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
/// <remarks>
/// The first emission is awaited before the write, so the write is known to happen after the subscription is
/// established, and the second emission is awaited as the signal that the change was observed.
/// </remarks>
public abstract class and_an_instance_changes_after_subscribing<THarness> : a_populated_sink<THarness>
    where THarness : ISinkHarness, new()
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    readonly TaskCompletionSource<string[]> _initial = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<string[]> _updated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string[] _initialNames;
    string[] _updatedNames;
    int _emissions;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances(take: 2).Subscribe(Received, Failed);
        _initialNames = await _initial.Task.WaitAsync(_deadline);
        await Write("a", "renamed");
        _updatedNames = await _updated.Task.WaitAsync(_deadline);
    }

    [Fact] public void should_emit_the_first_page() => _initialNames.ShouldEqual(["a", "b"]);
    [Fact] public void should_emit_the_page_again_with_the_change() => _updatedNames.ShouldEqual(["renamed", "b"]);

    void Received(IEnumerable<ExpandoObject> instances)
    {
        var names = Names(instances);
        (Interlocked.Increment(ref _emissions) == 1 ? _initial : _updated).TrySetResult(names);
    }

    void Failed(Exception error)
    {
        _initial.TrySetException(error);
        _updated.TrySetException(error);
    }
}
