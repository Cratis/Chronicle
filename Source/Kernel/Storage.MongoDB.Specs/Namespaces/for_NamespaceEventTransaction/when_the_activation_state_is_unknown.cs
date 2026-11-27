// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_state_is_unknown : given.a_namespace_transaction
{
    void Establish() => _context = _context with { ActivationIncarnation = null };
    Task Because() => ExecuteCatching();

    [Fact] void should_not_assume_unknown_state_is_current() => _failure.ShouldBeOfExactType<NamespaceIncarnationRequiresReactivation>();
    [Fact] void should_validate_the_marker_then_abort() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
    [Fact] void should_not_write_events_or_receipts() => _writeSessions.ShouldBeEmpty();
}
