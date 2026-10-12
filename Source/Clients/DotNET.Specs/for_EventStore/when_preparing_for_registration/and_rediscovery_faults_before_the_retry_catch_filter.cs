// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_rediscovery_faults_before_the_retry_catch_filter : given.an_explicit_store
{
    readonly TaskCompletionSource _retryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<CommandResult> _releaseTransport = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _beforeFilter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseFilter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Exception _transportFailure = new("transport unavailable");
    readonly Exception _preparationFailure = new("newer rediscovery failure");
    int _wireAttempts;
    Task _backgroundRetry;
    Exception? _outcomeAtFault;

    async Task Establish()
    {
        _options.RegistrationRetry.MaxAttempts = 1;
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            _wireAttempts++;
            if (_wireAttempts == 1) return Task.FromException<CommandResult>(_transportFailure);
            _retryEntered.TrySetResult();
            return _releaseTransport.Task;
        });
        _store = CreateStore();
        await _store.DiscoverAll();

        // Keep the real registration core and lease, but gate propagation to the background loop's catch filter.
        var core = typeof(EventStore).GetMethod("RegisterAllCore", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<Task>>(_store);
        var flight = new SingleFlightRegistration(async () =>
        {
            try
            {
                await core();
            }
            catch (Exception) when (_wireAttempts == 2)
            {
                _beforeFilter.TrySetResult();
                await _releaseFilter.Task.WaitAsync(TimeSpan.FromSeconds(10));
                throw;
            }
        });
        typeof(EventStore).GetField("_registerAllFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, flight);
    }

    async Task Because()
    {
        await Catch.Exception(_store.RegisterAll);
        _backgroundRetry = _store.PendingBackgroundRegistrationRetry!;
        await _retryEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _generator.BeforeGenerate = (_, _, _) => throw _preparationFailure;
        var rediscovery = _store.DiscoverAll();
        _releaseTransport.SetException(_transportFailure);
        await _beforeFilter.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Catch.Exception(() => rediscovery).WaitAsync(TimeSpan.FromSeconds(10));
        _outcomeAtFault = _store.Registration.Failure;
        _releaseFilter.SetResult();
        await _backgroundRetry.WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy() => _releaseFilter.TrySetResult();

    [Fact] void should_publish_the_exact_failure_at_the_point_rediscovery_faults() => ReferenceEquals(_outcomeAtFault, _preparationFailure).ShouldBeTrue();
    [Fact] void should_retain_the_newer_failure_after_the_old_retry_exits() => ReferenceEquals(_store.Registration.Failure, _preparationFailure).ShouldBeTrue();
    [Fact] void should_stop_the_retry_loop_without_another_wire_attempt() => _wireAttempts.ShouldEqual(2);
    [Fact] void should_complete_the_background_loop() => _backgroundRetry.IsCompletedSuccessfully.ShouldBeTrue();
}
