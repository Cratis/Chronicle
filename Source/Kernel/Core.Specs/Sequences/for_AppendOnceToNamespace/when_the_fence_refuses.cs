// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Sequences.for_AppendOnceToNamespace;

public class when_the_fence_refuses : given.a_namespace_bound_keyed_endpoint
{
    void Establish() => _refusal = new NamespaceCommitRejected();
    Task Because() => AppendSingle();

    [Fact] void should_preserve_the_refusal() => _error.ShouldEqual(_refusal);
    [Fact] void should_not_report_a_result() => _singleResult.ShouldBeNull();
    [Fact] void should_not_retry_without_the_identity() => _eventSequence.ReceivedCalls().Count().ShouldEqual(1);
}
