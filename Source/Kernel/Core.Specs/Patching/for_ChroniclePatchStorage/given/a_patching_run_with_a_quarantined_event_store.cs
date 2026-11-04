// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

/// <summary>
/// A real <see cref="PatchManager"/> with a real fan-out patch over two event stores, one of which is quarantined and
/// refuses to be opened as the real storage does.
/// </summary>
public class a_patching_run_with_a_quarantined_event_store : a_patching_run
{
    protected static readonly EventStoreName Healthy = "healthy-store";
    protected static readonly EventStoreName Held = "held-store";

    protected ResourceNameSnapshot _resourceNames;
    protected IEventStoreStorage _healthyStorage;
    protected bool _quarantineLifted;

    void Establish()
    {
        _resourceNames = new ResourceNameSnapshot();
        _resourceNames.Publish(new ResourceNameAuditResult([Held.Value], [], []));

        _healthyStorage = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([Healthy, Held]));
        _storage.GetEventStore(Healthy).Returns(_healthyStorage);
        _storage.GetEventStore(Held).Returns(_ => _quarantineLifted ? Substitute.For<IEventStoreStorage>() : throw new ResourceQuarantined());

        var grains = Substitute.For<IGrainFactory>();
        foreach (var store in new[] { Healthy, Held })
        {
            var namespaces = Substitute.For<INamespaces>();
            namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([]));
            grains.GetGrain<INamespaces>(store).Returns(namespaces);
        }

        _patches.Clear();
        _patches.Add(new RemoveUnhashedUniqueConstraintValues(_storage, grains, NullLogger<RemoveUnhashedUniqueConstraintValues>.Instance, _resourceNames));
    }
}
