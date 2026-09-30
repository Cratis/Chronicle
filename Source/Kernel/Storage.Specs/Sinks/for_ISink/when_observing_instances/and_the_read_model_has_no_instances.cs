// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_observing_instances;

/// <summary>
/// A read model nothing has been written to is observed. Whether the backend has a container for it yet is its own
/// business, so the subscriber gets the initial empty page either way.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_read_model_has_no_instances<THarness> : an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _initial = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IEnumerable<ExpandoObject> _page;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(page => _initial.TrySetResult(page), error => _initial.TrySetException(error));
        _page = await _initial.Task.WaitAsync(_deadline);
    }

    [Fact] public void should_emit_an_empty_page() => _page.ShouldBeEmpty();
}
