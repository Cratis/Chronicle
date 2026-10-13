// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_rediscovery_fails_during_background_retry : given.an_explicit_store
{
    TaskCompletionSource _retryEntered;
    TaskCompletionSource<CommandResult> _releaseRetry;
    Exception _transportFailure;
    Exception _preparationFailure;
    int _wireAttempts;
    Task _backgroundRetry;

    async Task Establish()
    {
        _retryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseRetry = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _transportFailure = new Exception("transport unavailable");
        _preparationFailure = new Exception("rediscovery schema failure");
        _options.RegistrationRetry.MaxAttempts = 1;
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            _wireAttempts++;
            if (_wireAttempts == 1) return Task.FromException<CommandResult>(_transportFailure);
            _retryEntered.TrySetResult();
            return _releaseRetry.Task;
        });
        _store = CreateStore();
        await _store.DiscoverAll();
    }

    async Task Because()
    {
        await Catch.Exception(_store.RegisterAll);
        _backgroundRetry = _store.PendingBackgroundRegistrationRetry!;
        await _retryEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _generator.BeforeGenerate = (_, _, _) => throw _preparationFailure;
        var rediscovery = _store.DiscoverAll();
        _releaseRetry.SetException(_transportFailure);
        await Catch.Exception(() => rediscovery).WaitAsync(TimeSpan.FromSeconds(10));
        await _backgroundRetry.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_stop_the_existing_background_retry_loop() => _backgroundRetry.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_publish_the_exact_preparation_failure() => ReferenceEquals(_store.Registration.Failure, _preparationFailure).ShouldBeTrue();
    [Fact] void should_not_keep_sending_transport_attempts_for_a_failed_local_revision() => _wireAttempts.ShouldEqual(2);
}
