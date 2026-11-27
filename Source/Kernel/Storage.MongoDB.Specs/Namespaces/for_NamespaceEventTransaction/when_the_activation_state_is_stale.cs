// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_state_is_stale : given.a_namespace_transaction
{
    void Establish() => _context = _context with { ActivationIncarnation = NamespaceIncarnationId.New() };
    Task Because() => ExecuteCatching();

    [Fact] void should_request_a_fresh_activation() => _failure.ShouldBeOfExactType<NamespaceIncarnationRequiresReactivation>();
    [Fact] void should_validate_the_marker_before_rejecting_activation_state() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
    [Fact] void should_not_write_events_or_receipts() => _writeSessions.ShouldBeEmpty();
    [Fact] void should_not_retry_with_a_refreshed_identity() => _attempts.ShouldEqual(1);
}
