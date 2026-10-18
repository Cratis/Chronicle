// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_completing_a_delivery_that_enqueues_the_next : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _original = new();
    readonly TaskCompletionSource<ObserverSubscriberResult> _next = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _continuation;

    void Establish()
    {
        _deliver("partition", [], _original);

        // Inline continuation deterministically places the next delivery inside the old SetResult/TryRemove window.
        _continuation = _original.Task.ContinueWith(_ => _deliver("partition", [], _next), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    async Task Because()
    {
        Reply();
        await _continuation.WaitAsync(TimeSpan.FromSeconds(5));
        Reply();
    }

    [Fact] void should_keep_the_next_delivery_until_its_own_result() => _next.Task.IsCompletedSuccessfully.ShouldBeTrue();
}
