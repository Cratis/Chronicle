// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceExceptionCodes;

public class when_the_commit_outcome_is_unknown : Specification
{
    string _code;

    void Because() => _code = NamespaceExceptionCodes.For(new NamespaceCommitOutcomeUnknown(new NamespaceCommitRejected(NamespaceCommitRejectionReason.Closing)))!;

    [Fact] void should_not_relabel_a_submitted_commit_as_a_definite_refusal() => _code.ShouldBeNull();
}
