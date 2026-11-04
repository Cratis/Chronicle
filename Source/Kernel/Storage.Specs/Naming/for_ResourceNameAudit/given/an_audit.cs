// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit.given;

public class an_audit : Specification
{
    protected static readonly TimeSpan _budget = TimeSpan.FromSeconds(30);

    protected IResourceInventory _inventory;
    protected ResourceNameSnapshot _snapshot;
    protected IResourceNameGate _gate;
    protected IResourceNameReservations _reservations;
    protected ResourceNameAudit _audit;
    protected ManualTimeProvider _time;

    void Establish()
    {
        _inventory = Substitute.For<IResourceInventory>();
        _snapshot = ResourceNameSnapshot.NotReady();
        _gate = Substitute.For<IResourceNameGate>();
        _reservations = Substitute.For<IResourceNameReservations>();
        _time = new();
        _audit = new([_inventory], _snapshot, _gate, ResourceNameExemptions.None, NullLogger<ResourceNameAudit>.Instance, _time);
    }

    protected static ResourceUse Use(string store, string identity, string? @namespace = null) =>
        new(new ResourceOwner(store, @namespace), @namespace is null ? ResourceKind.EventStoreDatabase : ResourceKind.NamespaceDatabase, identity);

    protected void Returns(params ResourceUse[] uses) => _inventory.Read(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<ResourceUse>>(uses));

    protected async Task Run(NameCollisionHandling mode = NameCollisionHandling.Quarantine, TimeSpan? timeout = null, Func<Exception, bool>? isTransient = null)
    {
        // Moves the manual time forward while the audit waits, so nothing depends on the wall clock.
        var run = _audit.RunAsync(mode, timeout ?? _budget, isTransient ?? (_ => false), CancellationToken.None);
        for (var step = 0; !run.IsCompleted && step < 10_000; step++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }

        await run;
    }
}
