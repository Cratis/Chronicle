// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Conditionally authorizes a fresh bounded budget for the exact observed recovery operation.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="OperationId">The observed removal operation, never automatically refreshed.</param>
/// <param name="ExpectedRevision">The observed revision, never automatically refreshed.</param>
[Command]
[BelongsTo(WellKnownServices.Namespaces)]
[StoreAccess(StoreAccess.Manage)]
public record ResumeNamespaceRemoval(EventStoreName EventStore, EventStoreNamespaceName Namespace, Guid OperationId, long ExpectedRevision)
{
    /// <summary>Resumes the exact observed removal; use the status query to inspect progress.</summary>
    /// <param name="incarnations">The capability-gated namespace owners.</param>
    /// <returns>The request acknowledgement, not a promise of completed deletion.</returns>
    public Task Handle(INamespaceIncarnations incarnations) => incarnations.Resume(EventStore, Namespace, OperationId, ExpectedRevision);
}
