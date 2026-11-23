// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_is_cancelled_between_attempts : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _onFenceFailure = _activation.Cancel;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_report_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_start_a_second_attempt() => _attempts.ShouldEqual(1);
    [Fact] void should_abort_without_writing_events() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
}
