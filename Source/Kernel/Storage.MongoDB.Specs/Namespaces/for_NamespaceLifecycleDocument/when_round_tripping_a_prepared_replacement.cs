// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleDocument;

public class when_round_tripping_a_prepared_replacement : Specification
{
    readonly NamespaceLifecycle _original = new("Tenant", NamespaceIncarnationId.New(), DateTimeOffset.UtcNow)
    {
        Revision = 43,
        PreviousIncarnation = NamespaceIncarnationId.New(),
        NotifyOnOpen = true
    };
    NamespaceLifecycle _copy;

    void Because() => _copy = NamespaceLifecycleDocument.Decode(NamespaceLifecycleDocument.Encode(_original));

    [Fact] void should_keep_the_same_prepared_identity_and_previous_marker() => _copy.ShouldEqual(_original);
    [Fact] void should_not_skip_pending_creation() => _copy.NotifyOnOpen.ShouldBeTrue();
    [Fact] void should_preserve_absent_retry_and_deadline() => _copy.Deadline.ShouldBeNull();
}
