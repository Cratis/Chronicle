// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_is_already_cancelled : given.a_namespace_transaction
{
    void Establish() => _activation.Cancel();
    Task Because() => ExecuteCatching();

    [Fact] void should_report_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_start_an_attempt() => _attempts.ShouldEqual(0);
    [Fact] void should_not_write() => _steps.ShouldBeEmpty();
}
