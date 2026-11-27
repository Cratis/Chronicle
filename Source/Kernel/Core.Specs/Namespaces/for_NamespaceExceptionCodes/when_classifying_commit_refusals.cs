// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceExceptionCodes;

public class when_classifying_commit_refusals : Specification
{
    [Theory]
    [InlineData(NamespaceCommitRejectionReason.IncarnationMismatch, "NamespaceIncarnationMismatch")]
    [InlineData(NamespaceCommitRejectionReason.Closing, "NamespaceIncarnationClosed")]
    [InlineData(NamespaceCommitRejectionReason.Unavailable, "NamespaceIncarnationUnavailable")]
    void should_use_the_typed_reason(NamespaceCommitRejectionReason reason, string expected) =>
        NamespaceExceptionCodes.For(new NamespaceCommitRejected(reason)).ShouldEqual(expected);
}
