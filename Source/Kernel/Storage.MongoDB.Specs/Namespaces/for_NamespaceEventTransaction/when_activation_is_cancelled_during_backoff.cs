// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_activation_is_cancelled_during_backoff : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _duringDelay = _activation.Cancel;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_cancel_instead_of_opening_another_transaction() => _failure.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_start_a_second_attempt() => _attempts.ShouldEqual(1);
    [Fact] void should_have_one_backoff() => _delays.Count.ShouldEqual(1);
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
}
