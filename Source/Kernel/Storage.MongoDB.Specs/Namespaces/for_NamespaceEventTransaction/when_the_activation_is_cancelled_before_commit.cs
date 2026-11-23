// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_is_cancelled_before_commit : given.a_namespace_transaction
{
    void Establish() => _duringWrite = _activation.Cancel;
    Task Because() => ExecuteCatching();

    [Fact] void should_report_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_abort_even_though_the_activation_token_is_cancelled() => _steps.SequenceEqual(["fence", "write", "abort"]).ShouldBeTrue();
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
}
