// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings;

public class when_the_call_is_cancelled : given.a_namespace_lookup
{
    void Establish() => _cancellationToken = new CancellationToken(true);
    Task Because() => Lookup();

    [Fact] void should_report_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_call_the_kernel() => _namespaces.ReceivedCalls().ShouldBeEmpty();
}
