// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_first_touches_race : given.an_event_store_initialization
{
    Task[] _tasks;
    readonly TaskCompletionSource _registrationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _allCallsStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _callsStarted;

    void Establish() => _projections.DiscoverAndRegister(_name).Returns(_ =>
    {
        _registrationStarted.TrySetResult();
        return _releaseRegistration.Task;
    });

    async Task Because()
    {
        _tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            var initialization = _initialization.Ensure(_name);
            if (Interlocked.Increment(ref _callsStarted) == 8) _allCallsStarted.TrySetResult();
            return initialization;
        })).ToArray();
        try
        {
            await Task.WhenAll(_registrationStarted.Task, _allCallsStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _releaseRegistration.TrySetResult();
        }
        await Task.WhenAll(_tasks).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_call_the_initializer_once() => _initializerReference.Received(1).Ensure();
    [Fact] void should_register_projections_once() => _projections.Received(1).DiscoverAndRegister(_name);
    [Fact] void should_append_one_store_added_event() => _systemSequence.ReceivedCalls().Count(call => call.GetArguments().OfType<EventStoreAdded>().Any(added => added.EventStore == _name)).ShouldEqual(1);
    [Fact] void should_write_the_marker_once() => _store.Received(1).MarkInitialized();
    [Fact] void should_complete_every_call() => _tasks.All(task => task.IsCompletedSuccessfully).ShouldBeTrue();
}
