// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage;

public class when_the_activation_is_canceled : given.a_failing_provider
{
    readonly CancellationToken _cancellation = new(true);
    OperationCanceledException _failure;

    void Establish()
    {
        _context = _context with { ActivationCancellation = _cancellation };
        _bound = new(_provider, _context);
        _failure = new(_cancellation);
        SetFailure(_failure);
    }

    Task Because() => ObserveFailures();

    [Fact] void should_preserve_the_activation_cancellation() => _observed.ShouldEachConformTo(error => ReferenceEquals(error, _failure));
    [Fact] void should_forward_the_canceled_context_to_every_fence() => _provider.ReceivedCalls().ShouldEachConformTo(call => ReferenceEquals(call.GetArguments()[0], _context));
    [Fact] void should_not_refresh_or_retry() => _provider.ReceivedCalls().Count().ShouldEqual(10);
}
