// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage;

public class when_the_commit_outcome_is_unknown : given.a_failing_provider
{
    readonly NamespaceCommitOutcomeUnknown _failure = new(new IOException("Commit response lost."));

    void Establish() => SetFailure(_failure);

    Task Because() => ObserveFailures();

    [Fact] void should_propagate_every_unknown_outcome_unchanged() => _observed.ShouldEachConformTo(error => ReferenceEquals(error, _failure));
    [Fact] void should_not_retry_or_recover_behind_the_callers_back() => _provider.ReceivedCalls().Count().ShouldEqual(10);
}
