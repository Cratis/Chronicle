// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_a_cancellation_callback_fails_during_release : given.two_discovered_reducers
{
    readonly InvalidOperationException _failure = new("cancellation failed");
    CancellationTokenRegistration _registration;
    Exception _error;
    Exception _retryError;
    IReducerHandler[] _remainingBeforeRetry;
    bool _secondWasNotCancelled;
    bool _firstWasRemoved;

    void Establish() => _registration = _firstToken.Register(() => throw _failure);

    void Because()
    {
        _error = Catch.Exception(_reducers.Release);
        _remainingBeforeRetry = _reducers.GetAllHandlers().ToArray();
        _firstWasRemoved = !_reducers.HasFor(_first.ReadModelType);
        _secondWasNotCancelled = !_secondToken.IsCancellationRequested;
        _retryError = Catch.Exception(_reducers.Release);
    }

    void Destroy() => _registration.Dispose();

    [Fact] void should_propagate_the_cancellation_failure() => _error.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_preserve_the_original_failure() => ((AggregateException)_error).InnerExceptions.ShouldContainOnly(_failure);
    [Fact] void should_remove_the_handler_whose_cancellation_ran() => _firstWasRemoved.ShouldBeTrue();
    [Fact] void should_keep_the_unprocessed_handler_after_failure() => _remainingBeforeRetry.ShouldContainOnly(_second);
    [Fact] void should_stop_cancelling_after_the_failure() => _secondWasNotCancelled.ShouldBeTrue();
    [Fact] void should_allow_retrying_the_remaining_handler() => _retryError.ShouldBeNull();
    [Fact] void should_empty_the_indexes_after_retry() => _reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_dispose_the_remaining_handler_on_retry() => Catch.Exception(() => _ = _second.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_preserve_the_failed_disconnect_disposal_semantics() => _first.CancellationToken.IsCancellationRequested.ShouldBeTrue();
}
