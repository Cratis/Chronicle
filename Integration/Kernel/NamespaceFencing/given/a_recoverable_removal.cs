// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing.given;

/// <summary>
/// Seeds a RecoveryRequired checkpoint through the real provider CAS, then exercises real operator APIs.
/// This is not evidence that a live crash or five real provider failures have been reproduced.
/// </summary>
/// <param name="fixture">The integration-only kernel fixture.</param>
public abstract class a_recoverable_removal(NamespaceFenceFixture fixture) : a_named_namespace(fixture)
{
    protected NamespaceLifecycle _recovery = null!;
    protected Guid _operation;
    protected long _revision;

    protected Task<CommandResult> Resume(long expectedRevision) => _services.Services.Namespaces.ResumeNamespaceRemoval(new()
    {
        EventStore = _storeName,
        Namespace = Tenant,
        OperationId = _operation,
        ExpectedRevision = expectedRevision
    });

    protected Task<CommandResult> Abort(long expectedRevision) => _services.Services.Namespaces.AbortNamespaceRemoval(new()
    {
        EventStore = _storeName,
        Namespace = Tenant,
        OperationId = _operation,
        ExpectedRevision = expectedRevision
    });

    protected async Task SeedUnknownDestructiveIntent()
    {
        var unknown = _recovery with
        {
            Revision = _revision + 1,
            Stage = NamespaceRemovalStage.Drop,
            AttemptInProgress = true,
            DestructiveAttemptOutstanding = true,
            DestructiveActionIssued = true,
            LastAttemptAt = DateTimeOffset.UtcNow,
            Deadline = DateTimeOffset.UtcNow,
            LastFailure = "IntegrationSeededUnknownDrop"
        };
        (await _capability.NamespaceLifecycles.TryWrite(Tenant, _revision, unknown)).ShouldBeTrue();
        _recovery = unknown;
        _revision = unknown.Revision;
    }

    async Task Establish()
    {
        var current = (await _capability.NamespaceLifecycles.Get(Tenant))!;
        _operation = Guid.NewGuid();
        _recovery = current.BeginRemoval(_operation, DateTimeOffset.UtcNow) with
        {
            Status = NamespaceLifecycleStatus.RecoveryRequired,
            Revision = current.Revision + 1,
            TotalAttempts = NamespaceLifecycle.MaximumAttempts,
            LastFailure = "IntegrationSeededBeforeDestruction"
        };
        (await _capability.GetNamespaceMarker(Tenant).TryClose(current.Incarnation)).ShouldBeTrue();
        (await _capability.NamespaceLifecycles.TryWrite(Tenant, current.Revision, _recovery)).ShouldBeTrue();
        _revision = _recovery.Revision;
        var status = await Status();
        status.Status.ShouldEqual(nameof(NamespaceLifecycleStatus.RecoveryRequired));
        status.OperationId.ShouldEqual(_operation);
    }
}
