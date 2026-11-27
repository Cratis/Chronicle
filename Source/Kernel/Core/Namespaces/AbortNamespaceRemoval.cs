// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Conditionally aborts pre-destructive removal and opens a new namespace incarnation.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="OperationId">The observed removal operation.</param>
/// <param name="ExpectedRevision">The observed revision.</param>
[Command]
[BelongsTo(WellKnownServices.Namespaces)]
[StoreAccess(StoreAccess.Manage)]
public record AbortNamespaceRemoval(EventStoreName EventStore, EventStoreNamespaceName Namespace, Guid OperationId, long ExpectedRevision)
{
    /// <summary>Aborts the exact observed removal; use the status query to inspect the replacement lifetime.</summary>
    /// <param name="incarnations">The capability-gated namespace owners.</param>
    /// <returns>The replacement lifetime's open acknowledgement.</returns>
    public Task Handle(INamespaceIncarnations incarnations) => incarnations.Abort(EventStore, Namespace, OperationId, ExpectedRevision);
}
